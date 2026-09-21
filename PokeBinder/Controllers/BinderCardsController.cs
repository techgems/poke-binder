using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PokeBinder.Auth;
using PokeBinder.Binders.DbContext;
using PokeBinder.Features.Binder.SaveBinderChanges;
using PokeBinder.TcgCatalog.DbContext;

namespace PokeBinder.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class BinderCardsController(
    BinderDbContext binderContext,
    TcgCatalogDbContext catalogContext) : ControllerBase
{
    /// <summary>
    /// Stores one edit to a binder: the pages the user is looking at and the whole tray, both
    /// halves in one body, so one debounce tick is one request and one transaction. Two calls
    /// could half-fail, and placing a card spends a copy out of the tray -- a committed page write
    /// with a failed tray write leaves that copy both placed and still waiting.
    /// <para>
    /// The pages are the request's scope and are not optional. Within them the cards are a
    /// snapshot, so a pocket left out is one the user emptied; outside them nothing is touched.
    /// That is what makes this call cheap enough to repeat -- page two of a fifty-page binder costs
    /// one page, not fifty -- and what separates it from BindersController's whole-binder write,
    /// which replaces the lot.
    /// </para>
    /// <para>
    /// PUT rather than POST because the request replaces what is at the pages it names: sending it
    /// again unchanged writes nothing, which is what lets the client debounce it, abort it and
    /// retry it without keeping a ledger of what it has already sent. It is also what makes it safe
    /// for a closing tab to fire one with <c>keepalive</c> and never read the answer.
    /// </para>
    /// </summary>
    /// <param name="binderId">Whose binder to edit. Must be one of the caller's own.</param>
    /// <param name="body">
    /// The slice's own request. Its <see cref="SaveBinderChanges.Request.BinderId"/> is whatever
    /// the caller happened to send and is overwritten below, so the field is not part of what this
    /// endpoint asks for.
    /// </param>
    [HttpPut("{binderId:int}")]
    public async Task<ActionResult<SaveBinderChanges.Response>> Save(
        int binderId,
        [FromBody] SaveBinderChanges.Request body,
        CancellationToken ct)
    {
        var userId = User.GetUserId();

        // One id, from one place: the route's. A body that named a different binder cannot disagree
        // with the URL because the URL wins here, before anything reads the request.
        //
        // Nothing else is normalised. A body that sent null for a list arrives as null, and the
        // validator refuses it -- it has NotNull rules that would otherwise never fire, and a
        // coalesce here would turn "the client sent nonsense" into a save of an empty page.
        var request = body with { BinderId = binderId };

        var validation = await new SaveBinderChangesValidator(binderContext, catalogContext, userId)
            .ValidateAsync(request, ct);

        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation.Errors));
        }

        var response = await SaveBinderChanges.Handler(request, userId, binderContext, ct);

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
