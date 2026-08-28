using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PokeBinder.Pages.Admin;

[Authorize]
public class CreateCardListModel : PageModel
{
    public void OnGet()
    {
    }
}
