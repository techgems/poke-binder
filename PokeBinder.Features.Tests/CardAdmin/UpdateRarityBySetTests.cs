using FluentValidation.Results;
using PokeBinder.Features.CardAdmin.UpdateRarityBySet;
using PokeBinder.Features.CardAdmin.UpdateRarityBySet.Models;
using PokeBinder.TcgCatalog.DbContext.Entities;

namespace PokeBinder.Features.Tests.CardAdmin;

/// <summary>
/// What writing a set's rarity weights does, and what it refuses.
///
/// <para>
/// The rules stated here are the ones everything downstream leans on: the stamp moves with the
/// rows, the slice cannot add or remove a rarity, and a name order may not repeat inside one set
/// while a pull rate may. The two columns differ because they are different kinds of number -- a
/// ranking that repeats itself does not say which of the two rarities comes first, whereas two
/// rarities really can take about the same number of packs.
/// </para>
/// </summary>
public class UpdateRarityBySetTests
{
    private static UpdateRarityBySet.Request Save(
        int setId,
        params (int Id, int? PullRate, int? NameOrder)[] weights) =>
        new()
        {
            SetId = setId,
            Weights = weights
                .Select(weight => new RarityWeightEdit
                {
                    Id = weight.Id,
                    PullRateRarityOrder = weight.PullRate,
                    NameRarityOrder = weight.NameOrder,
                })
                .ToList(),
        };

    private static Task<ValidationResult> ValidateAsync(
        RarityWeightFixture fixture,
        UpdateRarityBySet.Request request) =>
        new UpdateRarityBySetValidator(fixture.Context).ValidateAsync(request);

    private static Task<UpdateRarityBySet.Response> HandleAsync(
        RarityWeightFixture fixture,
        UpdateRarityBySet.Request request) =>
        UpdateRarityBySet.Handler(request, fixture.Context);

    // -------------------------------------------------------------------------------------------
    // What a save writes
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Both_values_are_stored_against_the_rarity_they_were_entered_for()
    {
        using var fixture = new RarityWeightFixture().WithCatalog();

        await HandleAsync(fixture, Save(
            RarityWeightFixture.AscendedHeroesId,
            (1, 1, 1),
            (2, 70, 8),
            (3, 540, 9)));

        Assert.Equal(70, fixture.Row(2).PullRateRarityOrder);
        Assert.Equal(8, fixture.Row(2).NameRarityOrder);
        Assert.Equal(540, fixture.Row(3).PullRateRarityOrder);
        Assert.Equal(9, fixture.Row(3).NameRarityOrder);
    }

    /// <summary>
    /// The same rarity name is worth different things in different sets, which is the whole reason
    /// these columns are per row rather than per name.
    /// </summary>
    [Fact]
    public async Task One_sets_weights_leave_the_same_rarity_in_another_set_alone()
    {
        using var fixture = new RarityWeightFixture().WithCatalog();

        await HandleAsync(fixture, Save(RarityWeightFixture.AscendedHeroesId, (2, 70, 8)));
        await HandleAsync(fixture, Save(RarityWeightFixture.AnniversaryId, (5, 20, 8)));

        Assert.Equal(70, fixture.Row(2).PullRateRarityOrder);
        Assert.Equal(20, fixture.Row(5).PullRateRarityOrder);
    }

    /// <summary>
    /// Clearing a field is an edit, not an omission: the rarity is unweighted again, and null is
    /// what says so.
    /// </summary>
    [Fact]
    public async Task An_emptied_field_clears_the_value_that_was_there()
    {
        using var fixture = new RarityWeightFixture()
            .WithCatalog()
            .WithWeights((2, 70, 8));

        await HandleAsync(fixture, Save(RarityWeightFixture.AscendedHeroesId, (2, null, 8)));

        Assert.Null(fixture.Row(2).PullRateRarityOrder);
        Assert.Equal(8, fixture.Row(2).NameRarityOrder);
    }

    /// <summary>A row the payload does not carry is not touched, so a partial save is safe.</summary>
    [Fact]
    public async Task Rows_the_payload_leaves_out_keep_what_they_had()
    {
        using var fixture = new RarityWeightFixture()
            .WithCatalog()
            .WithWeights((1, 1, 1), (2, 70, 8));

        await HandleAsync(fixture, Save(RarityWeightFixture.AscendedHeroesId, (2, 65, 8)));

        Assert.Equal(1, fixture.Row(1).PullRateRarityOrder);
        Assert.Equal(65, fixture.Row(2).PullRateRarityOrder);
    }

    /// <summary>
    /// The browsers compare their cached rows against this stamp and the server's own cache builds
    /// its keys from it, so a save that moved the rows and not the stamp would be invisible to both
    /// until something else happened to bump it.
    /// </summary>
    [Fact]
    public async Task Saving_moves_the_rarity_stamp()
    {
        using var fixture = new RarityWeightFixture().WithCatalog();

        var before = fixture.StampFor(FilterCacheGroup.RarityBySet);

        await HandleAsync(fixture, Save(RarityWeightFixture.AscendedHeroesId, (2, 70, 8)));

        Assert.NotEqual(before, fixture.StampFor(FilterCacheGroup.RarityBySet));
    }

    /// <summary>
    /// And only that stamp: a set's weights say nothing about which sets exist or which pokemon
    /// are in the catalog, and bumping those would cost every browser a re-fetch it does not need.
    /// </summary>
    [Fact]
    public async Task Saving_leaves_the_other_groups_stamps_alone()
    {
        using var fixture = new RarityWeightFixture().WithCatalog();

        var before = fixture.StampFor(FilterCacheGroup.Sets);

        await HandleAsync(fixture, Save(RarityWeightFixture.AscendedHeroesId, (2, 70, 8)));

        Assert.Equal(before, fixture.StampFor(FilterCacheGroup.Sets));
    }

    // -------------------------------------------------------------------------------------------
    // What the validator refuses
    // -------------------------------------------------------------------------------------------

    /// <summary>
    /// Edits only. Which rarities a set contains is the catalog's fact, written by the ETL and by
    /// the set-loading page, so a row this set does not have is refused rather than created.
    /// </summary>
    [Fact]
    public async Task A_rarity_from_another_set_is_refused()
    {
        using var fixture = new RarityWeightFixture().WithCatalog();

        // Row 5 is the 30th anniversary set's Special Illustration Rare.
        var validation = await ValidateAsync(fixture, Save(RarityWeightFixture.AscendedHeroesId, (5, 20, 8)));

        Assert.False(validation.IsValid);
    }

    [Fact]
    public async Task A_rarity_that_does_not_exist_is_refused()
    {
        using var fixture = new RarityWeightFixture().WithCatalog();

        var validation = await ValidateAsync(fixture, Save(RarityWeightFixture.AscendedHeroesId, (999, 20, 8)));

        Assert.False(validation.IsValid);
    }

    [Fact]
    public async Task A_set_that_does_not_exist_is_refused()
    {
        using var fixture = new RarityWeightFixture().WithCatalog();

        var validation = await ValidateAsync(fixture, Save(999, (1, 1, 1)));

        Assert.False(validation.IsValid);
    }

    /// <summary>
    /// A pull rate is a measurement, not a ranking: two rarities in a set can take about the same
    /// number of packs, and refusing to record that would only make somebody round one of them to
    /// a number they know is wrong. The cost is a tie, which the sort has to break anyway for the
    /// rarities nobody has weighted.
    /// </summary>
    [Fact]
    public async Task Two_rarities_in_one_set_may_share_a_pull_rate()
    {
        using var fixture = new RarityWeightFixture().WithCatalog();

        var request = Save(
            RarityWeightFixture.AscendedHeroesId,
            (2, 70, 8),
            (3, 70, 9));

        var validation = await ValidateAsync(fixture, request);

        Assert.True(validation.IsValid);

        await HandleAsync(fixture, request);

        Assert.Equal(70, fixture.Row(2).PullRateRarityOrder);
        Assert.Equal(70, fixture.Row(3).PullRateRarityOrder);
    }

    [Fact]
    public async Task Two_rarities_in_one_set_cannot_share_a_name_order()
    {
        using var fixture = new RarityWeightFixture().WithCatalog();

        var validation = await ValidateAsync(fixture, Save(
            RarityWeightFixture.AscendedHeroesId,
            (2, 70, 8),
            (3, 540, 8)));

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, error => error.ErrorMessage.Contains("name order of 8"));
    }

    /// <summary>
    /// The collision that a payload cannot see: the posted row clashes with a row that was left
    /// out, so the check has to run against the set as it will stand and not against the payload.
    /// </summary>
    [Fact]
    public async Task A_posted_name_order_cannot_collide_with_a_row_the_payload_left_out()
    {
        using var fixture = new RarityWeightFixture()
            .WithCatalog()
            .WithWeights((1, 5, 8));

        var validation = await ValidateAsync(fixture, Save(RarityWeightFixture.AscendedHeroesId, (2, 70, 8)));

        Assert.False(validation.IsValid);
    }

    /// <summary>
    /// The same name order in two different sets is not a collision, and it is the normal case:
    /// the whole point of that column is that a rarity name sits in the same place in every set
    /// that has it.
    /// </summary>
    [Fact]
    public async Task The_same_name_order_in_a_different_set_is_not_a_collision()
    {
        using var fixture = new RarityWeightFixture()
            .WithCatalog()
            .WithWeights((5, 70, 8));

        var validation = await ValidateAsync(fixture, Save(RarityWeightFixture.AscendedHeroesId, (2, 70, 8)));

        Assert.True(validation.IsValid);
    }

    /// <summary>
    /// Unweighted rows are exempt: null is not a value two rarities are sharing, it is two
    /// rarities nobody has weighted yet, and the catalog starts with 366 of them.
    /// </summary>
    [Fact]
    public async Task Two_unweighted_rarities_are_not_a_collision()
    {
        using var fixture = new RarityWeightFixture().WithCatalog();

        var validation = await ValidateAsync(fixture, Save(
            RarityWeightFixture.AscendedHeroesId,
            (1, null, null),
            (2, null, null)));

        Assert.True(validation.IsValid);
    }

    /// <summary>
    /// Zero is refused rather than stored, because a second way of saying "unweighted" is what
    /// would leave the comparator with two answers to the same question.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(UpdateRarityBySet.MaxOrder + 1)]
    public async Task A_value_outside_the_allowed_range_is_refused(int value)
    {
        using var fixture = new RarityWeightFixture().WithCatalog();

        var validation = await ValidateAsync(fixture, Save(RarityWeightFixture.AscendedHeroesId, (2, value, 8)));

        Assert.False(validation.IsValid);
    }

    [Fact]
    public async Task The_same_rarity_sent_twice_is_refused()
    {
        using var fixture = new RarityWeightFixture().WithCatalog();

        var validation = await ValidateAsync(fixture, Save(
            RarityWeightFixture.AscendedHeroesId,
            (2, 70, 8),
            (2, 80, 9)));

        Assert.False(validation.IsValid);
    }

    [Fact]
    public async Task An_empty_payload_is_refused()
    {
        using var fixture = new RarityWeightFixture().WithCatalog();

        var validation = await ValidateAsync(fixture, Save(RarityWeightFixture.AscendedHeroesId));

        Assert.False(validation.IsValid);
    }

    /// <summary>
    /// A null list is what a caller that sent nothing where the rows belong looks like. It has to
    /// come back as a refusal rather than as an exception -- see "Rules for controllers" in
    /// CLAUDE.md, which is where the Cascade.Stop on the list rule comes from.
    /// </summary>
    [Fact]
    public async Task A_null_list_is_refused_rather_than_throwing()
    {
        using var fixture = new RarityWeightFixture().WithCatalog();

        var request = new UpdateRarityBySet.Request
        {
            SetId = RarityWeightFixture.AscendedHeroesId,
            Weights = null!,
        };

        var validation = await ValidateAsync(fixture, request);

        Assert.False(validation.IsValid);
    }

    /// <summary>A valid save is valid, so none of the rules above is refusing the normal case.</summary>
    [Fact]
    public async Task A_full_set_of_distinct_weights_passes()
    {
        using var fixture = new RarityWeightFixture().WithCatalog();

        var validation = await ValidateAsync(fixture, Save(
            RarityWeightFixture.AscendedHeroesId,
            (1, 1, 1),
            (2, 70, 8),
            (3, 540, 9)));

        Assert.True(validation.IsValid);
    }
}
