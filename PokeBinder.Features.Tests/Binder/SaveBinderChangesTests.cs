using PokeBinder.Features.Binder.SaveBinderChanges;
using PokeBinder.Features.Binder.SaveBinderChanges.Models;

namespace PokeBinder.Features.Tests.Binder;

/// <summary>
/// What SaveBinderChanges does with a partial payload.
///
/// <para>
/// These are here to survive a rewrite of the slice. Every test states a rule the workspace's save
/// depends on -- what a claim covers, what silence inside it means, what silence outside it must
/// not mean -- and none of them reach for the slice's internals, so a refactor that keeps the
/// behaviour keeps the tests.
/// </para>
///
/// <para>
/// The one this contract exists for is <see cref="Pockets_outside_the_claimed_pages_survive"/>. A
/// partial payload that quietly empties the rest of the binder is the failure mode here, and it is
/// the one nobody would notice until a binder came back short.
/// </para>
/// </summary>
public class SaveBinderChangesTests
{
    /// <summary>A 3x3 binder, so page 1 is pockets 0-8, page 2 is 9-17, and so on.</summary>
    private const int CardsPerPage = 9;

    private static SaveBinderChanges.Request Save(
        int[] pages,
        (int Pocket, int CardId)[]? cards = null,
        (int CardId, int Quantity)[]? tray = null,
        bool[]? missing = null) =>
        new()
        {
            BinderId = BinderFixture.BinderId,
            Pages = pages,
            Cards = (cards ?? [])
                .Select((placement, position) => new PlacedCard
                {
                    CardId = placement.CardId,
                    IndexInBinder = placement.Pocket,
                    IsMissing = missing is not null && missing[position],
                })
                .ToList(),
            Tray = (tray ?? [])
                .Select(entry => new TrayCard { CardId = entry.CardId, Quantity = entry.Quantity })
                .ToList(),
        };

    private static Task<SaveBinderChanges.Response> HandleAsync(
        BinderFixture fixture,
        SaveBinderChanges.Request request) =>
        SaveBinderChanges.Handler(request, BinderFixture.UserId, fixture.Binders);

    private static Task<FluentValidation.Results.ValidationResult> ValidateAsync(
        BinderFixture fixture,
        SaveBinderChanges.Request request,
        int userId = BinderFixture.UserId) =>
        new SaveBinderChangesValidator(fixture.Binders, fixture.Catalog, userId)
            .ValidateAsync(request);

    // -------------------------------------------------------------------------------------------
    // What a claim covers, and what it leaves alone
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Pockets_outside_the_claimed_pages_survive()
    {
        using var fixture = new BinderFixture()
            .WithBinder()
            .WithPlacements((0, 100), (9, 200), (18, 300), (27, 400));

        // Page 2 only, and page 2 is now empty. The other three pages are not in the payload, and
        // under a whole-binder save that absence would empty them.
        var response = await HandleAsync(fixture, Save(pages: [2]));

        var stored = await fixture.StoredPlacementsAsync();

        Assert.Equal(1, response.Removed);
        Assert.Equal(100, stored[0]);
        Assert.Equal(300, stored[18]);
        Assert.Equal(400, stored[27]);
        Assert.False(stored.ContainsKey(9));
    }

    [Fact]
    public async Task A_row_between_two_claimed_pages_survives()
    {
        using var fixture = new BinderFixture()
            .WithBinder()
            .WithPlacements((0, 100), (9, 200), (18, 300));

        // Pages 1 and 3, which the diff reads as one span because that is the cheaper query --
        // page 2 sits inside that span and outside the claim, so it has to come out untouched.
        var response = await HandleAsync(fixture, Save(pages: [1, 3]));

        var stored = await fixture.StoredPlacementsAsync();

        Assert.Equal(2, response.Removed);
        Assert.Equal(200, stored[9]);
        Assert.Single(stored);
    }

    [Fact]
    public async Task A_pocket_the_claim_covers_and_the_payload_leaves_out_is_emptied()
    {
        using var fixture = new BinderFixture()
            .WithBinder()
            .WithPlacements((0, 100), (1, 200));

        // Inside the claim the payload is still a snapshot: pocket 1 is absent, so the user emptied
        // it. This is the half of the contract that makes a repeated tick harmless.
        var response = await HandleAsync(fixture, Save(pages: [1], cards: [(0, 100)]));

        var stored = await fixture.StoredPlacementsAsync();

        Assert.Equal(1, response.Removed);
        Assert.Equal(0, response.Changed);
        Assert.Equal(new Dictionary<int, int> { [0] = 100 }, stored);
    }

    [Fact]
    public async Task A_card_on_a_claimed_page_with_no_row_yet_is_placed()
    {
        using var fixture = new BinderFixture().WithBinder();

        var response = await HandleAsync(fixture, Save(pages: [2], cards: [(9, 200), (13, 250)]));

        var stored = await fixture.StoredPlacementsAsync();

        Assert.Equal(2, response.Placed);
        Assert.Equal(200, stored[9]);
        Assert.Equal(250, stored[13]);
    }

    [Fact]
    public async Task A_pocket_that_swapped_cards_is_changed_rather_than_replaced()
    {
        using var fixture = new BinderFixture().WithBinder().WithPlacements((0, 100));

        var response = await HandleAsync(fixture, Save(pages: [1], cards: [(0, 999)]));

        var stored = await fixture.StoredPlacementsAsync();

        // One row rewritten, not one deleted and one added: the row is keyed by pocket, and a
        // delete-then-insert on the same key is the write this diff exists to avoid.
        Assert.Equal(1, response.Changed);
        Assert.Equal(0, response.Placed);
        Assert.Equal(0, response.Removed);
        Assert.Equal(999, stored[0]);
    }

    [Fact]
    public async Task A_save_that_changes_nothing_reports_nothing()
    {
        using var fixture = new BinderFixture()
            .WithBinder()
            .WithPlacements((0, 100))
            .WithTray((500, 2));

        // What a debounce tick mostly is: the same spread and the same tray the server already has.
        var response = await HandleAsync(
            fixture,
            Save(pages: [1], cards: [(0, 100)], tray: [(500, 2)]));

        Assert.Equal(0, response.Placed);
        Assert.Equal(0, response.Changed);
        Assert.Equal(0, response.Removed);
        Assert.Equal(0, response.TrayAdded);
        Assert.Equal(0, response.TrayUpdated);
        Assert.Equal(0, response.TrayRemoved);
    }

    [Fact]
    public async Task Saving_the_same_payload_twice_stores_the_same_thing()
    {
        using var fixture = new BinderFixture()
            .WithBinder()
            .WithPlacements((0, 100), (9, 200));

        var request = Save(pages: [1], cards: [(1, 100)], tray: [(500, 1)]);

        await HandleAsync(fixture, request);

        var afterFirst = await fixture.StoredPlacementsAsync();

        // The debounce can fire twice, a dropped connection can be retried, and a sleeping tab can
        // wake up and send what it was holding. All three are this.
        var second = await HandleAsync(fixture, request);

        Assert.Equal(afterFirst, await fixture.StoredPlacementsAsync());
        Assert.Equal(0, second.Placed);
        Assert.Equal(0, second.Changed);
        Assert.Equal(0, second.Removed);
    }

    [Fact]
    public async Task The_missing_flag_travels_with_the_card()
    {
        using var fixture = new BinderFixture().WithBinder();

        await HandleAsync(fixture, Save(pages: [1], cards: [(0, 100)], missing: [true]));

        fixture.Binders.ChangeTracker.Clear();

        var stored = fixture.Binders.BinderCards.Single();

        // A pocket reserved for a card the collector does not own yet. It is on the request because
        // the claim is saved wholesale -- a client that leaves it out clears it.
        Assert.True(stored.IsMissing);
    }

    // -------------------------------------------------------------------------------------------
    // The tray, which travels whole
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task The_tray_is_replaced_by_what_the_save_carries()
    {
        using var fixture = new BinderFixture()
            .WithBinder()
            .WithTray((500, 3), (501, 1));

        // No scope to claim and none needed: the tray is the whole list every time, so a card left
        // out is one the user took out.
        var response = await HandleAsync(
            fixture,
            Save(pages: [1], tray: [(500, 1), (502, 4)]));

        var stored = await fixture.StoredTrayAsync();

        Assert.Equal(1, response.TrayAdded);
        Assert.Equal(1, response.TrayUpdated);
        Assert.Equal(1, response.TrayRemoved);
        Assert.Equal(new Dictionary<int, int> { [500] = 1, [502] = 4 }, stored);
    }

    [Fact]
    public async Task A_save_stores_both_halves()
    {
        using var fixture = new BinderFixture().WithBinder().WithTray((500, 2));

        // The reason both halves share one request: placing a card spends a copy out of the tray,
        // so a page write that commits without its tray write leaves that copy both placed and
        // still waiting.
        await HandleAsync(fixture, Save(pages: [1], cards: [(0, 500)], tray: [(500, 1)]));

        Assert.Equal(new Dictionary<int, int> { [0] = 500 }, await fixture.StoredPlacementsAsync());
        Assert.Equal(new Dictionary<int, int> { [500] = 1 }, await fixture.StoredTrayAsync());
    }

    [Fact]
    public async Task Emptying_the_tray_leaves_the_placed_cards_alone()
    {
        using var fixture = new BinderFixture()
            .WithBinder()
            .WithPlacements((0, 100))
            .WithTray((500, 2));

        var response = await HandleAsync(fixture, Save(pages: [1], cards: [(0, 100)]));

        Assert.Equal(1, response.TrayRemoved);
        Assert.Empty(await fixture.StoredTrayAsync());
        Assert.Equal(new Dictionary<int, int> { [0] = 100 }, await fixture.StoredPlacementsAsync());
    }

    // -------------------------------------------------------------------------------------------
    // What the validator refuses
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task A_card_outside_the_claimed_pages_is_refused()
    {
        using var fixture = new BinderFixture().WithBinder().WithCatalogCards(200);

        // The rule the whole-binder saves have no use for. Pocket 9 is on page 2, and this save
        // says it is editing page 1 -- writing it would put a card on a page the request never
        // claimed, which is exactly the diff's blind spot.
        var result = await ValidateAsync(fixture, Save(pages: [1], cards: [(9, 200)]));

        Assert.False(result.IsValid);
        Assert.Contains("outside the pages", result.ToString());
    }

    [Fact]
    public async Task A_card_inside_the_claimed_pages_passes()
    {
        using var fixture = new BinderFixture().WithBinder().WithCatalogCards(200);

        var result = await ValidateAsync(fixture, Save(pages: [2], cards: [(9, 200)]));

        Assert.True(result.IsValid, result.ToString());
    }

    [Fact]
    public async Task A_save_that_names_no_pages_is_refused()
    {
        using var fixture = new BinderFixture().WithBinder().WithCatalogCards(100);

        // Not read as "nothing to say about the pockets": a save that does not state its scope is
        // the one request this contract has no answer for, so it is refused rather than guessed at.
        var result = await ValidateAsync(fixture, Save(pages: [], cards: [(0, 100)]));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task A_page_beyond_the_binder_is_refused()
    {
        using var fixture = new BinderFixture().WithBinder(pages: 4);

        var result = await ValidateAsync(fixture, Save(pages: [5]));

        Assert.False(result.IsValid);
        Assert.Contains("4 pages", result.ToString());
    }

    [Fact]
    public async Task More_pages_than_a_spread_are_refused()
    {
        using var fixture = new BinderFixture().WithBinder();

        // A binder falls open two pages at a time, and a page flip flushes the pending save rather
        // than widening it, so a payload naming three pages is a client that has lost its scope.
        var result = await ValidateAsync(fixture, Save(pages: [1, 2, 3]));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task The_same_page_claimed_twice_is_refused()
    {
        using var fixture = new BinderFixture().WithBinder();

        var result = await ValidateAsync(fixture, Save(pages: [2, 2]));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Two_cards_in_one_pocket_are_refused()
    {
        using var fixture = new BinderFixture().WithBinder().WithCatalogCards(100, 200);

        // The row is keyed by binder and pocket, so the second one would collide on the primary key
        // -- and a client sending both has lost track of its own page.
        var result = await ValidateAsync(fixture, Save(pages: [1], cards: [(0, 100), (0, 200)]));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Another_users_binder_answers_like_a_missing_one()
    {
        using var fixture = new BinderFixture().WithBinder(userId: BinderFixture.OtherUserId);

        var result = await ValidateAsync(fixture, Save(pages: [1]));

        // The same message a binder that does not exist gets, so a request cannot be used to find
        // out which binder ids are real.
        Assert.False(result.IsValid);
        Assert.Contains("no longer exists", result.ToString());
    }

    [Fact]
    public async Task A_card_the_catalog_does_not_have_is_refused()
    {
        using var fixture = new BinderFixture().WithBinder().WithCatalogCards(100);

        // The two databases are separate files with no foreign key between them, so this check is
        // the only thing standing between a typo and a pocket pointing at nothing.
        var result = await ValidateAsync(fixture, Save(pages: [1], cards: [(0, 404)]));

        Assert.False(result.IsValid);
        Assert.Contains("not in the catalog", result.ToString());
    }

    [Fact]
    public async Task A_tray_card_the_catalog_does_not_have_is_refused()
    {
        using var fixture = new BinderFixture().WithBinder().WithCatalogCards(100);

        var result = await ValidateAsync(fixture, Save(pages: [1], tray: [(404, 1)]));

        Assert.False(result.IsValid);
        Assert.Contains("not in the catalog", result.ToString());
    }

    [Fact]
    public async Task A_tray_quantity_of_zero_is_refused()
    {
        using var fixture = new BinderFixture().WithBinder().WithCatalogCards(500);

        // Zero is not a second way to delete: a card the user took out is left out of the snapshot.
        var result = await ValidateAsync(fixture, Save(pages: [1], tray: [(500, 0)]));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task A_tray_bigger_than_the_workspace_allows_is_still_stored()
    {
        using var fixture = new BinderFixture().WithBinder().WithCatalogCards(500);

        // Deliberate, not an oversight: the server states no ceiling on copies or on distinct
        // cards. How big a tray is worth having is a question about the workspace, and
        // tray.svelte.ts answers it -- MAX_QUANTITY there is 999, and this save carries more.
        var request = Save(pages: [1], tray: [(500, 5_000)]);

        var result = await ValidateAsync(fixture, request);

        Assert.True(result.IsValid, result.ToString());

        await HandleAsync(fixture, request);

        Assert.Equal(new Dictionary<int, int> { [500] = 5_000 }, await fixture.StoredTrayAsync());
    }

    [Fact]
    public async Task The_same_tray_card_twice_is_refused()
    {
        using var fixture = new BinderFixture().WithBinder().WithCatalogCards(500);

        var result = await ValidateAsync(fixture, Save(pages: [1], tray: [(500, 1), (500, 2)]));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("pages")]
    [InlineData("cards")]
    [InlineData("tray")]
    public async Task A_list_the_client_sent_as_null_is_refused(string missing)
    {
        using var fixture = new BinderFixture().WithBinder();

        // The controller binds this request straight off the body and normalises nothing, so a
        // client that sent null where a list belongs gets here as null however the type is
        // declared. That has to be a refusal and not an exception: one is a 400 the client can
        // read, the other is a 500 and a log entry nobody is watching.
        var request = new SaveBinderChanges.Request
        {
            BinderId = BinderFixture.BinderId,
            Pages = missing == "pages" ? null! : [1],
            Cards = missing == "cards" ? null! : [],
            Tray = missing == "tray" ? null! : [],
        };

        var result = await ValidateAsync(fixture, request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task An_empty_save_of_a_real_page_passes()
    {
        using var fixture = new BinderFixture().WithBinder();

        // Emptying a page and emptying the tray are both legitimate edits, so a payload carrying
        // nothing but its claim has to be valid.
        var result = await ValidateAsync(fixture, Save(pages: [1]));

        Assert.True(result.IsValid, result.ToString());
    }

    // -------------------------------------------------------------------------------------------
    // The page arithmetic both halves of the slice share
    // -------------------------------------------------------------------------------------------

    [Fact]
    public void A_page_covers_the_pockets_between_its_bounds()
    {
        var pockets = SaveBinderChanges.PocketsOnPages([2], CardsPerPage);

        // Page n is (n - 1) * cardsPerPage through n * cardsPerPage - 1. The validator refuses a
        // card outside this and the handler diffs inside it, so the two have to agree.
        Assert.Equal(Enumerable.Range(9, 9), pockets.OrderBy(pocket => pocket));
    }

    [Fact]
    public void Two_pages_cover_both_pages_and_nothing_between_them()
    {
        var pockets = SaveBinderChanges.PocketsOnPages([1, 3], CardsPerPage);

        Assert.Equal(
            Enumerable.Range(0, 9).Concat(Enumerable.Range(18, 9)),
            pockets.OrderBy(pocket => pocket));
    }
}
