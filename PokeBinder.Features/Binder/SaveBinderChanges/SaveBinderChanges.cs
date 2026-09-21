using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;
using PokeBinder.Features.Binder.SaveBinderChanges.Models;
using PlacedCardEntity = PokeBinder.Binders.DbContext.Entities.BinderCard;
using TrayEntry = PokeBinder.Binders.DbContext.Entities.BinderTray;

namespace PokeBinder.Features.Binder.SaveBinderChanges;

/// <summary>
/// Stores one edit to a binder: the pages the user is looking at, and the whole tray.
///
/// <para>
/// This is what the workspace saves on, and it is the only thing that writes a binder's contents.
/// It is deliberately not the whole binder. A snapshot of every pocket is the right shape for a
/// client that loads, edits and saves once, and the wrong shape behind a debounce: editing page two
/// of a fifty-page binder would post fifty pages back every few seconds. So the payload is partial,
/// and being partial is the whole design problem -- an emptied pocket and an unsent pocket look
/// identical unless the request says what it covers.
/// </para>
///
/// <para>
/// <b>Which is why the request names its pages.</b> The pages are the scope: the diff runs over the
/// pockets those pages cover, and a row outside them is not written however the payload looks.
/// Inside the claim the payload is still a snapshot -- a pocket the request leaves out is one the
/// user emptied -- so a tick that fires twice, a retry after a dropped connection and a reconnect
/// after a sleeping tab all write the same thing. Outside the claim, the binder's other pages are
/// not this request's business.
/// </para>
///
/// <para>
/// The tray travels whole, because it has no comparable scope to claim and no need of one: it is a
/// flat list of one row per card, and the workspace keeps it to a few hundred at most.
/// </para>
///
/// <para>
/// Both halves in one call and one <c>SaveChangesAsync</c>, so one debounce tick is one
/// transaction. Split in two they can half-fail: placing a card spends a copy out of the tray, so
/// a committed page write with a failed tray write leaves that copy both placed and still waiting.
/// </para>
/// </summary>
public static class SaveBinderChanges
{
    /// <summary>
    /// Pages one save may claim.
    ///
    /// <para>
    /// Two, because a binder falls open two pages at a time and a save is armed against the spread
    /// that was on screen when the edit happened -- a page flip flushes the pending save rather
    /// than widening it. A payload naming more pages than the user can see at once is a client that
    /// has lost track of its own scope, and believing it costs a diff over pages nobody touched.
    /// </para>
    /// </summary>
    public const int MaxPages = 2;

    public record Request
    {
        public int BinderId { get; init; }

        /// <summary>
        /// The pages this save covers, counted from one. Not decoration: it is the range the diff
        /// runs over, so a page named here and left out of <see cref="Cards"/> is a page the user
        /// emptied, and a page not named here is one the save does not touch at all.
        /// </summary>
        public IReadOnlyList<int> Pages { get; init; } = [];

        /// <summary>
        /// Every card sitting on the claimed pages, in any order. An empty list is meaningful and
        /// allowed: those pages are empty now, and the stored cards on them are cleared to match.
        /// </summary>
        public IReadOnlyList<PlacedCard> Cards { get; init; } = [];

        /// <summary>
        /// The tray in full -- the whole list, not the rows that changed. An empty list is
        /// meaningful and allowed: the user emptied the tray.
        /// </summary>
        public IReadOnlyList<TrayCard> Tray { get; init; } = [];
    }

    /// <param name="Placed">Pockets on the claimed pages that were empty and now hold a card.</param>
    /// <param name="Changed">
    /// Pockets that held a card and now hold a different one, or the same one with its missing flag
    /// flipped. A card dragged from one pocket to another is not this: the row is keyed by pocket,
    /// so that reads as one Removed and one Placed.
    /// </param>
    /// <param name="Removed">Pockets on the claimed pages the request left empty.</param>
    /// <param name="TrayAdded">Cards that were not in the stored tray.</param>
    /// <param name="TrayUpdated">Cards whose quantity changed.</param>
    /// <param name="TrayRemoved">Cards the request left out of the tray.</param>
    public record Response(
        int BinderId,
        int Placed,
        int Changed,
        int Removed,
        int TrayAdded,
        int TrayUpdated,
        int TrayRemoved);

    /// <summary>
    /// The pockets a set of page numbers covers, counted from zero across the whole binder: page
    /// <c>n</c> is <c>(n - 1) * cardsPerPage</c> through <c>n * cardsPerPage - 1</c>.
    ///
    /// <para>
    /// The arithmetic lives here rather than in the handler because the validator needs the same
    /// answer -- it refuses a card outside the claim, and the handler diffs inside it. Two copies
    /// would be two chances to disagree about what a request covers, which is the one thing this
    /// contract cannot afford to be vague about.
    /// </para>
    /// </summary>
    public static HashSet<int> PocketsOnPages(IEnumerable<int> pages, int cardsPerPage)
    {
        var pockets = new HashSet<int>();

        foreach (var page in pages)
        {
            var start = (page - 1) * cardsPerPage;

            for (var offset = 0; offset < cardsPerPage; offset++)
            {
                pockets.Add(start + offset);
            }
        }

        return pockets;
    }

    /// <param name="userId">
    /// The signed-in user. SaveBinderChangesValidator has already established that this binder is
    /// theirs, so nothing here re-checks it -- except the read below, which is scoped by it anyway
    /// so that a handler called without its validator cannot read another user's grid.
    /// </param>
    public static async Task<Response> Handler(
        Request request,
        int userId,
        BinderDbContext context,
        CancellationToken ct = default)
    {
        // The grid is what turns a page number into pockets, and it is read here rather than taken
        // from the request: a client holding a stale grid would claim a range that is not the page
        // it edited, and a server that took its word for it would have no way to notice.
        var cardsPerPage = await context.Binders
            .Where(binder => binder.Id == request.BinderId && binder.UserId == userId)
            .Select(binder => binder.BinderSize!.X * binder.BinderSize.Y)
            .FirstAsync(ct);

        var claimed = PocketsOnPages(request.Pages, cardsPerPage);

        var (placed, changed, removed) = await SaveCardsAsync(request, claimed, context, ct);
        var (added, updated, trayRemoved) = await SaveTrayAsync(request, context, ct);

        // One call for both halves, for the reason in the summary. Most ticks change nothing on one
        // side or the other, and a tick that changes nothing at all costs two reads and no write.
        if (placed > 0 || changed > 0 || removed > 0 || added > 0 || updated > 0 || trayRemoved > 0)
        {
            await context.SaveChangesAsync(ct);
        }

        return new Response(request.BinderId, placed, changed, removed, added, updated, trayRemoved);
    }

    /// <summary>
    /// Diffs the claimed pages against what is stored and queues the difference. Nothing is written
    /// here: the caller commits both halves together.
    /// </summary>
    private static async Task<(int Placed, int Changed, int Removed)> SaveCardsAsync(
        Request request,
        HashSet<int> claimed,
        BinderDbContext context,
        CancellationToken ct)
    {
        // A save claiming no pages has nothing to say about the pockets, which is not the same as
        // saying they are empty. The validator refuses such a request; a handler called without it
        // leaves the cards alone rather than reading the binder to decide it changed nothing.
        if (claimed.Count == 0)
        {
            return (0, 0, 0);
        }

        // Read as one span rather than as a list of indexes: a page of a large grid is hundreds of
        // pockets, and an IN clause over those is a worse query than a range over the same rows.
        // Two claimed pages are adjacent in practice -- they are a spread -- so the span is the
        // claim, and the filter in the walk is what keeps a row in between from being touched when
        // they are not.
        var lowest = claimed.Min();
        var highest = claimed.Max();

        var stored = await context.BinderCards
            .Where(card => card.BinderId == request.BinderId
                && card.IndexInBinder >= lowest
                && card.IndexInBinder <= highest)
            .ToListAsync(ct);

        // Keyed by pocket, because that is the row's identity: the primary key is the binder and
        // the index, so the same card in two pockets is two rows and one pocket holds one card.
        var posted = request.Cards.ToDictionary(card => card.IndexInBinder);

        var placed = 0;
        var changed = 0;
        var removed = 0;

        foreach (var entry in stored)
        {
            // Inside the span that was read, outside the pages the request claims. This is the line
            // the whole contract turns on: a row here is one the save has no opinion about, and
            // deleting it for being absent from the payload is the failure mode -- a binder that
            // comes back short, with nothing anywhere to say why.
            if (!claimed.Contains(entry.IndexInBinder))
            {
                continue;
            }

            if (!posted.Remove(entry.IndexInBinder, out var card))
            {
                context.BinderCards.Remove(entry);
                removed++;
                continue;
            }

            // A pocket that swapped one card for another, or the same card newly flagged: either
            // way the row's contents changed and the count should say so. The stored flag is
            // nullable and older rows leave it null, which means the same as false -- comparing
            // without saying so would call every one of those rows changed on the first save.
            if (entry.CardId != card.CardId || (entry.IsMissing ?? false) != card.IsMissing)
            {
                entry.CardId = card.CardId;
                entry.IsMissing = card.IsMissing;
                changed++;
            }
        }

        // Whatever the walk did not strike off is a pocket with no row yet. The validator has
        // already established that every one of them is inside the claim.
        foreach (var (index, card) in posted)
        {
            context.BinderCards.Add(new PlacedCardEntity
            {
                BinderId = request.BinderId,
                CardId = card.CardId,
                IndexInBinder = index,
                IsMissing = card.IsMissing,
            });

            placed++;
        }

        return (placed, changed, removed);
    }

    /// <summary>
    /// Diffs the whole tray against what is stored and queues the difference. Nothing is written
    /// here: the caller commits both halves together.
    /// </summary>
    private static async Task<(int Added, int Updated, int Removed)> SaveTrayAsync(
        Request request,
        BinderDbContext context,
        CancellationToken ct)
    {
        var stored = await context.BinderTray
            .Where(entry => entry.BinderId == request.BinderId)
            .ToListAsync(ct);

        // Copied into a dictionary so the walk below can strike off the cards it has seen and be
        // left holding exactly the new ones.
        var posted = request.Tray.ToDictionary(card => card.CardId, card => card.Quantity);

        var added = 0;
        var updated = 0;
        var removed = 0;

        // Walked as a difference rather than deleting the tray and writing it back, because the
        // common case is a tick where one card changed and the rest of the tray did not. A blanket
        // rewrite would churn every row on every tick.
        foreach (var entry in stored)
        {
            if (!posted.Remove(entry.CardId, out var quantity))
            {
                context.BinderTray.Remove(entry);
                removed++;
                continue;
            }

            if (entry.Quantity != quantity)
            {
                entry.Quantity = quantity;
                updated++;
            }
        }

        foreach (var (cardId, quantity) in posted)
        {
            context.BinderTray.Add(new TrayEntry
            {
                BinderId = request.BinderId,
                CardId = cardId,
                Quantity = quantity,
            });

            added++;
        }

        return (added, updated, removed);
    }
}
