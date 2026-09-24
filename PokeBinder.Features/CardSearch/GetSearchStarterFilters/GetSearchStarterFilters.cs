using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PokeBinder.Features.CardSearch.GetSearchStarterFilters.Models;
using PokeBinder.Features.CardSearch.GetSearchStarterFilters.Utils;
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
///
/// <para>
/// The work splits in two, and the two halves are <see cref="ClientCacheStamps"/> and
/// <see cref="StampedGroups"/>: one decides which groups this caller gets, the other reads a group
/// it is getting. Nothing else in here decides anything, which is what keeps the answer to "when
/// does this group come back?" in one place.
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
    ///
    /// <para>
    /// This record says what a caller holds and nothing about what that is worth. What it is worth
    /// is <see cref="ClientCacheStamps"/>, which is the only thing that reads the members below.
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
        public bool SuperTypesCacheBypass { get; init; }

        /// <summary>As <see cref="SuperTypesCacheBypass"/>, for card types.</summary>
        public bool CardTypesCacheBypass { get; init; }

        /// <summary>
        /// The stamp this caller sent for one group, so the reading side can ask by group instead
        /// of every caller knowing which property holds which.
        /// </summary>
        internal string? StampFor(FilterCacheGroup group) => group switch
        {
            FilterCacheGroup.Pokemon => PokemonCacheStamp,
            FilterCacheGroup.Generations => GenerationsCacheStamp,
            FilterCacheGroup.Series => SeriesCacheStamp,
            FilterCacheGroup.Sets => SetsCacheStamp,
            FilterCacheGroup.RarityBySet => RarityBySetCacheStamp,

            // A group in the enum with no property here is one no caller can send a stamp for, and
            // null is what that means: nothing held, so the rows come back. Over-sending rather
            // than under-sending is the right way round for a group nobody has a field for yet.
            _ => null,
        };

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
        internal bool IsCold => HoldsNothing && !SuperTypesCacheBypass && !CardTypesCacheBypass;

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
    ///
    /// <para>
    /// Named properties rather than the seven positional parameters this used to take. Seven
    /// nullable lists in a row is seven chances to hand over the wrong one, and every one of them
    /// compiles.
    /// </para>
    /// </summary>
    public record Response
    {
        public IReadOnlyList<SuperTypeFilter>? SuperTypes { get; init; }

        public IReadOnlyList<GenerationsFilter>? Generations { get; init; }

        public IReadOnlyList<SeriesFilter>? Series { get; init; }

        public IReadOnlyList<SetsFilter>? Sets { get; init; }

        public IReadOnlyList<PokemonFilter>? Pokemon { get; init; }

        public IReadOnlyList<RarityBySetFilter>? RarityBySet { get; init; }

        public IReadOnlyList<CardTypeFilter>? CardType { get; init; }

        /// <summary>The current stamp of every cacheable group, whatever this response carries.</summary>
        public required FilterStamps Stamps { get; init; }
    }

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

        var clientCacheStamps = new ClientCacheStamps(request, stamps);
        var groups = new StampedGroups(cache, stamps, clientCacheStamps, ct);

        // The two unstamped groups share one cache entry: they are always read together, by the
        // page on every load and by a cold client once.
        var inline = clientCacheStamps.SendsAnyUnstamped
            ? await ReadInlineAsync(context, cache, ct)
            : null;

        // Each group is read where it lands, so it is named once: no reading order to keep in step
        // with an assembly order, and no group that could be filled in from the wrong query.
        return new Response
        {
            SuperTypes = clientCacheStamps.SendsSuperTypes ? inline?.SuperTypes : null,

            Generations = await groups.ReadAsync(FilterCacheGroup.Generations,
                token => context.GenerationFilterOptions.Select(MapGenerations).ToListAsync(token)),

            Series = await groups.ReadAsync(FilterCacheGroup.Series,
                token => context.Series.Select(MapSeries).ToListAsync(token)),

            Sets = await groups.ReadAsync(FilterCacheGroup.Sets,
                token => context.Sets.Select(MapSets).ToListAsync(token)),

            Pokemon = await groups.ReadAsync(FilterCacheGroup.Pokemon,
                token => context.PokemonFilterOptions.Select(MapPokemon).ToListAsync(token)),

            RarityBySet = await groups.ReadAsync(FilterCacheGroup.RarityBySet,
                token => context.RarityBySetFilterOptions.Select(MapRarities).ToListAsync(token)),

            CardType = clientCacheStamps.SendsCardTypes ? inline?.CardType : null,

            Stamps = FilterStamps.From(stamps),
        };
    }

    /// <summary>
    /// The five stamped groups: told a group and how to query it, this answers with the rows --
    /// from the cache when the stamp they were cached under is still the current one -- or with
    /// null when the caller is not being sent that group at all.
    ///
    /// <para>
    /// It asks <see cref="ClientCacheStamps"/> rather than being told, which is what lets a call site be
    /// the group and its query and nothing else.
    /// </para>
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
    private sealed class StampedGroups(
        IMemoryCache cache,
        IReadOnlyDictionary<FilterCacheGroup, string> stamps,
        ClientCacheStamps clientCacheStamps,
        CancellationToken ct)
    {
        public async Task<IReadOnlyList<T>?> ReadAsync<T>(
            FilterCacheGroup group,
            Func<CancellationToken, Task<List<T>>> query)
        {
            if (!clientCacheStamps.Sends(group))
            {
                return null;
            }

            if (!stamps.TryGetValue(group, out var stamp))
            {
                // A group with no row in [filterCacheStamps] has no handle to invalidate it by,
                // here or in the browser. So it is read fresh every time, and the stamp map says
                // null for it, which tells the client it is holding rows it cannot claim are
                // current. Bumping the group from the admin screen creates the row and ends this.
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
            Rarity = rarity.Rarity,
            PullRateRarityOrder = rarity.PullRateRarityOrder,
            NameRarityOrder = rarity.NameRarityOrder
        };

    private static readonly Expression<Func<CardTypeFilterOption, CardTypeFilter>> MapCardTypes =
        cardType => new CardTypeFilter()
        {
            Id = cardType.Id,
            Name = cardType.Name,
            ImageUrl = cardType.ImageUrl
        };
}
