using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;
using PokeBinder.Features.Binder.Shared.Arrangement;
using PokeBinder.Features.Binder.ReorderBinder.Models;
using PokeBinder.TcgCatalog.DbContext;

namespace PokeBinder.Features.Binder.ReorderBinder;

/// <summary>
/// Sorts every card placed in a binder and lays them out again from the first pocket, with no gaps.
/// How the criteria compose, and why the sort runs in memory, is BinderArrangement's: UpdateBinder
/// sorts the same way when a resize moves the cards, so the sort lives there once.
///
/// <para>
/// The tray takes no part: it is what has not been placed yet. The whole write is one
/// <c>SaveChangesAsync</c>, and the response is the binder's new order, so the client can show it
/// without asking again -- and record it for undo, since the arrangement this replaced is gone from
/// the server once this returns.
/// </para>
/// </summary>
public static class ReorderBinder
{
    /// <summary>One, two or three criteria: Set, Rarity and Card name, each at most once.</summary>
    public const int MaxCriteria = SortCriteriaRules.MaxCriteria;

    public record Request
    {
        public int BinderId { get; init; }

        /// <summary>Most significant first. Each one only breaks the ties of the ones above it.</summary>
        public IReadOnlyList<SortCriterion> Criteria { get; init; } = [];
    }

    /// <param name="Cards">
    /// Every placed card in its new pocket, from pocket zero up with no gaps. Pockets past the last
    /// one are empty.
    /// </param>
    public record Response(int BinderId, IReadOnlyList<ReorderedCard> Cards);

    /// <param name="userId">
    /// The signed-in user. ReorderBinderValidator has already established that this binder is
    /// theirs; the read below is scoped by it anyway, so a handler called without its validator
    /// cannot rearrange another user's binder.
    /// </param>
    public static async Task<Response> Handler(
        Request request,
        int userId,
        BinderDbContext binderContext,
        TcgCatalogDbContext catalogContext,
        CancellationToken ct = default)
    {
        await binderContext.Binders
            .Where(binder => binder.Id == request.BinderId && binder.UserId == userId)
            .Select(binder => binder.Id)
            .FirstAsync(ct);

        var stored = await binderContext.BinderCards
            .Where(card => card.BinderId == request.BinderId)
            .OrderBy(card => card.IndexInBinder)
            .ToListAsync(ct);

        var sorted = await BinderArrangement.SortAsync(stored, request.Criteria, catalogContext, ct);

        var reordered = BinderArrangement.LayOutFromFirstPocket(stored, sorted, request.BinderId, binderContext);

        await binderContext.SaveChangesAsync(ct);

        return new Response(request.BinderId, reordered);
    }
}
