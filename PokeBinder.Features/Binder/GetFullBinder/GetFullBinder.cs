using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;
using PokeBinder.Features.Binder.GetFullBinder.Models;
using PokeBinder.Features.CardImages;
using PokeBinder.TcgCatalog.DbContext;

namespace PokeBinder.Features.Binder.GetFullBinder;

/// <summary>
/// One binder, whole: what it is, every card placed in it and where, and what is still waiting in
/// its tray. This is the load that puts a client in a position to edit — and, because
/// SaveBinderCards and SaveBinderTray both take snapshots, in a position to save without first
/// asking what it does not already know.
///
/// It reads both databases. The binder, its placements and its tray are in one; the cards those
/// point at are in the catalog, a separate SQLite file. Nothing can join across the two, so the
/// card details are fetched in a second query — one for the whole binder, not one per card — and
/// stitched on here.
/// </summary>
public static class GetFullBinder
{
    public record Request
    {
        public int BinderId { get; init; }
    }

    /// <param name="Binder">Null when the binder does not exist or is not the caller's.</param>
    /// <param name="Cards">Placements in pocket order.</param>
    /// <param name="Tray">Cards picked out for this binder but not yet placed.</param>
    public record Response(
        BinderSummary? Binder,
        IReadOnlyList<BinderPlacement> Cards,
        IReadOnlyList<BinderTrayCard> Tray)
    {
        /// <summary>There was no such binder for this user. Everything else is empty.</summary>
        public bool Found => Binder is not null;

        internal static Response NotFound() => new(null, [], []);
    }

    /// <param name="userId">
    /// The signed-in user. A binder that is not theirs answers exactly as a missing one does, so
    /// the response cannot be used to find out which binder ids exist.
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
            .Where(entity => entity.Id == request.BinderId && entity.UserId == userId)
            .Select(entity => new BinderSummary
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                CreatedAt = entity.CreatedAt,
                Pages = entity.Pages,
                BinderSizeId = entity.BinderSizeId,
                SizeName = entity.BinderSize!.Name,
                SizeDescription = entity.BinderSize.Description,
                X = entity.BinderSize.X,
                Y = entity.BinderSize.Y,
                // CardsPerPage and CardCount are computed on the entities, which SQL cannot call,
                // so the same arithmetic is written out here to keep the projection translatable.
                CardsPerPage = entity.BinderSize.X * entity.BinderSize.Y,
                CardCount = entity.BinderSize.X * entity.BinderSize.Y * entity.Pages,
            })
            .FirstOrDefaultAsync(ct);

        if (binder is null)
        {
            return Response.NotFound();
        }

        // Read once, with the card id alongside: the placement rows and the card details come from
        // different databases, so the id has to survive the first query to look the second one up.
        var placementRows = await binderContext.BinderCards
            .Where(card => card.BinderId == binder.Id)
            .OrderBy(card => card.IndexInBinder)
            .Select(card => new
            {
                card.IndexInBinder,
                card.CardId,
                card.IsMissing,
            })
            .ToListAsync(ct);

        // Read alongside the placements rather than left to a second call: the tray belongs to this
        // binder, a client about to edit needs both, and asking separately is two round trips to
        // draw one screen.
        var trayRows = await binderContext.BinderTray
            .Where(entry => entry.BinderId == binder.Id)
            .OrderBy(entry => entry.CardId)
            .Select(entry => new { entry.CardId, entry.Quantity })
            .ToListAsync(ct);

        var cardIds = placementRows
            .Select(row => row.CardId)
            .Concat(trayRows.Select(row => row.CardId))
            .Distinct()
            .ToList();

        var cards = await LoadCardsAsync(cardIds, catalogContext, imageUrls, ct);

        // Page and slot are the index divided by the grid. Derived here rather than stored, and
        // here rather than in each client, so everything counts them the same way: pages from one,
        // because that is how a person refers to them, and slots from zero, because that is how a
        // grid is indexed.
        var placements = placementRows
            .Select(row => new BinderPlacement
            {
                IndexInBinder = row.IndexInBinder,
                Page = row.IndexInBinder / binder.CardsPerPage + 1,
                SlotOnPage = row.IndexInBinder % binder.CardsPerPage,
                IsMissing = row.IsMissing ?? false,
                Card = cards.GetValueOrDefault(row.CardId),
            })
            .ToList();

        var tray = trayRows
            .Select(row => new BinderTrayCard
            {
                Quantity = row.Quantity,
                Card = cards.GetValueOrDefault(row.CardId),
            })
            .ToList();

        return new Response(binder, placements, tray);
    }

    private static async Task<Dictionary<int, BinderCardDetails>> LoadCardsAsync(
        IReadOnlyList<int> cardIds,
        TcgCatalogDbContext catalogContext,
        CardImageUrls imageUrls,
        CancellationToken ct)
    {
        if (cardIds.Count == 0)
        {
            return [];
        }

        var cards = await catalogContext.Cards
            .Where(card => cardIds.Contains(card.Id))
            .Select(card => new BinderCardDetails
            {
                Id = card.Id,
                Name = card.Name,
                Rarity = card.Rarity,
                CardNumber = card.CardNumber,
                TcgPlayerId = card.TcgPlayerId,
                ImageUrl = card.ImageUrl,
                SetName = card.Set != null ? card.Set.Name : null,
            })
            .ToListAsync(ct);

        // The card stores a local file path, so it becomes a URL once the rows are back rather than
        // inside the query — string surgery in SQL would buy nothing here.
        foreach (var card in cards)
        {
            card.ImageUrl = imageUrls.ToPublicUrl(card.ImageUrl);
        }

        return cards.ToDictionary(card => card.Id);
    }
}
