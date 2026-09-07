using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using PokeBinder.Auth;
using PokeBinder.Binders.DbContext;
using PokeBinder.Features.Binder.GetFullBinder;
using PokeBinder.Features.CardImages;
using PokeBinder.Features.CardSearch.GetSearchStarterFilters;
using PokeBinder.TcgCatalog.DbContext;

namespace PokeBinder.Pages;

/// <summary>
/// Hosts the binder workspace SPA, and — when the URL names a binder — hands it that binder as part
/// of the page rather than leaving the app to ask for it. The first paint then has the cards
/// already in it, with no request between opening the page and seeing the binder.
///
/// Without a binder to open there is nothing for the workspace to work on, so the page answers 404
/// and renders the reason rather than booting an app with nothing in it.
/// </summary>
[Authorize]
public class BinderModel : PageModel
{
    private readonly BinderDbContext _binderContext;
    private readonly TcgCatalogDbContext _catalogContext;
    private readonly CardImageUrls _imageUrls;
    private readonly IMemoryCache _cache;

    public BinderModel(
        BinderDbContext binderContext,
        TcgCatalogDbContext catalogContext,
        CardImageUrls imageUrls,
        IMemoryCache cache)
    {
        _binderContext = binderContext;
        _catalogContext = catalogContext;
        _imageUrls = imageUrls;
        _cache = cache;
    }

    /// <summary>
    /// Which binder to open, from the query string — the list links here as /Binder?binderId=1.
    /// Left out, the workspace opens with nothing loaded, which is what the Card Tray tab is for.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int? BinderId { get; set; }

    /// <summary>
    /// The binder handed to the client, or null when the URL named none. This is the whole
    /// GetFullBinder response, so what the page embeds and what the API would have returned are the
    /// same object.
    /// </summary>
    public GetFullBinder.Response? Binder { get; private set; }

    /// <summary>
    /// The options the advanced card search is built from, handed over with the page. The slice
    /// caches them for a day, so this costs a dictionary lookup per page load rather than the seven
    /// queries it looks like.
    /// </summary>
    public GetSearchStarterFilters.Response? SearchFilters { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (BinderId is not null)
        {
            var binder = await GetFullBinder.Handler(
                new GetFullBinder.Request { BinderId = BinderId.Value },
                User.GetUserId(),
                _binderContext,
                _catalogContext,
                _imageUrls,
                ct);

            if (binder.Found)
            {
                Binder = binder;

                // Only alongside a binder: these are for the workspace, and the 404 below boots no
                // workspace to use them.
                SearchFilters = await GetSearchStarterFilters.Handler(
                    new GetSearchStarterFilters.Request(),
                    _catalogContext,
                    _cache,
                    ct);

                return Page();
            }
        }

        // Nothing to open, whether the URL named no binder at all or named one that is missing or
        // someone else's. All three answer the same way, and that answer is a 404: the status is
        // what the URL deserves, and the page renders the reason instead of the framework's bare
        // one. Answering identically also keeps the response from confirming which ids exist.
        Response.StatusCode = StatusCodes.Status404NotFound;

        return Page();
    }
}
