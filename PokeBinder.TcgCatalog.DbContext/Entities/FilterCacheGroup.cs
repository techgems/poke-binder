namespace PokeBinder.TcgCatalog.DbContext.Entities;

/// <summary>
/// The advanced-search filter groups the browser caches, and therefore the only groups that carry
/// a stamp. One member per row of [filterCacheStamps].
/// <para>
/// Card types and super types are not here on purpose. They are a handful of rows that change
/// effectively never, so they stay embedded in the page and stay out of this machinery entirely --
/// caching them would mean caching them forever, with no way to reach a client that already holds
/// the old copy.
/// </para>
/// <para>
/// Stored by name, not by number: [groupName] holds "Pokemon", "RarityBySet" and so on. Renaming a
/// member is a data migration, and reordering them is free.
/// </para>
/// </summary>
public enum FilterCacheGroup
{
    /// <summary>The pokemon list -- 1162 entries, and the bulk of the payload. Changes on a new game.</summary>
    Pokemon,

    /// <summary>Changes with a new generation, so years apart.</summary>
    Generations,

    /// <summary>Changes every two to three years.</summary>
    Series,

    /// <summary>Changes with each release, so every two to three months.</summary>
    Sets,

    /// <summary>The rarities available within each set; moves whenever <see cref="Sets"/> does.</summary>
    RarityBySet,
}
