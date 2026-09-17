using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using PokeBinder.Features.CardSearch.GetSearchStarterFilters;
using PokeBinder.TcgCatalog.DbContext;

namespace PokeBinder.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class CardFiltersController(
    TcgCatalogDbContext context,
    IMemoryCache cache) : ControllerBase
{
    /// <summary>
    /// The advanced-search filter groups whose stamp the caller no longer has, plus the current
    /// stamp of every cacheable group. A group the caller is already current on is not returned.
    ///
    /// <para>
    /// The caller sends what it holds, one stamp per group --
    /// <c>?pokemon=b77cc92f-...&amp;sets=118e9a64-...</c> -- taken from the stamp map the page
    /// embedded and stored beside its own copy of the rows. Omitting a group, or sending it empty,
    /// says "I have no copy of this" and returns it; sending the current stamp returns nothing for
    /// it. The server does the comparing, so a client cannot get this wrong in a way that leaves it
    /// holding stale rows.
    /// </para>
    ///
    /// <para>
    /// <c>?</c> with no stamp at all is the first visit and answers with the whole object, card
    /// types and super types included, so a cold start is one request rather than two. Those two
    /// groups have no stamp to send -- they are asked for by name with
    /// <c>?superTypes=true&amp;cardTypes=true</c>, which is what the page itself does.
    /// </para>
    ///
    /// <para>
    /// A GET, and cacheable-looking, but deliberately not cached by the browser: the stamps are the
    /// invalidation story and an HTTP cache on top of them would answer with rows the stamps have
    /// already retired.
    /// </para>
    /// </summary>
    [HttpGet("starterFilters")]
    public async Task<ActionResult<GetSearchStarterFilters.Response>> GetStarterFilters(
        [FromQuery] GetSearchStarterFilters.Request request,
        CancellationToken ct)
    {
        var response = await GetSearchStarterFilters.Handler(request, context, cache, ct);

        return Ok(response);
    }
}
