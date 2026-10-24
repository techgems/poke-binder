using FluentValidation.Results;
using PokeBinder.Features.Binder.AddSetToBinder;
using PokeBinder.Features.Binder.AddSetToBinder.Models;
using PokeBinder.Features.Binder.Shared.Arrangement;
using PokeBinder.Features.CardImages;

namespace PokeBinder.Features.Tests.Binder;

/// <summary>
/// What AddSetToBinder does to a binder.
///
/// <para>
/// Every binder here is the fixture's default, a 3x3 over four pages: nine pockets a page, so page
/// two starts at pocket 9 and page three at 18. The set is four cards, which fills one page and
/// leaves it part empty -- enough to see where the set starts and that whatever follows it starts on
/// a page of its own.
/// </para>
/// </summary>
public class AddSetToBinderTests
{
    private const int SetId = 5;

    private static readonly CardImageUrls ImageUrls = new("C:/ImagesTCG", "/cardImages");

    private static SortCriterion ByNumber(SortDirection direction = SortDirection.Asc) =>
        new() { Key = CriterionKey.Number, Direction = direction };

    private static SortCriterion ByRarity(SortDirection direction) =>
        new() { Key = CriterionKey.Rarity, Direction = direction, RarityKey = RarityKey.PullRate };

    private static AddSetToBinder.Request AddSet(
        ExistingCards existing = ExistingCards.After,
        bool emptyFirst = false,
        bool addPages = false,
        IReadOnlyList<string>? leaveOut = null,
        params SortCriterion[] criteria) =>
        new()
        {
            BinderId = BinderFixture.BinderId,
            SetId = SetId,
            LeaveOut = leaveOut ?? [],
            Criteria = criteria.Length == 0 ? [ByNumber()] : criteria,
            EmptyFirst = emptyFirst,
            Existing = existing,
            AddPages = addPages,
        };

    private static Task<AddSetToBinder.Response> HandleAsync(BinderFixture fixture, AddSetToBinder.Request request) =>
        AddSetToBinder.Handler(request, BinderFixture.UserId, fixture.Binders, fixture.Catalog, ImageUrls);

    private static Task<ValidationResult> ValidateAsync(
        BinderFixture fixture,
        AddSetToBinder.Request request,
        int userId = BinderFixture.UserId) =>
        new AddSetToBinderValidator(fixture.Binders, fixture.Catalog, userId).ValidateAsync(request);

    /// <summary>
    /// A set of four cards, numbered so that text order and number order disagree ("10" before "4"
    /// as text), with weighted rarities, and two cards of another set already placed on page one.
    /// </summary>
    private static BinderFixture WithASet(int pages = 4, params (int Pocket, int CardId)[] placed) =>
        new BinderFixture()
            .WithBinder(pages: pages)
            .WithSet(1, "Old Set", releaseDateUnix: 1_600_000_000)
            .WithSet(SetId, "New Set", releaseDateUnix: 1_700_000_000)
            .WithRarity(SetId, "Common", pullRate: 1)
            .WithRarity(SetId, "Rare", pullRate: 10)
            .WithRarity(SetId, "Ultra Rare", pullRate: 80)
            .WithCard(51, SetId, "Common", "Pidgey", "10")
            .WithCard(52, SetId, "Ultra Rare", "Gengar", "4")
            .WithCard(53, SetId, "Rare", "Snorlax", "2")
            .WithCard(54, SetId, "Common", "Eevee", "1")
            .WithCard(11, setId: 1, "Common", "Ratata", "5")
            .WithCard(12, setId: 1, "Rare", "Mew", "6")
            .WithPlacements(placed.Length == 0 ? [(0, 11), (4, 12)] : placed);

    // -------------------------------------------------------------------------------------------
    // Where the set goes
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task By_default_the_set_starts_on_the_page_after_the_last_card()
    {
        using var fixture = WithASet();

        await HandleAsync(fixture, AddSet());

        var stored = await fixture.StoredPlacementsAsync();

        // The cards already there stay put; the set fills page two, in card number order.
        Assert.Equal(new Dictionary<int, int>
        {
            [0] = 11, [4] = 12,
            [9] = 54, [10] = 53, [11] = 52, [12] = 51,
        }, stored);
    }

    [Fact]
    public async Task A_buffer_page_is_left_blank_before_the_set()
    {
        using var fixture = WithASet();

        await HandleAsync(fixture, AddSet(ExistingCards.Buffer));

        var stored = await fixture.StoredPlacementsAsync();

        Assert.DoesNotContain(stored.Keys, pocket => pocket is >= 9 and < 18);
        Assert.Equal([54, 53, 52, 51], stored.Where(p => p.Key >= 18).OrderBy(p => p.Key).Select(p => p.Value));
    }

    [Fact]
    public async Task Sent_to_the_end_the_cards_move_back_by_whole_pages()
    {
        using var fixture = WithASet();

        await HandleAsync(fixture, AddSet(ExistingCards.End));

        var stored = await fixture.StoredPlacementsAsync();

        // The set takes page one; the old page one becomes page two, each card in the same place on
        // it -- pocket 4 is still the middle of its page.
        Assert.Equal(new Dictionary<int, int>
        {
            [0] = 54, [1] = 53, [2] = 52, [3] = 51,
            [9] = 11, [13] = 12,
        }, stored);
    }

    [Fact]
    public async Task A_card_keeps_its_missing_flag_when_it_moves()
    {
        using var fixture = WithASet().WithMissing(4);

        await HandleAsync(fixture, AddSet(ExistingCards.End));

        Assert.Equal([13], await fixture.StoredMissingAsync());
    }

    [Fact]
    public async Task An_empty_binder_takes_the_set_from_the_first_pocket()
    {
        using var fixture = new BinderFixture()
            .WithBinder()
            .WithSet(SetId, "New Set", releaseDateUnix: 1_700_000_000)
            .WithCard(51, SetId, "Common", "Pidgey", "10")
            .WithCard(54, SetId, "Common", "Eevee", "1");

        // Neither a buffer nor the end means anything without cards to put them after.
        await HandleAsync(fixture, AddSet(ExistingCards.Buffer));

        Assert.Equal(new Dictionary<int, int> { [0] = 54, [1] = 51 }, await fixture.StoredPlacementsAsync());
    }

    [Fact]
    public async Task Emptying_first_removes_the_cards_and_leaves_the_tray_alone()
    {
        using var fixture = WithASet().WithTray((11, 2));

        await HandleAsync(fixture, AddSet(ExistingCards.End, emptyFirst: true));

        Assert.Equal(new Dictionary<int, int> { [0] = 54, [1] = 53, [2] = 52, [3] = 51 },
            await fixture.StoredPlacementsAsync());

        // Removed, not returned: the tray holds what it held.
        Assert.Equal(new Dictionary<int, int> { [11] = 2 }, await fixture.StoredTrayAsync());
    }

    [Fact]
    public async Task The_tray_is_left_alone_when_nothing_is_emptied()
    {
        using var fixture = WithASet().WithTray((51, 1));

        await HandleAsync(fixture, AddSet());

        Assert.Equal(new Dictionary<int, int> { [51] = 1 }, await fixture.StoredTrayAsync());
    }

    // -------------------------------------------------------------------------------------------
    // What goes in, and in what order
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Card_numbers_compare_as_numbers()
    {
        using var fixture = WithASet();

        await HandleAsync(fixture, AddSet(criteria: [ByNumber(SortDirection.Desc)]));

        // "10" is the highest number, though it is not the highest as text.
        Assert.Equal([51, 52, 53, 54], (await fixture.StoredPlacementsAsync())
            .Where(p => p.Key >= 9).OrderBy(p => p.Key).Select(p => p.Value));
    }

    [Fact]
    public async Task Criteria_order_the_set_like_order_by()
    {
        using var fixture = WithASet();

        await HandleAsync(fixture, AddSet(criteria: [ByRarity(SortDirection.Desc), ByNumber()]));

        // Rarest first, and the two commons by number between themselves.
        Assert.Equal([52, 53, 54, 51], (await fixture.StoredPlacementsAsync())
            .Where(p => p.Key >= 9).OrderBy(p => p.Key).Select(p => p.Value));
    }

    [Fact]
    public async Task Left_out_rarities_stay_out()
    {
        using var fixture = WithASet();

        var response = await HandleAsync(fixture, AddSet(leaveOut: ["Common"]));

        Assert.Equal([53, 52], response.Added.Select(card => card.Id));
        Assert.Equal([53, 52], (await fixture.StoredPlacementsAsync())
            .Where(p => p.Key >= 9).OrderBy(p => p.Key).Select(p => p.Value));
    }

    // -------------------------------------------------------------------------------------------
    // Pages
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Pages_are_added_at_the_end_when_the_set_does_not_fit()
    {
        // Two pages, the first holding a card in its last pocket: the set needs a third.
        using var fixture = WithASet(pages: 2, (8, 11));

        var response = await HandleAsync(fixture, AddSet(ExistingCards.Buffer, addPages: true));

        Assert.Equal(3, response.Pages);
        Assert.Equal(18, response.SetStart);
        Assert.Equal(8, (await fixture.StoredPlacementsAsync()).Single(p => p.Value == 11).Key);
        Assert.Equal(18, response.Cards.First(card => card.CardId == 54).IndexInBinder);
    }

    [Fact]
    public async Task A_binder_longer_than_needed_keeps_its_pages()
    {
        using var fixture = WithASet();

        var response = await HandleAsync(fixture, AddSet(emptyFirst: true));

        Assert.Equal(4, response.Pages);
    }

    // -------------------------------------------------------------------------------------------
    // The response
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task The_response_is_the_new_layout_and_the_cards_that_went_in()
    {
        using var fixture = WithASet();

        var response = await HandleAsync(fixture, AddSet());

        Assert.Equal([0, 4, 9, 10, 11, 12], response.Cards.Select(card => card.IndexInBinder));
        Assert.Equal([54, 53, 52, 51], response.Added.Select(card => card.Id));
        Assert.Equal("New Set", response.Added[0].SetName);
    }

    // -------------------------------------------------------------------------------------------
    // What the validator refuses
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task A_set_that_does_not_fit_is_refused_unless_pages_may_be_added()
    {
        using var fixture = WithASet(pages: 2, (8, 11));

        var refused = await ValidateAsync(fixture, AddSet(ExistingCards.Buffer));
        var allowed = await ValidateAsync(fixture, AddSet(ExistingCards.Buffer, addPages: true));

        Assert.Contains(refused.Errors, error => error.PropertyName == nameof(AddSetToBinder.Request.AddPages));
        Assert.True(allowed.IsValid);
    }

    [Fact]
    public async Task A_set_past_the_page_limit_is_refused_even_with_pages_allowed()
    {
        // A card on the binder's last allowed page, so anything after it is past the limit.
        using var fixture = WithASet(pages: 200, (200 * 9 - 1, 11));

        var result = await ValidateAsync(fixture, AddSet(addPages: true));

        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("at most 200"));
    }

    [Fact]
    public async Task A_set_with_every_rarity_left_out_is_refused()
    {
        using var fixture = WithASet();

        var result = await ValidateAsync(fixture, AddSet(leaveOut: ["Common", "Rare", "Ultra Rare"]));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddSetToBinder.Request.LeaveOut));
    }

    [Fact]
    public async Task A_set_the_catalog_does_not_have_is_refused()
    {
        using var fixture = WithASet();

        var result = await ValidateAsync(fixture, AddSet() with { SetId = 999 });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddSetToBinder.Request.SetId));
    }

    [Fact]
    public async Task Someone_elses_binder_is_refused()
    {
        using var fixture = WithASet();

        var result = await ValidateAsync(fixture, AddSet(), userId: BinderFixture.OtherUserId);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddSetToBinder.Request.BinderId));
    }

    [Fact]
    public async Task Sorting_one_set_by_set_is_refused()
    {
        using var fixture = WithASet();

        var result = await ValidateAsync(
            fixture, AddSet(criteria: [new SortCriterion { Key = CriterionKey.Set, Direction = SortDirection.Asc }]));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task A_null_leave_out_list_is_a_validation_failure_not_a_crash()
    {
        using var fixture = WithASet();

        var result = await ValidateAsync(fixture, AddSet() with { LeaveOut = null! });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddSetToBinder.Request.LeaveOut));
    }

    [Fact]
    public async Task A_null_criteria_list_is_a_validation_failure_not_a_crash()
    {
        using var fixture = WithASet();

        var result = await ValidateAsync(fixture, AddSet() with { Criteria = null! });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddSetToBinder.Request.Criteria));
    }

    // -------------------------------------------------------------------------------------------
    // Labels on existing cards travel with them; the set's cards arrive unlabelled
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Labels_on_a_kept_card_follow_it_to_its_new_pocket()
    {
        using var fixture = WithASet()
            .WithLabel(1, "Trade", "blue")
            .WithLabelsOn(0, 1);

        // Existing cards go after the set and shift back by a page. Pocket 0 had card 11 with
        // label 1; after the add it is at pocket 0 + setPages * cardsPerPage = 9.
        await HandleAsync(fixture, AddSet(existing: ExistingCards.End));

        var placements = await fixture.StoredPlacementsAsync();
        var assignments = await fixture.StoredAssignmentsAsync();

        Assert.Equal(11, placements[9]);
        Assert.Equal(new[] { 1 }, assignments[9]);
        Assert.False(assignments.ContainsKey(0));
    }

    [Fact]
    public async Task The_sets_cards_arrive_unlabelled()
    {
        using var fixture = WithASet();

        await HandleAsync(fixture, AddSet());

        var assignments = await fixture.StoredAssignmentsAsync();

        Assert.Empty(assignments);
    }
}
