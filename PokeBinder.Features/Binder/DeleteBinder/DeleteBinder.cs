using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;

namespace PokeBinder.Features.Binder.DeleteBinder;

/// <summary>
/// Deletes one of the user's binders, and everything that belongs to it: the cards placed in its
/// pockets and the cards waiting in its tray. Neither means anything without the binder -- both are
/// keyed by its id -- so they go with it rather than being left behind as rows nothing can reach.
///
/// <para>
/// <b>The children are removed explicitly.</b> The schema's foreign keys from <c>binderCards</c> and
/// <c>binderTray</c> to <c>binder</c> have no <c>ON DELETE CASCADE</c>, so deleting the binder row
/// alone would either fail on the constraint or strand the rows, depending on whether the
/// connection enforces foreign keys. Loading them and removing them in the same unit of work makes
/// it one <c>SaveChangesAsync</c>: one transaction, children first, and nothing half-deleted if it
/// fails.
/// </para>
///
/// <para>
/// There is no undo. The workspace asks first, and leaves for My Binders once this returns.
/// </para>
/// </summary>
public static class DeleteBinder
{
    public record Request
    {
        public int BinderId { get; init; }
    }

    /// <param name="userId">
    /// The signed-in user. DeleteBinderValidator has already established that the binder is
    /// theirs; every read below is scoped by it anyway, so a handler called without its validator
    /// cannot delete another user's binder.
    /// </param>
    public static async Task Handler(
        Request request,
        int userId,
        BinderDbContext context,
        CancellationToken ct = default)
    {
        var binder = await context.Binders
            .FirstAsync(b => b.Id == request.BinderId && b.UserId == userId, ct);

        var placed = await context.BinderCards
            .Where(card => card.BinderId == binder.Id)
            .ToListAsync(ct);

        var tray = await context.BinderTray
            .Where(entry => entry.BinderId == binder.Id)
            .ToListAsync(ct);

        context.BinderCards.RemoveRange(placed);
        context.BinderTray.RemoveRange(tray);
        context.Binders.Remove(binder);

        await context.SaveChangesAsync(ct);
    }
}
