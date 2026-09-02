using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokeBinder.Features.CardImages;
using PokeBinder.Features.CardSearch.SearchCardsByFilter;
using PokeBinder.TcgCatalog.DbContext;

namespace PokeBinder.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class CardSearchController(
    TcgCatalogDbContext context,
    CardImageUrls imageUrls) : ControllerBase
{
    /// <summary>
    /// Runs a filtered card search. The filters travel in the body rather than the query string
    /// because there are seven of them and every one is multi-valued.
    /// </summary>
    [HttpPost("byFilter")]
    public async Task<ActionResult<SearchCardsByFilter.Response>> SearchByFilter(
        [FromBody] SearchCardsByFilter.Request request,
        CancellationToken ct)
    {
        var response = await SearchCardsByFilter.Handler(request, context, imageUrls, ct);

        return Ok(response);
    }
}
