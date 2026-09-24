using Microsoft.EntityFrameworkCore;
using PokeBinder.Features.CardAdmin.GetRarityBySetForAdminEdit.Models;
using PokeBinder.TcgCatalog.DbContext;

namespace PokeBinder.Features.CardAdmin.GetRarityBySetForAdminEdit;

/// <summary>
/// One set's rarities and what they are currently worth, for the editor at /admin/setRarity.
/// Read-only; writing the values back is <see cref="UpdateRarityBySet"/>.
///
/// <para>
/// It answers with the set list as well as the chosen set's rows, because the page and the htmx
/// fragment it swaps in are the same question asked twice -- "which sets are there, and what does
/// this one look like?" -- and a second slice for the picker would be a second place for the sets
/// to be ordered differently. Fifty-three rows is not a query worth splitting.
/// </para>
///
/// <para>
/// <b>Every rarity the set has comes back, weighted or not.</b> A row nobody has weighted is the
/// normal state, not an omission: the columns ship empty and 53 sets are filled in by hand over
/// time, so hiding the unweighted rows would hide exactly the work this page exists for.
/// </para>
/// </summary>
public static class GetRarityBySetForAdminEdit
{
    /// <param name="SetId">
    /// The set to load, or null for the page's first render -- nothing is chosen yet, so the
    /// picker comes back and the rows do not. An id that is not a set answers the same way: the
    /// picker is the only honest thing to show somebody who named a set that is not there.
    /// </param>
    public record Request(int? SetId = null);

    /// <param name="Sets">Every set, newest first, however this was called.</param>
    /// <param name="Selected">
    /// The chosen set and its rarities, or null when none was chosen or the id was not a set.
    /// </param>
    public record Response(IReadOnlyList<RaritySetChoice> Sets, SelectedSet? Selected);

    /// <param name="Rarities">
    /// Every rarity the set contains, rarest first: by pull rate, then by name order, then by
    /// name. Pull rate leads because it is the truer answer to "how rare is this here?" -- the
    /// name order is a ranking of the names and says nothing about this set's generosity -- and
    /// reading the set in that order is how somebody sees at a glance that a number is out of
    /// place.
    /// <para>
    /// Unweighted rows need no rule of their own: an empty value sorts last in a descending
    /// ordering, so rows with no pull rate fall below the ones that have one, and rows with
    /// neither value settle at the foot in name order.
    /// </para>
    /// </param>
    public record SelectedSet(int SetId, string SetName, string SetCode, IReadOnlyList<RarityWeightRow> Rarities);

    public static async Task<Response> Handler(
        Request request,
        TcgCatalogDbContext context,
        CancellationToken ct = default)
    {
        var sets = await LoadSetsAsync(context, ct);

        if (request.SetId is not int setId)
        {
            return new Response(sets, null);
        }

        var set = await context.Sets
            .AsNoTracking()
            .Where(candidate => candidate.Id == setId)
            .Select(candidate => new { candidate.Id, candidate.Name, candidate.Code })
            .FirstOrDefaultAsync(ct);

        if (set is null)
        {
            return new Response(sets, null);
        }

        var rarities = await context.RarityBySetFilterOptions
            .AsNoTracking()
            .Where(rarity => rarity.SetId == setId)
            .Select(rarity => new RarityWeightRow
            {
                Id = rarity.Id,
                Rarity = rarity.Rarity,
                PullRateRarityOrder = rarity.PullRateRarityOrder,
                NameRarityOrder = rarity.NameRarityOrder,
            })
            .ToListAsync(ct);

        // Counted in one grouped read rather than one query per rarity. Matched on the rarity
        // string, which is what the filter rows are keyed by: a card whose rarity is not among
        // them is a card no filter can reach, and it counts towards nothing here either.
        var cardCounts = await context.Cards
            .AsNoTracking()
            .Where(card => card.SetId == setId && card.Rarity != null)
            .GroupBy(card => card.Rarity!)
            .Select(group => new { Rarity = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.Rarity, row => row.Count, ct);

        foreach (var rarity in rarities)
        {
            rarity.CardCount = cardCounts.TryGetValue(rarity.Rarity, out var count) ? count : 0;
        }

        // Rarest first, pull rate leading. Sorted here rather than in the query because the card
        // counts above are joined in memory anyway, and because a null sorts last in a descending
        // ordering here -- which is the behaviour this relies on to keep the unweighted rows at
        // the foot -- while SQL would have put them first.
        var ordered = rarities
            .OrderByDescending(rarity => rarity.PullRateRarityOrder)
            .ThenByDescending(rarity => rarity.NameRarityOrder)
            .ThenBy(rarity => rarity.Rarity)
            .ToList();

        return new Response(sets, new SelectedSet(set.Id, set.Name, set.Code, ordered));
    }

    /// <summary>
    /// The picker, newest set first -- a set is weighted soon after it is loaded, so the one being
    /// looked for is nearly always at the top -- with how much of each one is still to do.
    /// </summary>
    private static async Task<List<RaritySetChoice>> LoadSetsAsync(TcgCatalogDbContext context, CancellationToken ct) =>
        await context.Sets
            .AsNoTracking()
            .OrderByDescending(set => set.ReleaseDateUnix)
            .ThenBy(set => set.Name)
            .Select(set => new RaritySetChoice
            {
                Id = set.Id,
                Name = set.Name,
                Code = set.Code,
                UnweightedCount = context.RarityBySetFilterOptions
                    .Count(rarity => rarity.SetId == set.Id
                        && rarity.PullRateRarityOrder == null
                        && rarity.NameRarityOrder == null),
            })
            .ToListAsync(ct);
}
