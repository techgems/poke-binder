using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PokeBinder.Features.CardSearch.SearchCardsByFilter;

namespace PokeBinder.Pages;

public class CardSearchModel : PageModel
{
    [BindProperty]
    public SearchCardsByFilter.Request Query { get; set; } = new();

    public SearchCardsByFilter.Response? SearchResponse { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await Task.CompletedTask;
        return Page();
    }
}
