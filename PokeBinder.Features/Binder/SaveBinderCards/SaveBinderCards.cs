using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;
using PokeBinder.Features.Binder.SaveBinderCards.Models;
using PlacedCardEntity = PokeBinder.Binders.DbContext.Entities.BinderCard;

namespace PokeBinder.Features.Binder.SaveBinderCards;

/// <summary>
/// Stores where the cards sit in a binder.
///
/// Like SaveBinderTray, the request is a snapshot rather than a delta: it carries every placed
/// card, so a pocket the user emptied is one the request leaves out. That is what makes it safe to
/// repeat -- a debounce that fires twice, a retry after a dropped connection, and a reconnect after
/// a sleeping tab all write the same thing.
///
/// The cost of that is a rule the caller has to keep: <b>send the whole binder, not the page being
/// edited</b>. A request carrying only page two's cards says every other page is empty, and the
/// handler will believe it. GetFullBinder exists so the client has the whole thing to send back.
///
/// The tray is not touched here. Placing a card spends a copy from it, but that is SaveBinderTray's
/// row to write, and a client that moves cards about within a page changes no tray at all.
/// </summary>
public static class SaveBinderCards
{
    /// <summary>
    /// Pockets one request may fill. Above any real binder -- the largest seeded grid at the page
    /// ceiling is 20 x 200 -- and here so that a client bug cannot ask for an unbounded write.
    /// </summary>
    public const int MaxCards = 4000;

    public record Request
    {
        public int BinderId { get; init; }

        /// <summary>
        /// Every card in the binder, in any order. An empty list is meaningful and allowed: the
        /// user cleared the binder, and the stored cards are cleared to match.
        /// </summary>
        public IReadOnlyList<PlacedCard> Cards { get; init; } = [];
    }

    /// <param name="Placed">Pockets that were empty and now hold a card.</param>
    /// <param name="Changed">
    /// Pockets that held a card and now hold a different one, or the same one with its missing flag
    /// flipped. A card dragged from one pocket to another is not this: the row is keyed by pocket,
    /// so that reads as one Removed and one Placed.
    /// </param>
    /// <param name="Removed">Pockets the request left empty.</param>
    /// <param name="CardCount">Cards in the binder afterwards.</param>
    public record Response(int BinderId, int Placed, int Changed, int Removed, int CardCount);

    /// <param name="userId">
    /// The signed-in user. SaveBinderCardsValidator has already established that this binder is
    /// theirs, so nothing here re-checks it.
    /// </param>
    public static async Task<Response> Handler(
        Request request,
        int userId,
        BinderDbContext context,
        CancellationToken ct = default)
    {
        var stored = await context.BinderCards
            .Where(card => card.BinderId == request.BinderId)
            .ToListAsync(ct);

        // Keyed by pocket, because that is the row's identity: the primary key is the binder and
        // the index, so the same card in two pockets is two rows and one pocket holds one card.
        var posted = request.Cards.ToDictionary(card => card.IndexInBinder);

        var placed = 0;
        var changed = 0;
        var removed = 0;

        // Walked as a difference rather than deleting the binder's cards and writing them back: a
        // debounce tick usually carries one card that moved and several hundred that did not.
        foreach (var entry in stored)
        {
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

        // Most ticks change nothing; those cost a read and no write.
        if (placed > 0 || changed > 0 || removed > 0)
        {
            await context.SaveChangesAsync(ct);
        }

        return new Response(request.BinderId, placed, changed, removed, request.Cards.Count);
    }
}
