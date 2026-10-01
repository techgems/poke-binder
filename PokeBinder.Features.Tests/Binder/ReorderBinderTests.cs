using PokeBinder.Features.Binder.ReorderBinder;
using PokeBinder.Features.Binder.ReorderBinder.Models;

namespace PokeBinder.Features.Tests.Binder;

/// <summary>
/// What ReorderBinder does to a binder.
///
/// <para>
/// The one this slice exists to get right is <see cref="Criteria_compose_like_order_by"/>: each
/// criterion only orders what the ones above it left level, so sorting by set and then rarity puts
/// every set's cards together, rarest first within each. The one it must never get wrong is
/// <see cref="A_fifty_page_binder_keeps_every_card"/>: a sort that loses a card is not a sort.
/// </para>
/// </summary>
public class ReorderBinderTests
{
    private static SortCriterion BySet(SortDirection direction) =>
        new() { Key = CriterionKey.Set, Direction = direction };

    private static SortCriterion ByRarity(SortDirection direction, RarityKey key = RarityKey.PullRate) =>
        new() { Key = CriterionKey.Rarity, Direction = direction, RarityKey = key };

    private static SortCriterion ByName(SortDirection direction) =>
        new() { Key = CriterionKey.Name, Direction = direction };

    private static ReorderBinder.Request Reorder(params SortCriterion[] criteria) =>
        new() { BinderId = BinderFixture.BinderId, Criteria = criteria };

    private static Task<ReorderBinder.Response> HandleAsync(
        BinderFixture fixture,
        ReorderBinder.Request request) =>
        ReorderBinder.Handler(request, BinderFixture.UserId, fixture.Binders, fixture.Catalog);

    private static Task<FluentValidation.Results.ValidationResult> ValidateAsync(
        BinderFixture fixture,
        ReorderBinder.Request request,
        int userId = BinderFixture.UserId) =>
        new ReorderBinderValidator(fixture.Binders, userId).ValidateAsync(request);

    /// <summary>The stored binder as card ids in pocket order, which is what a reorder is judged by.</summary>
    private static async Task<List<int>> StoredOrderAsync(BinderFixture fixture) =>
        (await fixture.StoredPlacementsAsync())
            .OrderBy(placement => placement.Key)
            .Select(placement => placement.Value)
            .ToList();

    /// <summary>
    /// Two sets a month apart, three weighted rarities each and a card of every rarity, placed out
    /// of order with gaps between them.
    /// </summary>
    private static BinderFixture TwoSets() =>
        new BinderFixture()
            .WithBinder()
            .WithSet(1, "Older Set", releaseDateUnix: 1_700_000_000)
            .WithSet(2, "Newer Set", releaseDateUnix: 1_702_600_000)
            .WithRarity(1, "Common", pullRate: 1, nameOrder: 1)
            .WithRarity(1, "Rare", pullRate: 10, nameOrder: 3)
            .WithRarity(1, "Ultra Rare", pullRate: 80, nameOrder: 5)
            .WithRarity(2, "Common", pullRate: 1, nameOrder: 1)
            .WithRarity(2, "Double Rare", pullRate: 30, nameOrder: 9)
            .WithRarity(2, "Special Illustration Rare", pullRate: 500, nameOrder: 2)
            .WithCard(11, setId: 1, "Common", "Pidgey", "20")
            .WithCard(12, setId: 1, "Rare", "Snorlax", "30")
            .WithCard(13, setId: 1, "Ultra Rare", "Gengar", "99")
            .WithCard(21, setId: 2, "Common", "Eevee", "9")
            .WithCard(22, setId: 2, "Double Rare", "Zapdos", "50")
            .WithCard(23, setId: 2, "Special Illustration Rare", "Bulbasaur", "200")
            .WithPlacements((0, 11), (4, 22), (9, 13), (13, 21), (20, 12), (35, 23));

    // -------------------------------------------------------------------------------------------
    // What the criteria mean
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Criteria_compose_like_order_by()
    {
        using var fixture = TwoSets();

        await HandleAsync(fixture, Reorder(BySet(SortDirection.Desc), ByRarity(SortDirection.Desc)));

        // The newest set first, rarest to commonest, and only then the older set, rarest to
        // commonest again -- never one rarity order across the binder with the sets mixed in.
        Assert.Equal([23, 22, 21, 13, 12, 11], await StoredOrderAsync(fixture));
    }

    [Fact]
    public async Task The_first_criterion_outranks_the_second()
    {
        using var fixture = TwoSets();

        await HandleAsync(fixture, Reorder(ByRarity(SortDirection.Desc), BySet(SortDirection.Desc)));

        // Rarity first this time, so the sets only break ties between equal weights: the two
        // commons, which share a weight of 1, come out newest set first.
        Assert.Equal([23, 13, 22, 12, 21, 11], await StoredOrderAsync(fixture));
    }

    [Fact]
    public async Task A_dropped_criterion_takes_no_part()
    {
        using var fixture = TwoSets();

        await HandleAsync(fixture, Reorder(ByRarity(SortDirection.Desc), ByName(SortDirection.Desc)));

        // Set was dropped, so nothing groups by it: the two sets interleave by rarity alone, and the
        // two commons that tie on it are split by name -- Pidgey before Eevee -- where any trace of
        // the set ordering would have put the newer set's Eevee first.
        Assert.Equal([23, 13, 22, 12, 11, 21], await StoredOrderAsync(fixture));
    }

    [Fact]
    public async Task Each_criterion_has_its_own_direction()
    {
        using var fixture = TwoSets();

        await HandleAsync(fixture, Reorder(BySet(SortDirection.Asc), ByRarity(SortDirection.Asc)));

        Assert.Equal([11, 12, 13, 21, 22, 23], await StoredOrderAsync(fixture));
    }

    [Fact]
    public async Task Rarity_reads_the_ordering_it_was_given()
    {
        using var fixture = TwoSets();

        await HandleAsync(
            fixture,
            Reorder(BySet(SortDirection.Desc), ByRarity(SortDirection.Desc, RarityKey.Name)));

        // By name order a Double Rare (9) outranks a Special Illustration Rare (2), which pull rate
        // has the other way round.
        Assert.Equal([22, 23, 21, 13, 12, 11], await StoredOrderAsync(fixture));
    }

    [Fact]
    public async Task Card_name_sorts_either_way()
    {
        using var fixture = TwoSets();

        await HandleAsync(fixture, Reorder(ByName(SortDirection.Asc)));

        Assert.Equal([23, 21, 13, 11, 12, 22], await StoredOrderAsync(fixture));

        await HandleAsync(fixture, Reorder(ByName(SortDirection.Desc)));

        Assert.Equal([22, 12, 11, 13, 21, 23], await StoredOrderAsync(fixture));
    }

    [Theory]
    [InlineData(SortDirection.Desc)]
    [InlineData(SortDirection.Asc)]
    public async Task An_unweighted_rarity_sorts_last_whichever_way_the_criterion_runs(SortDirection direction)
    {
        using var fixture = TwoSets()
            .WithRarity(1, "Secret Rare", pullRate: null)
            .WithCard(14, setId: 1, "Secret Rare", "Mew", "120")
            .WithPlacements((30, 14));

        await HandleAsync(fixture, Reorder(ByRarity(direction)));

        // Unknown is not the commonest or the rarest, so flipping the direction must not carry it
        // from the back of the binder to the front.
        Assert.Equal(14, (await StoredOrderAsync(fixture)).Last());
    }

    [Fact]
    public async Task Sets_released_the_same_day_are_kept_apart()
    {
        using var fixture = TwoSets()
            .WithSet(3, "Another Set", releaseDateUnix: 1_702_600_000)
            .WithRarity(3, "Common", pullRate: 1)
            .WithCard(31, setId: 3, "Common", "Abra", "1")
            .WithCard(32, setId: 3, "Common", "Oddish", "300")
            .WithPlacements((1, 32), (2, 31));

        await HandleAsync(fixture, Reorder(BySet(SortDirection.Desc)));

        // Sets 2 and 3 share a release date. Set name breaks it, so each set stays in one run
        // instead of the two interleaving by card number.
        Assert.Equal([31, 32, 21, 22, 23, 11, 12, 13], await StoredOrderAsync(fixture));
    }

    [Fact]
    public async Task Ties_fall_to_the_card_number_in_natural_order_and_then_the_id()
    {
        using var fixture = new BinderFixture()
            .WithBinder()
            .WithSet(1, "Set", releaseDateUnix: 1_700_000_000)
            .WithRarity(1, "Common", pullRate: 1)
            .WithCard(1, setId: 1, "Common", "A", "SV49")
            .WithCard(2, setId: 1, "Common", "B", "10")
            .WithCard(3, setId: 1, "Common", "C", "SV9")
            .WithCard(4, setId: 1, "Common", "D", "4")
            .WithCard(5, setId: 1, "Common", "E", "4")
            .WithPlacements((0, 5), (1, 1), (2, 3), (3, 2), (4, 4));

        await HandleAsync(fixture, Reorder(BySet(SortDirection.Desc), ByRarity(SortDirection.Desc)));

        // Everything ties on set and rarity. "4" before "10", "SV9" before "SV49", and the two
        // number-4 cards by id.
        Assert.Equal([4, 5, 2, 3, 1], await StoredOrderAsync(fixture));
    }

    [Fact]
    public async Task The_same_criteria_always_give_the_same_binder()
    {
        using var fixture = TwoSets();
        var criteria = Reorder(BySet(SortDirection.Desc), ByRarity(SortDirection.Desc));

        var first = await HandleAsync(fixture, criteria);
        var second = await HandleAsync(fixture, criteria);

        Assert.Equal(first.Cards, second.Cards);
    }

    // -------------------------------------------------------------------------------------------
    // What the write leaves behind
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task A_fifty_page_binder_keeps_every_card()
    {
        using var fixture = new BinderFixture()
            .WithBinder(pages: 50)
            .WithSet(1, "Set", releaseDateUnix: 1_700_000_000);

        // 300 cards scattered over 450 pockets, two copies of every card, in no particular order.
        var placements = new List<(int Pocket, int CardId)>();

        for (var cardId = 1; cardId <= 150; cardId++)
        {
            fixture.WithCard(cardId, setId: 1, "Common", $"Card {cardId * 37 % 150:D3}", $"{cardId}");
        }

        for (var pocket = 0; placements.Count < 300; pocket++)
        {
            if (pocket % 3 == 1) continue;

            placements.Add((pocket, placements.Count % 150 + 1));
        }

        fixture.WithPlacements([.. placements]);

        var before = (await fixture.StoredPlacementsAsync()).Values.Order().ToList();

        var response = await HandleAsync(fixture, Reorder(ByName(SortDirection.Asc)));

        var stored = await fixture.StoredPlacementsAsync();

        // Nothing dropped and nothing doubled: the same 300 cards.
        Assert.Equal(before, stored.Values.Order().ToList());

        // From the first pocket with no gaps.
        Assert.Equal(Enumerable.Range(0, 300), stored.Keys.Order());

        // In the new order, which is what the response says it is.
        var names = stored.OrderBy(p => p.Key).Select(p => $"Card {p.Value * 37 % 150:D3}").ToList();

        Assert.Equal(names.Order(StringComparer.Ordinal), names);
        Assert.Equal(response.Cards.Select(card => card.CardId), stored.OrderBy(p => p.Key).Select(p => p.Value));
    }

    [Fact]
    public async Task The_missing_flag_travels_with_its_card()
    {
        using var fixture = TwoSets().WithMissing(35);

        var response = await HandleAsync(fixture, Reorder(BySet(SortDirection.Desc), ByRarity(SortDirection.Desc)));

        // Card 23 was in pocket 35, flagged; it sorts first, and the flag goes with it.
        Assert.Equal([0], await fixture.StoredMissingAsync());
        Assert.True(response.Cards.Single(card => card.CardId == 23).IsMissing);
    }

    [Fact]
    public async Task The_tray_is_left_alone()
    {
        using var fixture = TwoSets().WithTray((21, 3), (12, 1));

        await HandleAsync(fixture, Reorder(ByName(SortDirection.Asc)));

        Assert.Equal(new Dictionary<int, int> { [21] = 3, [12] = 1 }, await fixture.StoredTrayAsync());
    }

    [Fact]
    public async Task A_card_the_catalog_no_longer_has_is_kept_and_goes_last()
    {
        using var fixture = TwoSets().WithPlacements((3, 404));

        await HandleAsync(fixture, Reorder(BySet(SortDirection.Desc)));

        var order = await StoredOrderAsync(fixture);

        Assert.Equal(7, order.Count);
        Assert.Equal(404, order.Last());
    }

    [Fact]
    public async Task The_response_is_the_stored_order()
    {
        using var fixture = TwoSets();

        var response = await HandleAsync(fixture, Reorder(BySet(SortDirection.Desc)));

        Assert.Equal(
            (await fixture.StoredPlacementsAsync()).OrderBy(p => p.Key).Select(p => (p.Key, p.Value)),
            response.Cards.Select(card => (card.IndexInBinder, card.CardId)));
    }

    // -------------------------------------------------------------------------------------------
    // What is refused
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task A_criteria_list_the_client_sent_as_null_is_refused()
    {
        using var fixture = new BinderFixture().WithBinder();

        // Bound straight off the body, so a null list gets here as null. A 400, not a 500.
        var result = await ValidateAsync(fixture, new ReorderBinder.Request
        {
            BinderId = BinderFixture.BinderId,
            Criteria = null!,
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task A_null_criterion_is_refused()
    {
        using var fixture = new BinderFixture().WithBinder();

        var result = await ValidateAsync(fixture, Reorder(BySet(SortDirection.Desc), null!));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task No_criteria_is_refused()
    {
        using var fixture = new BinderFixture().WithBinder();

        var result = await ValidateAsync(fixture, Reorder());

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task The_same_criterion_twice_is_refused()
    {
        using var fixture = new BinderFixture().WithBinder();

        var result = await ValidateAsync(fixture, Reorder(BySet(SortDirection.Desc), BySet(SortDirection.Asc)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Rarity_without_an_ordering_is_refused()
    {
        using var fixture = new BinderFixture().WithBinder();

        var result = await ValidateAsync(fixture, Reorder(new SortCriterion
        {
            Key = CriterionKey.Rarity,
            Direction = SortDirection.Desc,
        }));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task A_rarity_ordering_on_another_criterion_is_refused()
    {
        using var fixture = new BinderFixture().WithBinder();

        var result = await ValidateAsync(fixture, Reorder(new SortCriterion
        {
            Key = CriterionKey.Set,
            Direction = SortDirection.Desc,
            RarityKey = RarityKey.PullRate,
        }));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task All_three_criteria_pass()
    {
        using var fixture = new BinderFixture().WithBinder();

        var result = await ValidateAsync(
            fixture,
            Reorder(BySet(SortDirection.Desc), ByRarity(SortDirection.Desc), ByName(SortDirection.Asc)));

        Assert.True(result.IsValid, result.ToString());
    }

    [Fact]
    public async Task Another_users_binder_answers_like_a_missing_one()
    {
        using var fixture = new BinderFixture().WithBinder(userId: BinderFixture.OtherUserId);

        var result = await ValidateAsync(fixture, Reorder(BySet(SortDirection.Desc)));

        Assert.False(result.IsValid);
        Assert.Contains("no longer exists", result.ToString());
    }
}
