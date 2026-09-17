using PokeBinder.TcgCatalog.DbContext.Entities;

namespace PokeBinder.Features.CardSearch.GetSearchStarterFilters.Utils;

/// <summary>
/// Which filter groups this caller's cached copies still buy it, group by group. The whole rule
/// lives here: ask this and there is nothing else to hold in your head.
///
/// <para>
/// A stamped group is sent when what the caller holds is not what the catalog currently stamps it
/// with. Null -- never stored -- is one such case and needs no rule of its own. So is a group with
/// no stamp row: nobody can claim a copy of it is current, so it always comes back.
/// </para>
///
/// <para>
/// The two unstamped groups have nothing to compare, so asking by name is all a caller can do --
/// except a cold one, which is given them without asking.
/// </para>
///
/// <para>
/// It decides and never reads: no database, no cache, nothing to await. That is what makes it the
/// half of <see cref="GetSearchStarterFilters"/> you can check by reading it.
/// </para>
/// </summary>
/// <param name="caller">What the request says it holds and what it asked for by name.</param>
/// <param name="current">The catalog's stamps as they stand this call.</param>
internal sealed class ClientCacheStamps(
    GetSearchStarterFilters.Request caller,
    IReadOnlyDictionary<FilterCacheGroup, string> current)
{
    public bool Sends(FilterCacheGroup group)
    {
        if (caller.UnstampedOnly)
        {
            return false;
        }

        var held = caller.StampFor(group);

        return held is null
            || !current.TryGetValue(group, out var stamp)
            || !string.Equals(held, stamp, StringComparison.Ordinal);
    }

    public bool SendsSuperTypes => caller.IsCold || caller.SuperTypesCacheBypass;

    public bool SendsCardTypes => caller.IsCold || caller.CardTypesCacheBypass;

    /// <summary>Whether either unstamped group is going out, and so whether to read the pair at all.</summary>
    public bool SendsAnyUnstamped => SendsSuperTypes || SendsCardTypes;
}
