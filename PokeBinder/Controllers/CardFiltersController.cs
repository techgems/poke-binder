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
    [HttpGet("starterFilters")]
    public async Task<ActionResult<GetSearchStarterFilters.Response>> GetStarterFilters(CancellationToken ct)
    {
        var response = await GetSearchStarterFilters.Handler(
            new GetSearchStarterFilters.Request(),
            context,
            cache,
            ct);

        return Ok(response);
    }
}
