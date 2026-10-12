using PokeBinder.Features.CardSearch.GetSetRarityCounts;
using PokeBinder.Features.Tests.Binder;

namespace PokeBinder.Features.Tests.CardSearch;

/// <summary>What GetSetRarityCounts answers. BinderFixture for its catalog helpers; the binder side is unused.</summary>
public class GetSetRarityCountsTests
{
    [Fact]
    public async Task Counts_each_rarity_commonest_first_with_unweighted_ones_last()
    {
        using var fixture = new BinderFixture()
            .WithSet(5, "New Set", releaseDateUnix: 1_700_000_000)
            .WithSet(6, "Other Set", releaseDateUnix: 1_700_000_000)
            .WithRarity(5, "Common", pullRate: 1)
            .WithRarity(5, "Ultra Rare", pullRate: 80)
            .WithCard(1, 5, "Ultra Rare", "Gengar", "4")
            .WithCard(2, 5, "Common", "Pidgey", "10")
            .WithCard(3, 5, "Common", "Eevee", "1")
            .WithCard(4, 5, "Promo", "Mew", "SV1")
            .WithCard(5, 6, "Common", "Ratata", "5");

        var response = await GetSetRarityCounts.Handler(new GetSetRarityCounts.Request(5), fixture.Catalog);

        // Counted from the cards, so a rarity with no weight row is still there, last.
        Assert.Equal(
            [("Common", 2, (int?)1), ("Ultra Rare", 1, 80), ("Promo", 1, null)],
            response.Rarities.Select(r => (r.Rarity, r.Count, r.PullRateRarityOrder)));
    }

    [Fact]
    public async Task An_unknown_set_has_no_rarities()
    {
        using var fixture = new BinderFixture();

        var response = await GetSetRarityCounts.Handler(new GetSetRarityCounts.Request(99), fixture.Catalog);

        Assert.Empty(response.Rarities);
    }
}
