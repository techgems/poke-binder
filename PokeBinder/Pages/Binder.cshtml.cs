using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PokeBinder.Pages;

[Authorize]
public class SvelteAppModel : PageModel
{
    public void OnGet()
    {
    }
}
