using Microsoft.EntityFrameworkCore;
using PokeBinder.TcgCatalog.DbContext;
using PokeBinder.TcgCatalog.DbContext.Entities;

namespace PokeBinder.Features.CardAdmin.BumpFilterCacheStamps;

/// <summary>
/// Invalidates the browser-side copy of one or more filter groups by giving each a new stamp.
/// Every client holding the old value then disagrees with the page it is served next and re-fetches
/// that group -- and only that group.
///
/// <para>
/// The new value is a fresh GUID, and that is the whole scheme: the client compares for equality,
/// so a stamp has to be different from the last one and nothing more. Bumping a group that did not
/// need it costs one re-fetch of that group, which is why this is a button rather than a field
/// somebody types a value into.
/// </para>
///
/// <para>
/// Whoever calls this is stating that the data behind those groups has changed. Nothing here
/// checks that, because nothing can: the rows this invalidates may have been corrected by a load
/// page, by a script or by hand in the database.
/// </para>
/// </summary>
public static class BumpFilterCacheStamps
{
    /// <summary>
    /// Which groups to invalidate. A bump-all is this request carrying every group, not a mode of
    /// its own -- one group or five is the same write either way.
    /// </summary>
    public record Request
    {
        public IReadOnlyList<FilterCacheGroup> Groups { get; init; } = [];
    }

    /// <param name="Bumped">
    /// How many groups were given a new stamp, so a caller can report "3 groups bumped" without
    /// re-reading the table.
    /// </param>
    public record Response(int Bumped);

    /// <summary>
    /// One transaction for the whole request: five groups bumped together either all move or none
    /// do, so a client can never be told that four groups are stale while the fifth silently
    /// stays behind.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A group that is not a defined <see cref="FilterCacheGroup"/>. An unknown group is a bug or a
    /// tampered form, not a row to create: it would write a stamp under a name nothing reads, and
    /// the client that needed invalidating would never hear about it.
    /// </exception>
    public static async Task<Response> Handler(
        Request request,
        TcgCatalogDbContext context,
        CancellationToken ct = default)
    {
        var groups = request.Groups.Distinct().ToList();

        foreach (var group in groups)
        {
            if (!Enum.IsDefined(group))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    group,
                    "Not a filter group that can be cached.");
            }
        }

        if (groups.Count == 0)
        {
            return new Response(0);
        }

        var existing = await context.FilterCacheStamps
            .Where(stamp => groups.Contains(stamp.Group))
            .ToListAsync(ct);

        var bumpedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (var group in groups)
        {
            var row = existing.FirstOrDefault(stamp => stamp.Group == group);

            if (row is null)
            {
                // A known group with no row yet -- created here rather than refused, so the admin
                // screen's Bump button is also what fixes a group that was never seeded.
                context.FilterCacheStamps.Add(new FilterCacheStamp
                {
                    Group = group,
                    Stamp = NewStamp(),
                    LastBumpedUnix = bumpedAt,
                });

                continue;
            }

            row.Stamp = NewStamp();
            row.LastBumpedUnix = bumpedAt;
        }

        await context.SaveChangesAsync(ct);

        return new Response(groups.Count);
    }

    /// <summary>
    /// A value no client can already be holding. Format is incidental -- it is never parsed, only
    /// compared -- but a GUID makes "this is not a version number" plain at a glance.
    /// </summary>
    private static string NewStamp() => Guid.NewGuid().ToString();
}
