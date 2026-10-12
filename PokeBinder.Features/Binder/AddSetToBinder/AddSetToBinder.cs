using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;
using PokeBinder.Features.Binder.AddSetToBinder.Models;
using PokeBinder.Features.Binder.Shared.Arrangement;
using PokeBinder.Features.CardImages;
using PokeBinder.TcgCatalog.DbContext;
using CatalogCard = PokeBinder.TcgCatalog.DbContext.Entities.Card;

namespace PokeBinder.Features.Binder.AddSetToBinder;

/// <summary>
/// Puts every card of one set into a binder's pockets in one action, rather than card by card
/// through search: in the order the request asks for, less any rarities it leaves out, and wherever
/// the request says relative to the cards already there.
///
/// <para>
/// The tray takes no part, whatever the options: the set goes straight into the pockets, and the
/// cards an emptied binder held are removed rather than sent there. The client snapshots the binder
/// before and after for undo, which is what brings removed cards back.
/// </para>
///
/// <para>
/// <b>The binder only ever grows.</b> When the set does not fit, the request has to say pages may be
/// added, and they go on at the end -- which moves no card already placed, since a pocket is one
/// absolute index across the binder. The validator refuses a set that does not fit without that.
/// </para>
///
/// <para>
/// The whole write is one <c>SaveChangesAsync</c>, and the response is the binder's new layout, so
/// the client can show it without asking again -- and record it for undo, since the layout this
/// replaced is gone from the server once this returns.
/// </para>
/// </summary>
public static class AddSetToBinder
{
    public record Request
    {
        public int BinderId { get; init; }

        public int SetId { get; init; }

        /// <summary>Rarities, by name, whose cards stay out. Empty puts the whole set in.</summary>
        public IReadOnlyList<string> LeaveOut { get; init; } = [];

        /// <summary>
        /// The order within the set, most significant first: Rarity, Card name and Card number. No
        /// Set criterion, since there is only the one.
        /// </summary>
        public IReadOnlyList<SortCriterion> Criteria { get; init; } = [];

        /// <summary>Removes every card from the binder before the set goes in. Not to the tray.</summary>
        public bool EmptyFirst { get; init; }

        public ExistingCards Existing { get; init; }

        /// <summary>Pages may be added at the end when the set does not fit as the binder stands.</summary>
        public bool AddPages { get; init; }
    }

    /// <param name="Pages">The binder's page count now, which is more than before when pages were added.</param>
    /// <param name="SetStart">The pocket the set's first card went in, for the workspace to turn to.</param>
    /// <param name="Cards">Every placed card in its pocket, in pocket order.</param>
    /// <param name="Added">The set's cards as the workspace draws them, one per card that went in.</param>
    public record Response(
        int BinderId,
        int Pages,
        int SetStart,
        IReadOnlyList<ReorderedCard> Cards,
        IReadOnlyList<AddedCard> Added);

    /// <summary>Where the set and the cards already placed go, worked out before anything is written.</summary>
    /// <param name="SetStart">The pocket the set's first card goes in.</param>
    /// <param name="ExistingShift">How many pockets every kept card moves back by.</param>
    /// <param name="KeepExisting">Whether the cards already placed stay in the binder at all.</param>
    /// <param name="PagesNeeded">The pages the binder needs for all of it.</param>
    internal record Plan(int SetStart, int ExistingShift, bool KeepExisting, int PagesNeeded);

    /// <summary>
    /// The layout as a few numbers. The validator runs it to decide whether the set fits, and the
    /// handler to place it, so the two cannot disagree. The workspace previews the same thing in
    /// <c>set-placement.ts</c>.
    /// </summary>
    /// <param name="lastPlacedIndex">The binder's last full pocket, or null for an empty binder.</param>
    internal static Plan PlanPlacement(
        int? lastPlacedIndex,
        int cardsPerPage,
        int setCards,
        bool emptyFirst,
        ExistingCards existing)
    {
        var keep = !emptyFirst && lastPlacedIndex is not null;

        // Pages up to and including the last one holding a card, which is where "after them" ends.
        var usedPages = keep ? lastPlacedIndex!.Value / cardsPerPage + 1 : 0;

        var setPages = Math.Max(1, (setCards + cardsPerPage - 1) / cardsPerPage);

        return (keep ? existing : ExistingCards.After) switch
        {
            // Whole pages, so a card keeps its place on its page and its neighbours.
            ExistingCards.End => new Plan(0, setPages * cardsPerPage, keep, setPages + usedPages),
            ExistingCards.Buffer => new Plan((usedPages + 1) * cardsPerPage, 0, keep, usedPages + 1 + setPages),
            _ => new Plan(usedPages * cardsPerPage, 0, keep, usedPages + setPages),
        };
    }

    /// <summary>The set's cards that go in, before any sort. The validator counts them; the handler places them.</summary>
    internal static IQueryable<CatalogCard> SetCards(
        TcgCatalogDbContext catalogContext,
        int setId,
        IReadOnlyList<string> leaveOut) =>
        catalogContext.Cards.Where(card =>
            card.SetId == setId && (card.Rarity == null || !leaveOut.Contains(card.Rarity)));

    /// <param name="userId">
    /// The signed-in user. AddSetToBinderValidator has already established that this binder is
    /// theirs; the read below is scoped by it anyway, so a handler called without its validator
    /// cannot write to another user's binder.
    /// </param>
    public static async Task<Response> Handler(
        Request request,
        int userId,
        BinderDbContext binderContext,
        TcgCatalogDbContext catalogContext,
        CardImageUrls imageUrls,
        CancellationToken ct = default)
    {
        var binder = await binderContext.Binders
            .Include(b => b.BinderSize)
            .FirstAsync(b => b.Id == request.BinderId && b.UserId == userId, ct);

        var stored = await binderContext.BinderCards
            .Where(card => card.BinderId == binder.Id)
            .OrderBy(card => card.IndexInBinder)
            .ToListAsync(ct);

        // By id first, so whatever the criteria and the sort's own tiebreaks leave level still
        // comes out the same way every time.
        var setCardIds = await SetCards(catalogContext, request.SetId, request.LeaveOut)
            .OrderBy(card => card.Id)
            .Select(card => card.Id)
            .ToListAsync(ct);

        var sorted = await BinderArrangement.SortCardsAsync(setCardIds, request.Criteria, catalogContext, ct);

        var plan = PlanPlacement(
            stored.Count == 0 ? null : stored[^1].IndexInBinder,
            binder.BinderSize!.CardsPerPage,
            sorted.Count,
            request.EmptyFirst,
            request.Existing);

        IEnumerable<ReorderedCard> kept = plan.KeepExisting
            ? stored.Select(card => new ReorderedCard(
                card.IndexInBinder + plan.ExistingShift, card.CardId, card.IsMissing ?? false))
            : [];

        var added = sorted.Select((cardId, offset) => new ReorderedCard(plan.SetStart + offset, cardId, false));

        var laidOut = BinderArrangement.LayOut(stored, [.. kept, .. added], binder.Id, binderContext);

        // Only ever more pages, never fewer: a binder longer than the set needs keeps its length.
        if (plan.PagesNeeded > binder.Pages)
        {
            binder.Pages = plan.PagesNeeded;
        }

        await binderContext.SaveChangesAsync(ct);

        var cards = await LoadCardsAsync(sorted, catalogContext, imageUrls, ct);

        return new Response(binder.Id, binder.Pages, plan.SetStart, laidOut, cards);
    }

    /// <summary>The set's cards as the workspace draws them, in the order they went in.</summary>
    private static async Task<List<AddedCard>> LoadCardsAsync(
        List<int> cardIds,
        TcgCatalogDbContext catalogContext,
        CardImageUrls imageUrls,
        CancellationToken ct)
    {
        var cards = await catalogContext.Cards
            .Where(card => cardIds.Contains(card.Id))
            .Select(card => new AddedCard
            {
                Id = card.Id,
                Name = card.Name,
                Rarity = card.Rarity,
                CardNumber = card.CardNumber,
                TcgPlayerId = card.TcgPlayerId,
                ImageUrl = card.ImageUrl,
                SetName = card.Set != null ? card.Set.Name : null,
            })
            .ToDictionaryAsync(card => card.Id, ct);

        // The card stores a local file path, so it becomes a URL once the rows are back.
        foreach (var card in cards.Values)
        {
            card.ImageUrl = imageUrls.ToPublicUrl(card.ImageUrl);
        }

        return cardIds.Select(id => cards[id]).ToList();
    }
}
