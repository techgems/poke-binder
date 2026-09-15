using PokeBinder.TcgCatalog.DbContext.Entities;

namespace PokeBinder.Features.CardAdmin.GetFilterCacheStamps.Models;

/// <summary>
/// One filter group as the admin screen shows it: what its stamp currently is, and when that value
/// last changed.
/// </summary>
public class FilterCacheStampStatus
{
    public FilterCacheGroup Group { get; set; }

    /// <summary>
    /// The current stamp, or null when the table holds no row for this group. Null is a real state
    /// rather than an error: the group is known to the code and has never been stamped, which is
    /// what a group added after the seed migration looks like until someone bumps it.
    /// </summary>
    public string? Stamp { get; set; }

    /// <summary>When <see cref="Stamp"/> last changed. Unix seconds, UTC; null alongside a null stamp.</summary>
    public long? LastBumpedUnix { get; set; }

    /// <summary>False for a group the table has no row for. The next bump creates it.</summary>
    public bool IsStamped => Stamp is not null;
}
