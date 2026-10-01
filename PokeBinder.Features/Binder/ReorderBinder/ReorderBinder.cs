using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;
using PokeBinder.Features.Binder.ReorderBinder.Models;
using PokeBinder.TcgCatalog.DbContext;
using PlacedCardEntity = PokeBinder.Binders.DbContext.Entities.BinderCard;

namespace PokeBinder.Features.Binder.ReorderBinder;

/// <summary>
/// Sorts every card placed in a binder and lays them out again from the first pocket, with no gaps.
///
/// <para>
/// <b>The criteria compose the way <c>ORDER BY a, b, c</c> does.</b> The first one orders the whole
/// binder; the second only orders cards the first left level, and so on. "Set newest first, then
/// Rarity rare first" is therefore the newest set's cards, rarest to commonest, then the next set's
/// cards, rarest to commonest again -- never a binder-wide rarity order with the sets mixed in.
/// </para>
///
/// <para>
/// It sorts in memory rather than in SQL because the two halves of a row live in two databases: the
/// pockets are the binder's, and everything a criterion reads -- the set's release date, the
/// rarity's weight in that set, the name -- is the catalog's, with no join between them. A binder
/// is a few thousand pockets at most, so reading both and sorting here costs nothing, and it is
/// what lets the card number compare naturally ("4" before "10"), which SQLite's text ordering
/// cannot.
/// </para>
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
    public const int MaxCriteria = 3;

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

    /// <summary>What a criterion can read about one placed card. Null wherever the catalog has no answer.</summary>
    private sealed record Row(
        int IndexInBinder,
        int CardId,
        bool IsMissing,
        int? SetId,
        string? SetName,
        long? ReleaseDateUnix,
        int? PullRate,
        int? NameOrder,
        string? Name,
        string? CardNumber);

    // Case and accents do not split names a collector reads as the same.
    private static readonly StringComparer NameComparer =
        StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);

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

        // In pocket order, so the stable sort below leaves two copies of one card in the order they
        // already had rather than an arbitrary one.
        var stored = await binderContext.BinderCards
            .Where(card => card.BinderId == request.BinderId)
            .OrderBy(card => card.IndexInBinder)
            .ToListAsync(ct);

        var rows = await ReadRowsAsync(stored, catalogContext, ct);

        // Captured as values before anything is written: the rows are read off the same entities
        // the write below changes.
        var reordered = Order(rows, request.Criteria)
            .Select((row, index) => new ReorderedCard(index, row.CardId, row.IsMissing))
            .ToList();

        Write(stored, reordered, request.BinderId, binderContext);

        await binderContext.SaveChangesAsync(ct);

        return new Response(request.BinderId, reordered);
    }

    /// <summary>
    /// Joins each pocket to what the catalog knows about its card. Two queries for the whole
    /// binder, not one per card: the cards, then the weights of every rarity in the sets they come
    /// from.
    /// </summary>
    private static async Task<List<Row>> ReadRowsAsync(
        List<PlacedCardEntity> stored,
        TcgCatalogDbContext catalogContext,
        CancellationToken ct)
    {
        var cardIds = stored.Select(card => card.CardId).Distinct().ToList();

        var cards = await catalogContext.Cards
            .Where(card => cardIds.Contains(card.Id))
            .Select(card => new
            {
                card.Id,
                card.SetId,
                SetName = card.Set != null ? card.Set.Name : null,
                ReleaseDateUnix = card.Set != null ? card.Set.ReleaseDateUnix : (long?)null,
                card.Rarity,
                card.Name,
                card.CardNumber,
            })
            .ToDictionaryAsync(card => card.Id, ct);

        var setIds = cards.Values
            .Where(card => card.SetId is not null)
            .Select(card => card.SetId!.Value)
            .Distinct()
            .ToList();

        var weightRows = await catalogContext.RarityBySetFilterOptions
            .Where(row => setIds.Contains(row.SetId))
            .Select(row => new { row.SetId, row.Rarity, row.PullRateRarityOrder, row.NameRarityOrder })
            .ToListAsync(ct);

        // First row wins rather than ToDictionary: nothing in the model makes (setId, rarity)
        // unique, and a duplicate row is a catalog fault that should not make a reorder a 500.
        var weights = new Dictionary<(int, string), (int? PullRate, int? NameOrder)>();

        foreach (var row in weightRows)
        {
            weights.TryAdd((row.SetId, row.Rarity), (row.PullRateRarityOrder, row.NameRarityOrder));
        }

        return stored
            .Select(pocket =>
            {
                // A card the catalog no longer has still sits in the binder. It has nothing to sort
                // by, so every criterion puts it last, and it is kept rather than dropped.
                if (!cards.TryGetValue(pocket.CardId, out var card))
                {
                    return new Row(pocket.IndexInBinder, pocket.CardId, pocket.IsMissing ?? false,
                        null, null, null, null, null, null, null);
                }

                var weight = card.SetId is not null && card.Rarity is not null
                    && weights.TryGetValue((card.SetId.Value, card.Rarity), out var found)
                        ? found
                        : (PullRate: null, NameOrder: null);

                return new Row(
                    pocket.IndexInBinder,
                    pocket.CardId,
                    pocket.IsMissing ?? false,
                    card.SetId,
                    card.SetName,
                    card.ReleaseDateUnix,
                    weight.PullRate,
                    weight.NameOrder,
                    card.Name,
                    card.CardNumber);
            })
            .ToList();
    }

    /// <summary>
    /// The criteria as one <c>ORDER BY</c>: a <c>ThenBy</c> per criterion, most significant first,
    /// and the tie rules after the last of them.
    ///
    /// <para>
    /// <b>An absent value sorts last in both directions.</b> A rarity nobody has weighted is not the
    /// commonest or the rarest, it is unknown, and flipping the direction must not carry it from the
    /// back of the binder to the front -- which is what keeps the EX-era Secret Rares and the promo
    /// rows from landing among the commons under "rare first".
    /// </para>
    ///
    /// <para>
    /// <b>Ties.</b> Whatever the criteria leave level is ordered by card number, ascending and
    /// natural, with a blank number last, then by card id. Nothing about where a card sits now takes
    /// part, so the same criteria always produce the same binder; only two copies of one card are
    /// left level, and they keep the order they had.
    /// </para>
    /// </summary>
    private static IEnumerable<Row> Order(List<Row> rows, IReadOnlyList<SortCriterion> criteria)
    {
        // A no-op first key, so every criterion below is a ThenBy and none of them is special.
        var ordered = rows.OrderBy(_ => 0);

        foreach (var criterion in criteria)
        {
            ordered = criterion.Key switch
            {
                // Two sets released the same day are still two sets, and this criterion is what
                // keeps a set's cards together: left to the next criterion, their cards would
                // interleave. Set name A-Z in either direction breaks that tie, since a shared date
                // has no newer end, and the id only when the names match too.
                CriterionKey.Set => ordered
                    .ThenByNullsLast(row => row.ReleaseDateUnix, criterion.Direction)
                    .ThenByNullsLast(row => row.SetName, SortDirection.Asc, NameComparer)
                    .ThenByNullsLast(row => row.SetId, SortDirection.Asc),

                // Equal weights are a tie on purpose -- every base rarity is 1 -- so they fall
                // through to the next criterion rather than being split here.
                CriterionKey.Rarity => ordered.ThenByNullsLast(
                    row => criterion.RarityKey == RarityKey.Name ? row.NameOrder : row.PullRate,
                    criterion.Direction),

                CriterionKey.Name => ordered.ThenByNullsLast(row => row.Name, criterion.Direction, NameComparer),

                _ => ordered,
            };
        }

        return ordered
            .ThenByNullsLast(row => row.CardNumber, SortDirection.Asc, NaturalComparer.Instance)
            .ThenBy(row => row.CardId);
    }

    /// <summary>One key of the <c>ORDER BY</c>: its absent values last whichever way it runs.</summary>
    private static IOrderedEnumerable<Row> ThenByNullsLast<TKey>(
        this IOrderedEnumerable<Row> ordered,
        Func<Row, TKey?> key,
        SortDirection direction,
        IComparer<TKey?>? comparer = null)
    {
        var present = ordered.ThenBy(row => key(row) is null);

        return direction == SortDirection.Desc
            ? present.ThenByDescending(key, comparer)
            : present.ThenBy(key, comparer);
    }

    /// <summary>
    /// Rewrites the pockets as the new order: pocket <c>n</c> gets the <c>n</c>th card, and every
    /// pocket past the last card is emptied.
    ///
    /// <para>
    /// A diff by pocket rather than delete-everything-and-insert. The row's key is the binder and
    /// the pocket, so a pocket that holds a card before and after is the same row either way, and
    /// removing and re-adding one key in a single unit of work is something EF refuses to track.
    /// </para>
    /// </summary>
    private static void Write(
        List<PlacedCardEntity> stored,
        List<ReorderedCard> reordered,
        int binderId,
        BinderDbContext context)
    {
        var byPocket = stored.ToDictionary(card => card.IndexInBinder);

        foreach (var card in reordered)
        {
            if (byPocket.Remove(card.IndexInBinder, out var entry))
            {
                if (entry.CardId != card.CardId || (entry.IsMissing ?? false) != card.IsMissing)
                {
                    entry.CardId = card.CardId;
                    entry.IsMissing = card.IsMissing;
                }

                continue;
            }

            context.BinderCards.Add(new PlacedCardEntity
            {
                BinderId = binderId,
                CardId = card.CardId,
                IndexInBinder = card.IndexInBinder,
                IsMissing = card.IsMissing,
            });
        }

        // Whatever is left held a card before and sits past the last one now.
        context.BinderCards.RemoveRange(byPocket.Values);
    }

    /// <summary>
    /// Compares card numbers the way they are printed and read: runs of digits by value, so "4"
    /// comes before "10" and "SV9" before "SV49", and everything else case-insensitively.
    /// </summary>
    private sealed class NaturalComparer : IComparer<string?>
    {
        public static readonly NaturalComparer Instance = new();

        public int Compare(string? left, string? right)
        {
            if (left is null || right is null)
            {
                return left is null ? (right is null ? 0 : 1) : -1;
            }

            var i = 0;
            var j = 0;

            while (i < left.Length && j < right.Length)
            {
                if (char.IsAsciiDigit(left[i]) && char.IsAsciiDigit(right[j]))
                {
                    var leftStart = i;
                    var rightStart = j;

                    while (i < left.Length && char.IsAsciiDigit(left[i])) i++;
                    while (j < right.Length && char.IsAsciiDigit(right[j])) j++;

                    // By value without parsing, so a run too long for a long still compares: strip
                    // the leading zeros, then the longer run is the bigger number.
                    var leftRun = left.AsSpan(leftStart, i - leftStart).TrimStart('0');
                    var rightRun = right.AsSpan(rightStart, j - rightStart).TrimStart('0');

                    var byValue = leftRun.Length != rightRun.Length
                        ? leftRun.Length.CompareTo(rightRun.Length)
                        : leftRun.SequenceCompareTo(rightRun);

                    if (byValue != 0) return byValue;

                    continue;
                }

                var byChar = char.ToUpperInvariant(left[i]).CompareTo(char.ToUpperInvariant(right[j]));

                if (byChar != 0) return byChar;

                i++;
                j++;
            }

            return (left.Length - i).CompareTo(right.Length - j);
        }
    }
}
