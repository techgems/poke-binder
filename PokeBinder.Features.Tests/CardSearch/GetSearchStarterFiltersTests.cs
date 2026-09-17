using PokeBinder.Features.CardSearch.GetSearchStarterFilters;
using PokeBinder.TcgCatalog.DbContext.Entities;

namespace PokeBinder.Features.Tests.CardSearch;

/// <summary>
/// What GetSearchStarterFilters answers, for each kind of caller.
///
/// <para>
/// These are here to survive a rewrite of the slice. Every test states a rule the browser-side
/// cache depends on -- what a caller is entitled to, what a stamp means, when the catalog is read
/// again -- and none of them reach for the slice's internals, so a refactor that keeps the
/// behaviour keeps the tests.
/// </para>
/// </summary>
public class GetSearchStarterFiltersTests
{
    private static GetSearchStarterFilters.Request Holding(
        string? pokemon = null,
        string? generations = null,
        string? series = null,
        string? sets = null,
        string? rarityBySet = null,
        bool superTypes = false,
        bool cardTypes = false) =>
        new()
        {
            PokemonCacheStamp = pokemon,
            GenerationsCacheStamp = generations,
            SeriesCacheStamp = series,
            SetsCacheStamp = sets,
            RarityBySetCacheStamp = rarityBySet,
            SuperTypesCacheBypass = superTypes,
            CardTypesCacheBypass = cardTypes,
        };

    /// <summary>Every stamp the catalog currently holds, as a caller that is fully up to date would send.</summary>
    private static GetSearchStarterFilters.Request HoldingEverything() =>
        Holding(
            pokemon: CatalogFixture.StampFor(FilterCacheGroup.Pokemon),
            generations: CatalogFixture.StampFor(FilterCacheGroup.Generations),
            series: CatalogFixture.StampFor(FilterCacheGroup.Series),
            sets: CatalogFixture.StampFor(FilterCacheGroup.Sets),
            rarityBySet: CatalogFixture.StampFor(FilterCacheGroup.RarityBySet));

    private static Task<GetSearchStarterFilters.Response> HandleAsync(
        CatalogFixture fixture,
        GetSearchStarterFilters.Request request) =>
        GetSearchStarterFilters.Handler(request, fixture.Context, fixture.Cache);

    // -------------------------------------------------------------------------------------------
    // Who gets what
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task A_caller_holding_nothing_and_naming_nothing_gets_every_group()
    {
        using var fixture = new CatalogFixture().WithCatalog().WithStamps();

        var response = await HandleAsync(fixture, Holding());

        // The first visit: one request has to be enough to fill an empty store, the two groups that
        // are never cached included.
        Assert.NotNull(response.Pokemon);
        Assert.NotNull(response.Generations);
        Assert.NotNull(response.Series);
        Assert.NotNull(response.Sets);
        Assert.NotNull(response.RarityBySet);
        Assert.NotNull(response.SuperTypes);
        Assert.NotNull(response.CardType);
    }

    [Fact]
    public async Task A_caller_naming_only_the_unstamped_groups_gets_no_stamped_group()
    {
        using var fixture = new CatalogFixture().WithCatalog().WithStamps();

        // The page: it embeds the two unstamped groups and the stamp map, and holds no stamps
        // because it is not a browser. Holding nothing must not read as "send me everything" here,
        // or the page goes back to embedding 141 KB.
        var response = await HandleAsync(fixture, Holding(superTypes: true, cardTypes: true));

        Assert.NotNull(response.SuperTypes);
        Assert.NotNull(response.CardType);

        Assert.Null(response.Pokemon);
        Assert.Null(response.Generations);
        Assert.Null(response.Series);
        Assert.Null(response.Sets);
        Assert.Null(response.RarityBySet);
    }

    [Fact]
    public async Task A_current_stamp_leaves_its_group_out()
    {
        using var fixture = new CatalogFixture().WithCatalog().WithStamps();

        var response = await HandleAsync(fixture, HoldingEverything());

        Assert.Null(response.Pokemon);
        Assert.Null(response.Generations);
        Assert.Null(response.Series);
        Assert.Null(response.Sets);
        Assert.Null(response.RarityBySet);
    }

    [Fact]
    public async Task A_stamp_the_catalog_no_longer_recognises_brings_its_group_back()
    {
        using var fixture = new CatalogFixture().WithCatalog().WithStamps();

        var request = HoldingEverything() with { SetsCacheStamp = "a stamp from last year" };

        var response = await HandleAsync(fixture, request);

        Assert.Equal(3, response.Sets?.Count);

        // And only that group: the other four stamps still hold.
        Assert.Null(response.Pokemon);
        Assert.Null(response.Generations);
        Assert.Null(response.Series);
        Assert.Null(response.RarityBySet);
    }

    [Fact]
    public async Task A_group_no_stamp_was_sent_for_comes_back_even_when_the_others_are_current()
    {
        using var fixture = new CatalogFixture().WithCatalog().WithStamps();

        // Nothing stored for pokemon -- a cleared entry, or one that failed to write -- so there is
        // no stamp to send for it and the rows have to come back.
        var request = HoldingEverything() with { PokemonCacheStamp = null };

        var response = await HandleAsync(fixture, request);

        Assert.Equal(3, response.Pokemon?.Count);
        Assert.Null(response.Sets);
    }

    [Fact]
    public async Task A_group_that_is_not_sent_is_null_rather_than_an_empty_list()
    {
        using var fixture = new CatalogFixture().WithCatalog().WithStamps();

        var response = await HandleAsync(fixture, HoldingEverything());

        // "You already have this" and "there are none of these" have to stay different answers: a
        // client that reads the second for the first overwrites a good copy with nothing.
        Assert.Null(response.Sets);
    }

    // -------------------------------------------------------------------------------------------
    // The stamp map
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Every_response_carries_the_stamp_of_every_group_whatever_it_returns()
    {
        using var fixture = new CatalogFixture().WithCatalog().WithStamps();

        var carryingNothing = await HandleAsync(fixture, HoldingEverything());
        var carryingEverything = await HandleAsync(fixture, Holding());

        foreach (var response in new[] { carryingNothing, carryingEverything })
        {
            Assert.Equal(CatalogFixture.StampFor(FilterCacheGroup.Pokemon), response.Stamps.Pokemon);
            Assert.Equal(CatalogFixture.StampFor(FilterCacheGroup.Generations), response.Stamps.Generations);
            Assert.Equal(CatalogFixture.StampFor(FilterCacheGroup.Series), response.Stamps.Series);
            Assert.Equal(CatalogFixture.StampFor(FilterCacheGroup.Sets), response.Stamps.Sets);
            Assert.Equal(CatalogFixture.StampFor(FilterCacheGroup.RarityBySet), response.Stamps.RarityBySet);
        }
    }

    [Fact]
    public async Task A_group_with_no_stamp_row_is_served_with_a_null_stamp()
    {
        using var fixture = new CatalogFixture()
            .WithCatalog()
            .WithStamps(FilterCacheGroup.Pokemon, FilterCacheGroup.Generations, FilterCacheGroup.Series, FilterCacheGroup.Sets);

        var response = await HandleAsync(fixture, HoldingEverything());

        // Nobody can claim a copy of an unstamped group is current, so the rows come back and the
        // map says so. A client reading a null stamp is being told not to store what it was given.
        Assert.NotNull(response.RarityBySet);
        Assert.Null(response.Stamps.RarityBySet);
    }

    // -------------------------------------------------------------------------------------------
    // What the cache does, and when the catalog is read again
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task A_group_is_served_from_cache_while_its_stamp_is_unchanged()
    {
        using var fixture = new CatalogFixture().WithCatalog().WithStamps();

        var first = await HandleAsync(fixture, Holding());

        // Added behind the cache's back. A second call under the same stamp must not see it --
        // which is the only way, from outside, to prove the rows did not come from the catalog.
        fixture.Context.Sets.Add(new Set { Id = 99, Code = "XX", Name = "Added later", FullName = "Added later", SeriesId = 1 });
        await fixture.Context.SaveChangesAsync();

        var second = await HandleAsync(fixture, Holding(sets: "stale"));

        Assert.Equal(first.Sets?.Count, second.Sets?.Count);
        Assert.DoesNotContain(second.Sets!, set => set.Name == "Added later");
    }

    [Fact]
    public async Task A_bumped_stamp_reads_the_catalog_again()
    {
        using var fixture = new CatalogFixture().WithCatalog().WithStamps();

        await HandleAsync(fixture, Holding());

        fixture.Context.Sets.Add(new Set { Id = 99, Code = "XX", Name = "Added later", FullName = "Added later", SeriesId = 1 });
        await fixture.Context.SaveChangesAsync();

        // The stamp is part of the cache key, so moving it is what retires the old entry. Nothing
        // is evicted, and nothing needs to be: the new key simply misses.
        await fixture.BumpAsync(FilterCacheGroup.Sets, "bumped-by-the-admin-screen");

        var response = await HandleAsync(fixture, Holding(sets: CatalogFixture.StampFor(FilterCacheGroup.Sets)));

        Assert.Contains(response.Sets!, set => set.Name == "Added later");
        Assert.Equal("bumped-by-the-admin-screen", response.Stamps.Sets);
    }

    [Fact]
    public async Task A_group_with_no_stamp_row_is_never_cached()
    {
        using var fixture = new CatalogFixture()
            .WithCatalog()
            .WithStamps(FilterCacheGroup.Pokemon, FilterCacheGroup.Generations, FilterCacheGroup.Series, FilterCacheGroup.RarityBySet);

        await HandleAsync(fixture, Holding());

        fixture.Context.Sets.Add(new Set { Id = 99, Code = "XX", Name = "Added later", FullName = "Added later", SeriesId = 1 });
        await fixture.Context.SaveChangesAsync();

        var response = await HandleAsync(fixture, Holding());

        // With no stamp there is no key to cache under that anything could ever invalidate, so the
        // group is read fresh every time rather than held for a day.
        Assert.Contains(response.Sets!, set => set.Name == "Added later");
    }

    // -------------------------------------------------------------------------------------------
    // The groups themselves
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Super_types_are_the_card_types_in_use_without_the_ETL_placeholder()
    {
        using var fixture = new CatalogFixture().WithCatalog().WithStamps();

        var response = await HandleAsync(fixture, Holding(superTypes: true));

        var names = response.SuperTypes!.Select(superType => superType.Name).ToList();

        // Five cards, four distinct types, one of them the placeholder the ETL writes when the
        // source data had none. That one is never a filter.
        Assert.Equal(3, names.Count);
        Assert.Contains("Pokemon", names);
        Assert.Contains("Trainer", names);
        Assert.Contains("Energy", names);
        Assert.DoesNotContain("UNKNOWN", names);
    }

    [Fact]
    public async Task Card_types_carry_their_art_and_tolerate_a_type_without_any()
    {
        using var fixture = new CatalogFixture().WithCatalog().WithStamps();

        var response = await HandleAsync(fixture, Holding(cardTypes: true));

        Assert.Equal(2, response.CardType?.Count);
        Assert.Equal("/images/fire.png", response.CardType!.Single(type => type.Name == "Fire").ImageUrl);
        Assert.Null(response.CardType!.Single(type => type.Name == "Water").ImageUrl);
    }

    [Fact]
    public async Task Sets_carry_the_series_they_belong_to()
    {
        using var fixture = new CatalogFixture().WithCatalog().WithStamps();

        var response = await HandleAsync(fixture, Holding());

        // The filter panel narrows sets by the chosen series, so this is the one field on a set the
        // UI cannot do without.
        Assert.Equal(2, response.Sets!.Single(set => set.Name == "Neo Genesis").SeriesId);
    }

    [Fact]
    public async Task Pokemon_carry_their_generation_and_pokedex_number()
    {
        using var fixture = new CatalogFixture().WithCatalog().WithStamps();

        var response = await HandleAsync(fixture, Holding());

        var typhlosion = response.Pokemon!.Single(entry => entry.Name == "Typhlosion");

        Assert.Equal(2, typhlosion.GenerationId);
        Assert.Equal(157, typhlosion.PokedexNumber);
    }
}
