using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PokeBinder.TcgCatalog.Domain.Services;
using PokeBinder.TcgCatalog.DomainModels.Queries;
using PokeBinder.TcgCatalog.DomainModels.Responses;

namespace PokeBinder.Pages;

public class CardSearchModel(CardSearchService cardSearchService) : PageModel
{
    [BindProperty]
    public CardSearchQuery Query { get; set; } = new();

    public CardSearchResponse? SearchResponse { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        SearchResponse = await cardSearchService.SearchAsync(Query, ct);
        return Page();
    }
}
