using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PokeBinder.Features.CardSearch.GetSearchStarterFilters.Models;
using PokeBinder.TcgCatalog.DbContext;
using PokeBinder.TcgCatalog.DbContext.Entities;
using System.Linq.Expressions;

namespace PokeBinder.Features.CardSearch.GetSearchStarterFilters;

/// <summary>
/// The options the advanced card search is built from. One slice for all seven groups: the caller
/// sends the stamps it holds, and gets back the groups those stamps no longer buy it.
///
/// <para>
/// Five of the groups are cached in the browser against a stamp, and those are the ones a caller
/// sends a stamp for. The other two -- card types and super types -- are never cached there: they
/// are a couple of kilobytes that change effectively never, and giving them a stamp would mean
/// maintaining an invalidation path for 2 KB. They travel with the page instead, and they come
/// back here for the one caller that cannot have them yet: a client with nothing stored.
/// </para>
///
/// <para>
/// Holding no stamp for anything is that client -- a first visit -- and it gets everything, the two
/// unstamped groups included, in one round trip. Holding four current stamps and one stale one gets
/// exactly the one group, which is the case this design exists for.
/// </para>
///
/// <para>
/// Every response carries the full stamp map, not just the stamps of the groups it brought. It is
/// the same map the page embeds, and it is what the client compares against next time.
/// </para>
/// </summary>
public static class GetSearchStarterFilters
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    /// <summary>Placeholder card type the ETL writes when the source data has none; never offered as a filter.</summary>
    private const string UnknownCardType = "UNKNOWN";

    /// <summary>
    /// No stamp in this key, because the two groups it holds have nothing to invalidate them with.
    /// The 24 hours is their whole staleness budget, which is the trade taken when they were left
    /// out of the stamp machinery.
    /// </summary>
    private const string InlineCacheKey = "CardSearch:Filters:Inline";

    /// <summary>
    /// What the caller already holds: one field per stamped group, carrying the stamp it stored
    /// with its copy of that group.
    ///
    /// <para>
    /// The caller states what it has and the server decides what that entitles it to, rather than
    /// the caller deciding and the server obliging. A stamp that equals the current one means the
    /// copy is good and the group is left out of the response; anything else -- a different stamp,
    /// or null for a group it has never stored -- means the copy is no use and the rows come back.
    /// A client that compares wrongly, or not at all, still gets a correct answer.
    /// </para>
    ///
    /// <para>
    /// All five null is the first visit: nothing stored, so all five groups come back, and with
    /// them the two groups that carry no stamp -- see <see cref="IsCold"/>.
    /// </para>
    /// </summary>
    public record Request
    {
        public string? PokemonCacheStamp { get; init; }

        public string? GenerationsCacheStamp { get; init; }

        public string? SeriesCacheStamp { get; init; }

        public string? SetsCacheStamp { get; init; }

        public string? RarityBySetCacheStamp { get; init; }

        /// <summary>
        /// Include the super types. There is no stamp to send for this group, so asking is all a
        /// caller can do: the page sets this because it embeds them, and a cold client is given
        /// them without asking.
        /// </summary>
        public bool SuperTypesCacheByPass { get; init; }

        /// <summary>As <see cref="SuperTypesCacheByPass"/>, for card types.</summary>
        public bool CardTypesCacheBypass { get; init; }

        /// <summary>No stamp sent for any of the five. True of a first visit and of the page alike.</summary>
        private bool HoldsNothing =>
            PokemonCacheStamp is null
            && GenerationsCacheStamp is null
            && SeriesCacheStamp is null
            && SetsCacheStamp is null
            && RarityBySetCacheStamp is null;

        /// <summary>
        /// A caller that holds nothing and asks for nothing by name: a first visit, with an empty
        /// store. It is handed the whole object -- the two unstamped groups included -- so a cold
        /// start costs one request instead of two.
        /// </summary>
        internal bool IsCold => HoldsNothing && !SuperTypesCacheByPass && !CardTypesCacheBypass;

        /// <summary>
        /// A caller that holds nothing and named the unstamped groups: the page. It embeds those
        /// two and the stamp map, and wants none of the five big groups it deliberately stopped
        /// embedding -- so holding no stamp for them means "not my business" rather than "send
        /// them", which is what it means from a browser.
        /// <para>
        /// The flags are the whole of the difference, and they are enough: a browser with an empty
        /// store has the unstamped pair from the page already and never asks for it, so it reads as
        /// cold above and gets everything.
        /// </para>
        /// </summary>
        internal bool UnstampedOnly => HoldsNothing && !IsCold;
    }

    /// <summary>
    /// A group the caller did not ask for comes back null, not empty: "not sent" and "sent, and
    /// there are none" are different answers, and a client must not overwrite a good cached copy
    /// with an empty list.
    /// </summary>
    /// <param name="Stamps">The current stamp of every cacheable group, whatever this response carries.</param>
    public record Response(
        IReadOnlyList<SuperTypeFilter>? SuperTypes,
        IReadOnlyList<GenerationsFilter>? Generations,
        IReadOnlyList<SeriesFilter>? Series,
        IReadOnlyList<SetsFilter>? Sets,
        IReadOnlyList<PokemonFilter>? Pokemon,
        IReadOnlyList<RarityBySetFilter>? RarityBySet,
        IReadOnlyList<CardTypeFilter>? CardType,
        FilterStamps Stamps
    );

    public static async Task<Response> Handler(
        Request request,
        TcgCatalogDbContext context,
        IMemoryCache cache,
        CancellationToken ct = default)
    {
        // One read of the stamp table per call, uncached on purpose: it is what the cache keys
        // below are built from, so a cached copy of it would hold every group on its old key and
        // put a window between a bump and anyone noticing.
        var stamps = await context.FilterCacheStamps
            .AsNoTracking()
            .ToDictionaryAsync(row => row.Group, row => row.Stamp, ct);

        // A group is sent when what the caller holds is not what the catalog currently stamps it
        // with. Null -- never stored -- is one such case and needs no rule of its own. So is a
        // group with no stamp row: nobody can claim a copy of it is current, so it always comes
        // back.
        bool Stale(string? held, FilterCacheGroup group) =>
            !request.UnstampedOnly
            && (held is null
                || !stamps.TryGetValue(group, out var current)
                || !string.Equals(held, current, StringComparison.Ordinal));

        var generations = await ReadAsync(
            Stale(request.GenerationsCacheStamp, FilterCacheGroup.Generations), FilterCacheGroup.Generations, stamps, cache,
            token => context.GenerationFilterOptions.Select(MapGenerations).ToListAsync(token), ct);

        var series = await ReadAsync(
            Stale(request.SeriesCacheStamp, FilterCacheGroup.Series), FilterCacheGroup.Series, stamps, cache,
            token => context.Series.Select(MapSeries).ToListAsync(token), ct);

        var sets = await ReadAsync(
            Stale(request.SetsCacheStamp, FilterCacheGroup.Sets), FilterCacheGroup.Sets, stamps, cache,
            token => context.Sets.Select(MapSets).ToListAsync(token), ct);

        var pokemon = await ReadAsync(
            Stale(request.PokemonCacheStamp, FilterCacheGroup.Pokemon), FilterCacheGroup.Pokemon, stamps, cache,
            token => context.PokemonFilterOptions.Select(MapPokemon).ToListAsync(token), ct);

        var rarities = await ReadAsync(
            Stale(request.RarityBySetCacheStamp, FilterCacheGroup.RarityBySet), FilterCacheGroup.RarityBySet, stamps, cache,
            token => context.RarityBySetFilterOptions.Select(MapRarities).ToListAsync(token), ct);

        // The two unstamped groups share one cache entry: they are always read together, by the
        // page on every load and by a cold client once.
        var cold = request.IsCold;

        var inline = cold || request.SuperTypesCacheByPass || request.CardTypesCacheBypass
            ? await ReadInlineAsync(context, cache, ct)
            : null;

        return new Response(
            cold || request.SuperTypesCacheByPass ? inline?.SuperTypes : null,
            generations,
            series,
            sets,
            pokemon,
            rarities,
            cold || request.CardTypesCacheBypass ? inline?.CardType : null,
            FilterStamps.From(stamps));
    }

    /// <summary>
    /// One group, from the cache when the stamp it was cached under is still the current one.
    ///
    /// <para>
    /// The stamp is <em>in the key</em> rather than being something a bump evicts. IMemoryCache is
    /// per process, so eviction would only ever clear the instance that handled the bump and leave
    /// every other one serving the old rows for the rest of the day. A stamped key misses on every
    /// instance at once, and the entries nobody asks for any more age out on their own.
    /// </para>
    ///
    /// <para>
    /// Which is also why these entries are evictable. The old single key held the whole response at
    /// Priority = NeverRemove, which was safe when there was exactly one of it; now that a bump
    /// mints a new key, pinning them would leave the dead ones holding memory for a day with no way
    /// to reclaim it under pressure.
    /// </para>
    /// </summary>
    private static async Task<IReadOnlyList<T>?> ReadAsync<T>(
        bool wanted,
        FilterCacheGroup group,
        IReadOnlyDictionary<FilterCacheGroup, string> stamps,
        IMemoryCache cache,
        Func<CancellationToken, Task<List<T>>> query,
        CancellationToken ct)
    {
        if (!wanted)
        {
            return null;
        }

        if (!stamps.TryGetValue(group, out var stamp))
        {
            // A group with no row in [filterCacheStamps] has no handle to invalidate it by, here or
            // in the browser. So it is read fresh every time, and the stamp map says null for it,
            // which tells the client it is holding rows it cannot claim are current. Bumping the
            // group from the admin screen creates the row and ends this.
            return await query(ct);
        }

        var key = $"CardSearch:Filters:{group}:{stamp}";

        if (cache.TryGetValue(key, out List<T>? cached) && cached is not null)
        {
            return cached;
        }

        var rows = await query(ct);

        cache.Set(key, rows, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration,
        });

        return rows;
    }

    /// <summary>The two groups with no stamp, read and cached together under a key that has none.</summary>
    private static async Task<InlineFilters> ReadInlineAsync(
        TcgCatalogDbContext context,
        IMemoryCache cache,
        CancellationToken ct)
    {
        if (cache.TryGetValue(InlineCacheKey, out InlineFilters? cached) && cached is not null)
        {
            return cached;
        }

        var superTypes = await QuerySuperTypesAsync(context, ct);
        var cardTypes = await context.CardTypeFilterOptions.Select(MapCardTypes).ToListAsync(ct);

        var inline = new InlineFilters(superTypes, cardTypes);

        cache.Set(InlineCacheKey, inline, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration,
        });

        return inline;
    }

    /// <summary>Cache shape for the pair above, so one lookup answers for both.</summary>
    private record InlineFilters(
        IReadOnlyList<SuperTypeFilter> SuperTypes,
        IReadOnlyList<CardTypeFilter> CardType);

    // Super types have no table of their own, so they are read as the distinct card types in use.
    private static async Task<List<SuperTypeFilter>> QuerySuperTypesAsync(TcgCatalogDbContext context, CancellationToken ct)
    {
        var names = await context.Cards
            .Where(card => card.CardType != UnknownCardType)
            .Select(card => card.CardType)
            .Distinct()
            .ToListAsync(ct);

        return names.Select(name => new SuperTypeFilter() { Name = name }).ToList();
    }

    private static readonly Expression<Func<GenerationFilterOption, GenerationsFilter>> MapGenerations =
        series => new GenerationsFilter()
        {
            Id = series.Id,
            Name = series.Name
        };

    private static readonly Expression<Func<Series, SeriesFilter>> MapSeries =
        series => new SeriesFilter()
        {
            Id = series.Id,
            Name = series.Name
        };

    private static readonly Expression<Func<Set, SetsFilter>> MapSets =
        set => new SetsFilter()
        {
            Id = set.Id,
            Name = set.Name,
            SeriesId = set.SeriesId
        };

    private static readonly Expression<Func<PokemonFilterOption, PokemonFilter>> MapPokemon =
        pokemon => new PokemonFilter()
        {
            Id = pokemon.Id,
            PokedexNumber = pokemon.PokedexNumber,
            Name = pokemon.Name,
            GenerationId = pokemon.GenerationId,
            AlternateName = pokemon.AlternateName
        };

    private static readonly Expression<Func<RarityBySetFilterOption, RarityBySetFilter>> MapRarities =
        rarity => new RarityBySetFilter()
        {
            Id = rarity.Id,
            SetId = rarity.SetId,
            Rarity = rarity.Rarity
        };

    private static readonly Expression<Func<CardTypeFilterOption, CardTypeFilter>> MapCardTypes =
        cardType => new CardTypeFilter()
        {
            Id = cardType.Id,
            Name = cardType.Name,
            ImageUrl = cardType.ImageUrl
        };
}
