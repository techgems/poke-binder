using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PokeBinder.TcgCatalog.DomainModels.Queries;
using PokeBinder.TcgCatalog.DomainModels.Responses;

namespace PokeBinder.Pages;

public class CardSearchModel : PageModel
{
    [BindProperty]
    public CardSearchQuery Query { get; set; } = new();

    public CardSearchResponse? SearchResponse { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await Task.CompletedTask;
        return Page();
    }
}
