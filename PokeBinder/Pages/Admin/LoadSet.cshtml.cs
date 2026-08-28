using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PokeBinder.Pages.Admin;

[Authorize]
public class LoadSetModel : PageModel
{
    public void OnGet()
    {
    }
}
