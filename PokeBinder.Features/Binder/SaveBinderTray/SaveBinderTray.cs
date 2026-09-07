using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;
using PokeBinder.Features.Binder.SaveBinderTray.Models;
using TrayEntry = PokeBinder.Binders.DbContext.Entities.BinderTray;

namespace PokeBinder.Features.Binder.SaveBinderTray;

/// <summary>
/// Stores a binder's card tray as the front end currently holds it.
///
/// The request is a snapshot, not a delta: it carries the whole tray, so a card the user took out
/// is one the request leaves out, and storing it removes that row. That is what makes the slice
/// safe behind a debounce -- a tick that fires twice, a retry after a dropped connection, and a
/// reconnect after the tab slept all write the same thing, and a client that has drifted is put
/// back in step by the next tick rather than compounding. A delta would need every request
/// delivered exactly once, forever, to stay correct.
///
/// The tray is scoped to the binder, which is what gives each binder its own staging area. There
/// is no tray row to create first: a tray is only its entries.
/// </summary>
public static class SaveBinderTray
{
    /// <summary>
    /// Cards one tray may hold. A tray stages the cards being placed right now, so this sits far
    /// above any real use; it is here so that a client bug cannot ask for an unbounded write.
    /// </summary>
    public const int MaxCards = 500;

    /// <summary>Copies of a single card. Same reasoning as <see cref="MaxCards"/>.</summary>
    public const int MaxQuantity = 999;

    public record Request
    {
        /// <summary>The binder whose tray this is.</summary>
        public int BinderId { get; init; }

        /// <summary>
        /// The tray in full. An empty list is meaningful and allowed: the user emptied the tray,
        /// and the stored one is emptied to match.
        /// </summary>
        public IReadOnlyList<TrayCard> Cards { get; init; } = [];
    }

    /// <param name="Added">Cards that were not in the stored tray.</param>
    /// <param name="Updated">Cards whose quantity changed.</param>
    /// <param name="Removed">Cards the request left out.</param>
    /// <param name="TrayCards">Distinct cards in the tray afterwards.</param>
    public record Response(int BinderId, int Added, int Updated, int Removed, int TrayCards);

    /// <param name="userId">
    /// The signed-in user. SaveBinderTrayValidator has already established that this binder is
    /// theirs, so nothing here re-checks it.
    /// </param>
    public static async Task<Response> Handler(
        Request request,
        int userId,
        BinderDbContext context,
        CancellationToken ct = default)
    {
        var stored = await context.BinderTray
            .Where(entry => entry.BinderId == request.BinderId)
            .ToListAsync(ct);

        // Copied into a dictionary so the walk below can strike off the cards it has seen and be
        // left holding exactly the new ones.
        var posted = request.Cards.ToDictionary(card => card.CardId, card => card.Quantity);

        var added = 0;
        var updated = 0;
        var removed = 0;

        // Walked as a difference rather than deleting the tray and writing it back, because the
        // common case is a debounce tick where one card changed and the rest of the tray did not.
        // A blanket rewrite would churn every row on every tick.
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

        // Most ticks change nothing; those cost a read and no write.
        if (added > 0 || updated > 0 || removed > 0)
        {
            await context.SaveChangesAsync(ct);
        }

        return new Response(request.BinderId, added, updated, removed, request.Cards.Count);
    }
}
