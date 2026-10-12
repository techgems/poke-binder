using Microsoft.EntityFrameworkCore;
using PokeBinder.TcgCatalog.DbContext;

namespace PokeBinder.Features.CardSearch.GetSetRarityCounts;

/// <summary>
/// How many cards one set has of each rarity. What the add-a-set wizard needs before it sends
/// anything: whether the set fits the binder, and what leaving a rarity out takes off it.
///
/// <para>
/// Counted from the cards rather than read off <c>rarityBySetFilterOption</c>, whose rows say
/// which rarities a set is filtered by, not what it holds -- a rarity with cards and no row is
/// still cards that go in. The row's pull-rate weight comes along where there is one, so the
/// wizard can list the rarities commonest first without a second lookup.
/// </para>
/// </summary>
public static class GetSetRarityCounts
{
    public record Request(int SetId);

    /// <param name="Rarities">Commonest first; a rarity nobody has weighted last, by name.</param>
    public record Response(int SetId, IReadOnlyList<RarityCount> Rarities);

    /// <param name="Rarity">Null for cards the catalog has no rarity for.</param>
    /// <param name="PullRateRarityOrder">The rarity's weight in this set, or null when it has none.</param>
    public record RarityCount(string? Rarity, int Count, int? PullRateRarityOrder);

    /// <summary>An unknown set answers with no rarities, the same as a set with no cards.</summary>
    public static async Task<Response> Handler(
        Request request,
        TcgCatalogDbContext context,
        CancellationToken ct = default)
    {
        var counts = await context.Cards
            .Where(card => card.SetId == request.SetId)
            .GroupBy(card => card.Rarity)
            .Select(group => new { Rarity = group.Key, Count = group.Count() })
            .ToListAsync(ct);

        var weights = await context.RarityBySetFilterOptions
            .Where(row => row.SetId == request.SetId)
            .Select(row => new { row.Rarity, row.PullRateRarityOrder })
            .ToListAsync(ct);

        var rarities = counts
            .Select(count => new RarityCount(
                count.Rarity,
                count.Count,
                weights.FirstOrDefault(weight => weight.Rarity == count.Rarity)?.PullRateRarityOrder))
            .OrderBy(rarity => rarity.PullRateRarityOrder is null)
            .ThenBy(rarity => rarity.PullRateRarityOrder)
            .ThenBy(rarity => rarity.Rarity, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new Response(request.SetId, rarities);
    }
}
