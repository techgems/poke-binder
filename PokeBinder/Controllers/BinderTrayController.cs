using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PokeBinder.Auth;
using PokeBinder.Binders.DbContext;
using PokeBinder.Features.Binder.SaveBinderTray;
using PokeBinder.Features.Binder.SaveBinderTray.Models;
using PokeBinder.TcgCatalog.DbContext;

namespace PokeBinder.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class BinderTrayController(
    BinderDbContext binderContext,
    TcgCatalogDbContext catalogContext) : ControllerBase
{
    /// <summary>
    /// Stores a binder's tray as the caller currently holds it. The body is the whole tray, not the
    /// cards that changed: sending it again with nothing changed writes nothing, which is what lets
    /// the client debounce this call and retry it without keeping a ledger of what it has already
    /// sent.
    /// <para>
    /// PUT rather than POST for the same reason — the request replaces the tray at this URL and
    /// says so. An empty array is a valid body: it empties the tray.
    /// </para>
    /// </summary>
    /// <param name="binderId">Whose tray to replace. Must be one of the caller's own binders.</param>
    [HttpPut("{binderId:int}")]
    public async Task<ActionResult<SaveBinderTray.Response>> Save(
        int binderId,
        [FromBody] IReadOnlyList<TrayCard> cards,
        CancellationToken ct)
    {
        var userId = User.GetUserId();

        // The binder comes from the route and the cards from the body, so the request the slice
        // sees is assembled here rather than posted whole. One id, from one place: a body that
        // named a different binder than the URL could not disagree with it.
        var request = new SaveBinderTray.Request
        {
            BinderId = binderId,
            Cards = cards ?? [],
        };

        var validation = await new SaveBinderTrayValidator(binderContext, catalogContext, userId)
            .ValidateAsync(request, ct);

        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation.Errors));
        }

        var response = await SaveBinderTray.Handler(request, userId, binderContext, ct);

        return Ok(response);
    }

    /// <summary>
    /// Turns the validator's failures into the shape ASP.NET already returns for a bad request, so
    /// the client has one error format to read rather than one per endpoint.
    /// </summary>
    private ModelStateDictionary ToModelState(IEnumerable<ValidationFailure> failures)
    {
        foreach (var failure in failures)
        {
            ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
        }

        return ModelState;
    }
}
