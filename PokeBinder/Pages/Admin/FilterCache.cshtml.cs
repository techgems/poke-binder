using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PokeBinder.Components.Admin;
using PokeBinder.Features.CardAdmin.BumpFilterCacheStamps;
using PokeBinder.Features.CardAdmin.GetFilterCacheStamps;
using PokeBinder.Features.CardAdmin.GetFilterCacheStamps.Models;
using PokeBinder.TcgCatalog.DbContext;
using PokeBinder.TcgCatalog.DbContext.Entities;

namespace PokeBinder.Pages.Admin;

/// <summary>
/// The stamps that tell a browser its cached copy of the advanced-search filters is out of date,
/// and the buttons that move them.
///
/// <para>
/// Nothing derives these values, so this page is how they change: whoever loads or corrects catalog
/// data says so here, one group at a time or all five at once. It is also the only place to see
/// what each stamp currently is and when it last moved, which is what makes "did that load
/// actually invalidate anything?" a question with an answer.
/// </para>
///
/// <para>
/// Both handlers answer with the <see cref="FilterCacheStampList"/> component rather than a
/// redirect, so a bump re-renders the five rows in place and the new stamp is visible where the old
/// one was. The page renders that component through its tag helper and the handlers render its view
/// directly -- same view, same model, so the table htmx swaps in cannot drift from the one it
/// replaced.
/// </para>
/// </summary>
[Authorize]
public class FilterCacheModel : PageModel
{
    private readonly TcgCatalogDbContext _catalog;

    public FilterCacheModel(TcgCatalogDbContext catalog)
    {
        _catalog = catalog;
    }

    public IReadOnlyList<FilterCacheStampStatus> Stamps { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Stamps = await LoadAsync(ct);
    }

    /// <summary>
    /// Bumps the one group named by the posted form. The name is parsed rather than trusted: this
    /// arrives as a string from a form, and a value that is not a group is refused instead of
    /// writing a stamp nothing will ever read.
    /// </summary>
    public async Task<IActionResult> OnPostBumpAsync(string? group, CancellationToken ct)
    {
        if (!Enum.TryParse<FilterCacheGroup>(group, out var parsed) || !Enum.IsDefined(parsed))
        {
            return BadRequest($"Unknown filter group '{group}'.");
        }

        return await BumpAsync([parsed], ct);
    }

    /// <summary>
    /// Bumps every group. The escape hatch for a change that touched more than one of them, or for
    /// a database that was edited outside this page at all -- five re-fetches is a cheap way to be
    /// certain, and each client pays for them once.
    /// </summary>
    public async Task<IActionResult> OnPostBumpAllAsync(CancellationToken ct)
    {
        return await BumpAsync(Enum.GetValues<FilterCacheGroup>(), ct);
    }

    private async Task<IActionResult> BumpAsync(IReadOnlyList<FilterCacheGroup> groups, CancellationToken ct)
    {
        await BumpFilterCacheStamps.Handler(
            new BumpFilterCacheStamps.Request { Groups = groups },
            _catalog,
            ct);

        Stamps = await LoadAsync(ct);

        return Partial(FilterCacheStampList.ViewPath, new FilterCacheStampList { Stamps = Stamps });
    }

    private async Task<IReadOnlyList<FilterCacheStampStatus>> LoadAsync(CancellationToken ct) =>
        (await GetFilterCacheStamps.Handler(new GetFilterCacheStamps.Request(), _catalog, ct)).Stamps;
}
