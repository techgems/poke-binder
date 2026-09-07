using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PokeBinder.Auth;
using PokeBinder.Binders.DbContext;
using PokeBinder.Features.Binder.GetBinderList;
using PokeBinder.Features.Binder.GetBinderList.Models;
using PokeBinder.Features.Binder.GetNewBinderOptions;
using PokeBinder.Features.Binder.GetNewBinderOptions.Models;
using PokeBinder.Features.Binder.SaveBinder;

namespace PokeBinder.Pages;

/// <summary>
/// The user's binders, and the form that adds one. Both halves of the page are fragments the
/// create handler can re-render on its own -- the list when a binder is saved, the form when the
/// validator refuses it -- which is all the page needs from HTMX.
/// </summary>
[Authorize]
public class MyBindersModel : PageModel
{
    private readonly BinderDbContext _db;

    public MyBindersModel(BinderDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<BinderListItem> Binders { get; private set; } = [];

    [BindProperty]
    public NewBinderForm Input { get; set; } = new();

    public async Task OnGetAsync(CancellationToken ct)
    {
        Binders = (await GetBinderList.Handler(new GetBinderList.Request(), User.GetUserId(), _db, ct)).Binders;
        Input.Sizes = await LoadSizesAsync(ct);
        Input.PrefillPagesFromSize();
    }

    /// <summary>
    /// Posted by the modal's form. Answers with one fragment either way: the form again when the
    /// save was refused, or the rebuilt list when it went through.
    /// </summary>
    public async Task<IActionResult> OnPostCreateAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();

        var request = new SaveBinder.Request
        {
            Name = Input.Name,
            Description = Input.Description,
            BinderSizeId = Input.BinderSizeId,
            Pages = Input.Pages,
        };

        // The slice's rules, unchanged and in one place: the page neither repeats them nor gets to
        // disagree with them.
        var validation = await new SaveBinderValidator(_db, userId).ValidateAsync(request, ct);

        if (!validation.IsValid)
        {
            Input.Sizes = await LoadSizesAsync(ct);
            Input.Errors = validation.Errors
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(failure => failure.ErrorMessage).ToList());

            // Swapped into the form's own place, so the user keeps what they typed alongside the
            // reason it was refused.
            return Partial("_NewBinderForm", Input);
        }

        await SaveBinder.Handler(request, userId, _db, ct);

        Binders = (await GetBinderList.Handler(new GetBinderList.Request(), userId, _db, ct)).Binders;

        // The form asked for the reply to land in its own place, which is right for a rejection and
        // wrong for a save. Rather than teaching the form about both outcomes, the response
        // redirects the swap: the list is what changed, and binder-saved is what closes the modal.
        Response.Headers["HX-Retarget"] = "#binder-list";
        Response.Headers["HX-Reswap"] = "outerHTML";
        Response.Headers["HX-Trigger"] = "binder-saved";

        return Partial("_BinderList", Binders);
    }

    /// <summary>
    /// The choices the create form offers. One call, whatever the form ends up asking for.
    /// </summary>
    private async Task<IReadOnlyList<BinderSizeOption>> LoadSizesAsync(CancellationToken ct) =>
        (await GetNewBinderOptions.Handler(new GetNewBinderOptions.Request(), _db, ct)).Sizes;

    /// <summary>
    /// What the create form posts, plus what it needs to draw itself again when the save is
    /// refused. Deliberately not SaveBinder.Request: this one is bound from a form, so it carries
    /// the size list and the messages, and the owner is never part of it.
    /// </summary>
    public class NewBinderForm
    {
        public string? Name { get; set; }

        public string? Description { get; set; }

        public int BinderSizeId { get; set; }

        public int Pages { get; set; }

        /// <summary>Populated by the page, not posted.</summary>
        public IReadOnlyList<BinderSizeOption> Sizes { get; set; } = [];

        /// <summary>Validation failures by property name; empty until a save is refused.</summary>
        public Dictionary<string, List<string>> Errors { get; set; } = [];

        public IReadOnlyList<string> ErrorsFor(string property) =>
            Errors.TryGetValue(property, out var messages) ? messages : [];

        /// <summary>
        /// Opens the form on the first size with its recommended page count, so the two fields
        /// agree before the user has touched either. Alpine keeps them in step from there.
        /// </summary>
        public void PrefillPagesFromSize()
        {
            var size = Sizes.FirstOrDefault();

            if (size is null)
            {
                return;
            }

            BinderSizeId = size.Id;
            Pages = size.DefaultPages;
        }
    }
}
