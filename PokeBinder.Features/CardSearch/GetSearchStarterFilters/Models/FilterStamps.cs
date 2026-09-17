using PokeBinder.TcgCatalog.DbContext.Entities;

namespace PokeBinder.Features.CardSearch.GetSearchStarterFilters.Models;

/// <summary>
/// The stamp for each filter group a browser is allowed to cache, as one object on the wire.
///
/// <para>
/// Always the complete current map, whatever the response it travels on carries. That is what the
/// client compares its stored copy against, and it is the same answer whether the page embedded it
/// or an endpoint returned it. The rule on the client side is the one that matters: write a
/// group's stamp only when writing that group's rows, so a stamp is never stored for data the
/// client does not have.
/// </para>
///
/// <para>
/// Null means the group has no row in [filterCacheStamps] at all, which nobody can cache -- nothing
/// could tell a client when the data changed. A client handed a null stamp should use the rows for
/// the session and ask again next time.
/// </para>
///
/// <para>
/// Five properties rather than a dictionary keyed by group name: this is a wire contract the client
/// has a type for, and a typed field is one the compiler checks on both sides. Adding a group here
/// is a code change on both sides anyway, since a new group needs its own storage slot in the
/// browser.
/// </para>
/// </summary>
public record FilterStamps
{
    public string? Pokemon { get; init; }

    public string? Generations { get; init; }

    public string? Series { get; init; }

    public string? Sets { get; init; }

    public string? RarityBySet { get; init; }

    /// <summary>
    /// Builds the map from whatever groups the caller has stamps for. A group missing from
    /// <paramref name="stamps"/> comes out null, which is the whole of the "no stamp" case above.
    /// </summary>
    public static FilterStamps From(IReadOnlyDictionary<FilterCacheGroup, string> stamps) =>
        new()
        {
            Pokemon = Read(stamps, FilterCacheGroup.Pokemon),
            Generations = Read(stamps, FilterCacheGroup.Generations),
            Series = Read(stamps, FilterCacheGroup.Series),
            Sets = Read(stamps, FilterCacheGroup.Sets),
            RarityBySet = Read(stamps, FilterCacheGroup.RarityBySet),
        };

    private static string? Read(IReadOnlyDictionary<FilterCacheGroup, string> stamps, FilterCacheGroup group) =>
        stamps.TryGetValue(group, out var stamp) ? stamp : null;
}
