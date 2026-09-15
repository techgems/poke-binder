using Microsoft.EntityFrameworkCore;
using PokeBinder.Features.CardAdmin.GetFilterCacheStamps.Models;
using PokeBinder.TcgCatalog.DbContext;
using PokeBinder.TcgCatalog.DbContext.Entities;

namespace PokeBinder.Features.CardAdmin.GetFilterCacheStamps;

/// <summary>
/// The current cache stamps, as the admin screen reads them: every group the client may cache,
/// with the value it is being served and when that value last moved. Read-only -- moving a stamp
/// is BumpFilterCacheStamps.
/// </summary>
public static class GetFilterCacheStamps
{
    public record Request();

    public record Response(IReadOnlyList<FilterCacheStampStatus> Stamps);

    /// <summary>
    /// Answers for every <see cref="FilterCacheGroup"/> rather than for every row found, and in the
    /// enum's declared order. A group the table has no row for still comes back -- unstamped -- so
    /// the screen shows it with a Bump button instead of hiding the one group that needs one.
    /// </summary>
    public static async Task<Response> Handler(
        Request request,
        TcgCatalogDbContext context,
        CancellationToken ct = default)
    {
        // Five rows: read the table and match in memory rather than asking it about each group.
        var rows = await context.FilterCacheStamps
            .AsNoTracking()
            .ToDictionaryAsync(stamp => stamp.Group, ct);

        var stamps = Enum.GetValues<FilterCacheGroup>()
            .Select(group => rows.TryGetValue(group, out var row)
                ? new FilterCacheStampStatus
                {
                    Group = group,
                    Stamp = row.Stamp,
                    LastBumpedUnix = row.LastBumpedUnix,
                }
                : new FilterCacheStampStatus { Group = group })
            .ToList();

        return new Response(stamps);
    }
}
