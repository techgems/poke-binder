using PokeBinder.Features.CardAdmin.GetRarityBySetForAdminEdit;

namespace PokeBinder.Features.Tests.CardAdmin;

/// <summary>
/// What the rarity-weight editor is given to draw itself with: the set picker, one set's rarities,
/// and the card counts that make a weight guessable.
/// </summary>
public class GetRarityBySetForAdminEditTests
{
    private static Task<GetRarityBySetForAdminEdit.Response> HandleAsync(
        RarityWeightFixture fixture,
        int? setId = null) =>
        GetRarityBySetForAdminEdit.Handler(new GetRarityBySetForAdminEdit.Request(setId), fixture.Context);

    [Fact]
    public async Task No_set_chosen_gives_the_picker_and_no_rows()
    {
        using var fixture = new RarityWeightFixture().WithCatalog();

        var response = await HandleAsync(fixture);

        Assert.Equal(3, response.Sets.Count);
        Assert.Null(response.Selected);
    }

    /// <summary>
    /// A set that is not there answers like no set at all. The picker is the only honest thing to
    /// show somebody who named one that has gone.
    /// </summary>
    [Fact]
    public async Task An_unknown_set_gives_the_picker_and_no_rows()
    {
        using var fixture = new RarityWeightFixture().WithCatalog();

        var response = await HandleAsync(fixture, 999);

        Assert.NotEmpty(response.Sets);
        Assert.Null(response.Selected);
    }

    /// <summary>
    /// Every rarity the set has, weighted or not. Hiding the unweighted ones would hide exactly
    /// the work this page exists for.
    /// </summary>
    [Fact]
    public async Task An_unweighted_set_still_lists_all_of_its_rarities()
    {
        using var fixture = new RarityWeightFixture().WithCatalog();

        var response = await HandleAsync(fixture, RarityWeightFixture.AscendedHeroesId);

        Assert.NotNull(response.Selected);
        Assert.Equal(3, response.Selected.Rarities.Count);
        Assert.All(response.Selected.Rarities, rarity => Assert.True(rarity.IsUnweighted));
    }

    [Fact]
    public async Task Only_the_chosen_sets_rarities_come_back()
    {
        using var fixture = new RarityWeightFixture().WithCatalog();

        var response = await HandleAsync(fixture, RarityWeightFixture.AnniversaryId);

        Assert.NotNull(response.Selected);
        Assert.Equal(
            ["Common", "Special Illustration Rare"],
            response.Selected.Rarities.Select(rarity => rarity.Rarity).Order());
    }

    /// <summary>
    /// The card count is the context somebody types a weight against: two cards out of a set is a
    /// hint that pulling one is hard.
    /// </summary>
    [Fact]
    public async Task Each_rarity_carries_how_many_cards_of_it_the_set_holds()
    {
        using var fixture = new RarityWeightFixture().WithCatalog();

        var response = await HandleAsync(fixture, RarityWeightFixture.AscendedHeroesId);

        var counts = response.Selected!.Rarities.ToDictionary(rarity => rarity.Rarity, rarity => rarity.CardCount);

        Assert.Equal(2, counts["Common"]);
        Assert.Equal(1, counts["Special Illustration Rare"]);

        // A rarity the set is filtered by with no cards behind it. Zero rather than missing: the
        // row is still editable, and the count is the thing that makes it look wrong.
        Assert.Equal(0, counts["Mega Hyper Rare"]);
    }

    [Fact]
    public async Task Stored_weights_come_back_as_they_were_saved()
    {
        using var fixture = new RarityWeightFixture()
            .WithCatalog()
            .WithWeights((2, 70, 8));

        var response = await HandleAsync(fixture, RarityWeightFixture.AscendedHeroesId);

        var row = response.Selected!.Rarities.Single(rarity => rarity.Id == 2);

        Assert.Equal(70, row.PullRateRarityOrder);
        Assert.Equal(8, row.NameRarityOrder);
        Assert.False(row.IsUnweighted);
    }

    /// <summary>
    /// Rarest first, with the unweighted rows kept together at the foot rather than mixed in as
    /// if they were commons.
    /// </summary>
    [Fact]
    public async Task Weighted_rarities_are_listed_rarest_first_and_unweighted_rows_come_last()
    {
        using var fixture = new RarityWeightFixture()
            .WithCatalog()
            .WithWeights((1, 1, 1), (3, 540, 9));

        var response = await HandleAsync(fixture, RarityWeightFixture.AscendedHeroesId);

        Assert.Equal(
            ["Mega Hyper Rare", "Common", "Special Illustration Rare"],
            response.Selected!.Rarities.Select(rarity => rarity.Rarity));
    }

    /// <summary>
    /// Pull rate leads where the two disagree. It is the truer answer to how rare a card is in
    /// this set -- the name order ranks the names and says nothing about this set's generosity --
    /// so a set that is unusually kind about one of its top rarities reads that way here.
    /// </summary>
    [Fact]
    public async Task Pull_rate_decides_the_order_before_name_order_does()
    {
        using var fixture = new RarityWeightFixture()
            .WithCatalog()
            // The Mega Hyper Rare is named the rarer of the two, and this set gives it up twice as
            // easily as its Special Illustration Rare.
            .WithWeights((2, 100, 8), (3, 50, 9));

        var response = await HandleAsync(fixture, RarityWeightFixture.AscendedHeroesId);

        Assert.Equal(
            ["Special Illustration Rare", "Mega Hyper Rare", "Common"],
            response.Selected!.Rarities.Select(rarity => rarity.Rarity));
    }

    /// <summary>
    /// A row with a name order and no pull rate still outranks one with neither, so the foot of
    /// the list is the rows nothing is known about rather than a mix.
    /// </summary>
    [Fact]
    public async Task A_row_with_only_a_name_order_sits_below_the_pull_rated_rows_and_above_the_bare_ones()
    {
        using var fixture = new RarityWeightFixture()
            .WithCatalog()
            .WithWeights((3, 540, 9), (2, null, 8));

        var response = await HandleAsync(fixture, RarityWeightFixture.AscendedHeroesId);

        Assert.Equal(
            ["Mega Hyper Rare", "Special Illustration Rare", "Common"],
            response.Selected!.Rarities.Select(rarity => rarity.Rarity));
    }

    /// <summary>
    /// The picker is a worklist: 53 sets are filled in by hand over time, and what the person
    /// sitting down to it needs to know is which are still untouched.
    /// </summary>
    [Fact]
    public async Task The_picker_says_how_many_rarities_each_set_still_has_no_weight_for()
    {
        using var fixture = new RarityWeightFixture()
            .WithCatalog()
            .WithWeights((1, 1, 1));

        var response = await HandleAsync(fixture);

        var sets = response.Sets.ToDictionary(set => set.Id, set => set.UnweightedCount);

        Assert.Equal(2, sets[RarityWeightFixture.AscendedHeroesId]);
        Assert.Equal(2, sets[RarityWeightFixture.AnniversaryId]);
        Assert.Equal(0, sets[RarityWeightFixture.EmptySetId]);
    }

    /// <summary>Newest first, because a set is weighted soon after it is loaded.</summary>
    [Fact]
    public async Task The_picker_lists_the_newest_set_first()
    {
        using var fixture = new RarityWeightFixture().WithCatalog();

        var response = await HandleAsync(fixture);

        Assert.Equal(RarityWeightFixture.EmptySetId, response.Sets[0].Id);
        Assert.Equal(RarityWeightFixture.AscendedHeroesId, response.Sets[1].Id);
        Assert.Equal(RarityWeightFixture.AnniversaryId, response.Sets[2].Id);
    }

    /// <summary>
    /// A set with no rarity rows is not an error. It is a set loaded without them, and the page
    /// has to be able to say so rather than render an empty form that looks broken.
    /// </summary>
    [Fact]
    public async Task A_set_with_no_rarities_comes_back_selected_and_empty()
    {
        using var fixture = new RarityWeightFixture().WithCatalog();

        var response = await HandleAsync(fixture, RarityWeightFixture.EmptySetId);

        Assert.NotNull(response.Selected);
        Assert.Empty(response.Selected.Rarities);
    }
}
