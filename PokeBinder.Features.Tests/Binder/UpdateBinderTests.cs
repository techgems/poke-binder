using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext.Entities;
using PokeBinder.Features.Binder.ReorderBinder.Models;
using PokeBinder.Features.Binder.UpdateBinder;
using PokeBinder.Features.Binder.UpdateBinder.Models;
using BinderEntity = PokeBinder.Binders.DbContext.Entities.Binder;

namespace PokeBinder.Features.Tests.Binder;

/// <summary>
/// What UpdateBinder does to a binder: its settings, and -- when a resize moves the cards -- each of
/// the four things the user can choose to happen to them.
///
/// <para>
/// The one it must never get wrong is <see cref="A_bare_resize_that_would_strand_cards_is_refused"/>:
/// an edit that carries no choice leaves every card in its pocket, so a size that no longer has
/// those pockets is refused rather than quietly losing the cards in them.
/// </para>
/// </summary>
public class UpdateBinderTests
{
    /// <summary>The fixture's own grid: 3x3, so 9 pockets a page.</summary>
    private const int ThreeByThree = 1;

    /// <summary>A second grid the tests resize to: 2x2, so 4 pockets a page.</summary>
    private const int TwoByTwo = 2;

    /// <summary>The edit the settings tab sends for the fixture's binder as it stands, with nothing changed.</summary>
    private static UpdateBinder.Request Unchanged() => new()
    {
        BinderId = BinderFixture.BinderId,
        Name = "Test binder",
        BinderSizeId = ThreeByThree,
        Pages = 4,
    };

    private static Task<UpdateBinder.Response> HandleAsync(BinderFixture fixture, UpdateBinder.Request request) =>
        UpdateBinder.Handler(request, BinderFixture.UserId, fixture.Binders, fixture.Catalog);

    private static Task<FluentValidation.Results.ValidationResult> ValidateAsync(
        BinderFixture fixture,
        UpdateBinder.Request request,
        int userId = BinderFixture.UserId) =>
        new UpdateBinderValidator(fixture.Binders, userId).ValidateAsync(request);

    /// <summary>A 3x3 binder of four pages, with a 2x2 grid in the size list beside it.</summary>
    private static BinderFixture Binder()
    {
        var fixture = new BinderFixture().WithBinder();

        fixture.Binders.BinderSizes.Add(new BinderSize
        {
            Id = TwoByTwo,
            Name = "2x2",
            Description = "4 cards per page",
            X = 2,
            Y = 2,
            DefaultPages = 40,
        });

        fixture.Binders.SaveChanges();

        return fixture;
    }

    private static async Task<BinderEntity> StoredBinderAsync(BinderFixture fixture)
    {
        fixture.Binders.ChangeTracker.Clear();

        return await fixture.Binders.Binders.SingleAsync(binder => binder.Id == BinderFixture.BinderId);
    }

    // -------------------------------------------------------------------------------------------
    // The settings
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Saves_the_name_description_size_and_pages()
    {
        using var fixture = Binder();

        await HandleAsync(fixture, Unchanged() with
        {
            Name = "Renamed",
            Description = "For the SV era",
            BinderSizeId = TwoByTwo,
            Pages = 10,
        });

        var binder = await StoredBinderAsync(fixture);

        Assert.Equal("Renamed", binder.Name);
        Assert.Equal("For the SV era", binder.Description);
        Assert.Equal(TwoByTwo, binder.BinderSizeId);
        Assert.Equal(10, binder.Pages);
    }

    [Fact]
    public async Task Trims_the_name_and_stores_a_blank_description_as_none()
    {
        using var fixture = Binder();

        await HandleAsync(fixture, Unchanged() with { Name = "  Spaced  ", Description = "   " });

        var binder = await StoredBinderAsync(fixture);

        Assert.Equal("Spaced", binder.Name);
        Assert.Null(binder.Description);
    }

    [Fact]
    public async Task Without_a_choice_every_card_stays_in_its_pocket()
    {
        using var fixture = Binder().WithPlacements((0, 11), (5, 12), (9, 13)).WithTray((14, 1));

        await HandleAsync(fixture, Unchanged() with { Pages = 2 });

        Assert.Equal(new Dictionary<int, int> { [0] = 11, [5] = 12, [9] = 13 }, await fixture.StoredPlacementsAsync());
        Assert.Equal(new Dictionary<int, int> { [14] = 1 }, await fixture.StoredTrayAsync());
    }

    // -------------------------------------------------------------------------------------------
    // The four choices
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Keep_lays_the_cards_out_from_the_first_pocket_in_the_order_they_had()
    {
        using var fixture = Binder().WithPlacements((2, 11), (7, 12), (20, 13), (35, 14));

        await HandleAsync(fixture, Unchanged() with
        {
            BinderSizeId = TwoByTwo,
            Pages = 1,
            Rearrange = Rearrangement.Keep,
        });

        Assert.Equal(
            new Dictionary<int, int> { [0] = 11, [1] = 12, [2] = 13, [3] = 14 },
            await fixture.StoredPlacementsAsync());
    }

    [Fact]
    public async Task Keep_carries_each_missing_flag_with_its_card()
    {
        using var fixture = Binder().WithPlacements((3, 11), (8, 12)).WithMissing(8);

        await HandleAsync(fixture, Unchanged() with { Rearrange = Rearrangement.Keep });

        Assert.Equal([1], await fixture.StoredMissingAsync());
    }

    [Fact]
    public async Task Sort_lays_the_cards_out_in_the_order_the_criteria_give()
    {
        using var fixture = Binder()
            .WithSet(1, "Set", releaseDateUnix: 1_700_000_000)
            .WithCard(11, setId: 1, "Common", "Pidgey", "1")
            .WithCard(12, setId: 1, "Common", "Eevee", "2")
            .WithCard(13, setId: 1, "Common", "Snorlax", "3")
            .WithPlacements((0, 11), (4, 12), (30, 13));

        await HandleAsync(fixture, Unchanged() with
        {
            BinderSizeId = TwoByTwo,
            Pages = 1,
            Rearrange = Rearrangement.Sort,
            Criteria = [new SortCriterion { Key = CriterionKey.Name, Direction = SortDirection.Asc }],
        });

        Assert.Equal(
            new Dictionary<int, int> { [0] = 12, [1] = 11, [2] = 13 },
            await fixture.StoredPlacementsAsync());
    }

    [Fact]
    public async Task Tray_returns_every_placed_card_to_the_tray_beside_what_it_held()
    {
        using var fixture = Binder()
            .WithPlacements((0, 11), (1, 12), (6, 11))
            .WithTray((12, 2), (15, 1));

        await HandleAsync(fixture, Unchanged() with { Rearrange = Rearrangement.Tray });

        Assert.Empty(await fixture.StoredPlacementsAsync());
        Assert.Equal(
            new Dictionary<int, int> { [11] = 2, [12] = 3, [15] = 1 },
            await fixture.StoredTrayAsync());
    }

    [Fact]
    public async Task Drop_removes_the_placed_cards_and_leaves_the_tray_alone()
    {
        using var fixture = Binder().WithPlacements((0, 11), (9, 12)).WithTray((13, 1));

        await HandleAsync(fixture, Unchanged() with { Pages = 1, Rearrange = Rearrangement.Drop });

        Assert.Empty(await fixture.StoredPlacementsAsync());
        Assert.Equal(new Dictionary<int, int> { [13] = 1 }, await fixture.StoredTrayAsync());
    }

    // -------------------------------------------------------------------------------------------
    // Whether the cards fit
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task A_bare_resize_that_would_strand_cards_is_refused()
    {
        using var fixture = Binder().WithPlacements((0, 11), (20, 12));

        // Two pages of 3x3 is 18 pockets, and pocket 20 is not one of them.
        var result = await ValidateAsync(fixture, Unchanged() with { Pages = 2 });

        var failure = Assert.Single(result.Errors);
        Assert.Equal(nameof(UpdateBinder.Request.Pages), failure.PropertyName);
    }

    [Fact]
    public async Task A_bare_resize_that_strands_nothing_is_allowed()
    {
        using var fixture = Binder().WithPlacements((0, 11), (17, 12));

        var result = await ValidateAsync(fixture, Unchanged() with { Pages = 2 });

        Assert.True(result.IsValid, result.ToString());
    }

    [Theory]
    [InlineData(Rearrangement.Keep)]
    [InlineData(Rearrangement.Sort)]
    public async Task Laying_out_again_needs_a_pocket_for_every_card(Rearrangement rearrange)
    {
        using var fixture = Binder().WithPlacements((0, 11), (1, 12), (2, 13), (3, 14), (4, 15));

        // One page of 2x2 is four pockets for five cards.
        var result = await ValidateAsync(fixture, Unchanged() with
        {
            BinderSizeId = TwoByTwo,
            Pages = 1,
            Rearrange = rearrange,
            Criteria = rearrange == Rearrangement.Sort
                ? [new SortCriterion { Key = CriterionKey.Name, Direction = SortDirection.Asc }]
                : null,
        });

        var failure = Assert.Single(result.Errors);
        Assert.Equal(nameof(UpdateBinder.Request.Pages), failure.PropertyName);
    }

    [Theory]
    [InlineData(Rearrangement.Tray)]
    [InlineData(Rearrangement.Drop)]
    public async Task Emptying_the_binder_fits_any_size(Rearrangement rearrange)
    {
        using var fixture = Binder().WithPlacements((0, 11), (1, 12), (2, 13), (3, 14), (35, 15));

        var result = await ValidateAsync(fixture, Unchanged() with
        {
            BinderSizeId = TwoByTwo,
            Pages = 1,
            Rearrange = rearrange,
        });

        Assert.True(result.IsValid, result.ToString());
    }

    // -------------------------------------------------------------------------------------------
    // What a request has to carry
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task The_owner_may_edit_it()
    {
        using var fixture = Binder();

        var result = await ValidateAsync(fixture, Unchanged());

        Assert.True(result.IsValid, result.ToString());
    }

    [Fact]
    public async Task Someone_elses_binder_is_refused()
    {
        using var fixture = Binder();

        var result = await ValidateAsync(fixture, Unchanged(), userId: BinderFixture.OtherUserId);

        Assert.Contains(result.Errors, failure => failure.PropertyName == nameof(UpdateBinder.Request.BinderId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_binder_needs_a_name(string? name)
    {
        using var fixture = Binder();

        var result = await ValidateAsync(fixture, Unchanged() with { Name = name });

        var failure = Assert.Single(result.Errors);
        Assert.Equal(nameof(UpdateBinder.Request.Name), failure.PropertyName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public async Task Pages_out_of_range_are_refused(int pages)
    {
        using var fixture = Binder();

        var result = await ValidateAsync(fixture, Unchanged() with { Pages = pages });

        var failure = Assert.Single(result.Errors);
        Assert.Equal(nameof(UpdateBinder.Request.Pages), failure.PropertyName);
    }

    [Fact]
    public async Task An_unknown_size_is_refused()
    {
        using var fixture = Binder();

        var result = await ValidateAsync(fixture, Unchanged() with { BinderSizeId = 99 });

        var failure = Assert.Single(result.Errors);
        Assert.Equal(nameof(UpdateBinder.Request.BinderSizeId), failure.PropertyName);
    }

    [Fact]
    public async Task A_sort_whose_criteria_are_null_is_a_validation_failure_not_an_exception()
    {
        using var fixture = Binder().WithPlacements((0, 11));

        // What System.Text.Json produces for "criteria": null -- the case Cascade.Stop is there for.
        var result = await ValidateAsync(fixture, Unchanged() with { Rearrange = Rearrangement.Sort, Criteria = null });

        var failure = Assert.Single(result.Errors);
        Assert.Equal(nameof(UpdateBinder.Request.Criteria), failure.PropertyName);
    }

    [Fact]
    public async Task A_sort_with_no_criteria_is_refused()
    {
        using var fixture = Binder().WithPlacements((0, 11));

        var result = await ValidateAsync(fixture, Unchanged() with { Rearrange = Rearrangement.Sort, Criteria = [] });

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Criteria_without_a_sort_are_refused()
    {
        using var fixture = Binder().WithPlacements((0, 11));

        var result = await ValidateAsync(fixture, Unchanged() with
        {
            Rearrange = Rearrangement.Keep,
            Criteria = [new SortCriterion { Key = CriterionKey.Name, Direction = SortDirection.Asc }],
        });

        var failure = Assert.Single(result.Errors);
        Assert.Equal(nameof(UpdateBinder.Request.Criteria), failure.PropertyName);
    }

    [Fact]
    public async Task The_handler_will_not_edit_someone_elses_binder_without_its_validator()
    {
        using var fixture = new BinderFixture().WithBinder(userId: BinderFixture.OtherUserId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => HandleAsync(fixture, Unchanged() with { Name = "Mine now" }));
    }
}
