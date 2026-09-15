using PokeBinder.Features.CardAdmin.GetFilterCacheStamps.Models;
using TechGems.StaticComponents;

namespace PokeBinder.Components.Admin;

/// <summary>
/// The filter-cache stamps as a table: one row per group, showing the value being served, when it
/// last moved, and the button that moves it.
///
/// <para>
/// It takes the read slice's own projection rather than a row of scalars per group, because the
/// whole element is what htmx swaps -- the table has to be able to draw itself from one call, both
/// when the page renders it and when a bump handler answers with it. A component per row would put
/// the loop back in the page, where the handler cannot reach it.
/// </para>
/// </summary>
public class FilterCacheStampList : StaticNode
{
    /// <summary>
    /// The id of the element htmx replaces, and the one thing about this component the page has to
    /// know: its bump-all button targets the same element the rows inside here do.
    /// </summary>
    public const string SwapTargetId = "filter-cache-stamps";

    /// <summary>
    /// This component's view, for a page handler that has to answer an htmx post with the table
    /// rather than with a whole page. Named here so the handler and the component cannot drift
    /// apart over a moved file.
    /// </summary>
    public const string ViewPath = "~/Components/Admin/FilterCacheStampList.cshtml";

    public IReadOnlyList<FilterCacheStampStatus> Stamps { get; set; } = [];

    /// <summary>
    /// Absolute, UTC, and to the minute. A stamp is a fact about the catalog rather than about the
    /// reader's day, and a bump nobody remembers making is exactly what this column is for.
    /// </summary>
    public static string LastBumped(long? unixSeconds) =>
        unixSeconds is null
            ? "never"
            : DateTimeOffset.FromUnixTimeSeconds(unixSeconds.Value).UtcDateTime.ToString("yyyy-MM-dd HH:mm") + " UTC";

    /// <summary>
    /// How long ago that was, in the roundest terms that are still true -- it answers "is this
    /// stamp from today's load or from last spring?" at a glance, which the timestamp above does
    /// not. Empty for a group that has never been stamped.
    /// </summary>
    public static string Elapsed(long? unixSeconds)
    {
        if (unixSeconds is null)
        {
            return string.Empty;
        }

        var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(unixSeconds.Value);

        return age switch
        {
            { TotalMinutes: < 2 } => "just now",
            { TotalHours: < 1 } => $"{(int)age.TotalMinutes} minutes ago",
            { TotalHours: < 2 } => "an hour ago",
            { TotalDays: < 1 } => $"{(int)age.TotalHours} hours ago",
            { TotalDays: < 2 } => "yesterday",
            _ => $"{(int)age.TotalDays} days ago",
        };
    }
}
