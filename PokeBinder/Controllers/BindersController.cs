using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PokeBinder.Auth;
using PokeBinder.Binders.DbContext;
using PokeBinder.Features.Binder.DeleteBinder;
using PokeBinder.Features.Binder.UpdateBinder;
using PokeBinder.TcgCatalog.DbContext;

namespace PokeBinder.Controllers;

/// <summary>
/// The binder itself, as opposed to what is in it: BinderCardsController owns the pockets and the
/// tray, this owns the binder they belong to.
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class BindersController(
    BinderDbContext binderContext,
    TcgCatalogDbContext catalogContext) : ControllerBase
{
    /// <summary>
    /// Saves an edit to a binder -- name, description, grid, page count -- and, when the new size
    /// moves the cards already placed, what happens to them, all in one transaction.
    /// <para>
    /// PUT: the body is the binder's settings as they should now stand. The rearrangement it may
    /// carry is the one part that depends on what the binder held when it arrived, which is why
    /// the client settles its pending page save before sending this.
    /// </para>
    /// </summary>
    /// <param name="binderId">The binder to edit. Must be one of the caller's own.</param>
    /// <param name="body">
    /// The slice's own request. Its <see cref="UpdateBinder.Request.BinderId"/> is overwritten by
    /// the route's, so the field is not part of what this endpoint asks for.
    /// </param>
    [HttpPut("{binderId:int}")]
    public async Task<ActionResult<UpdateBinder.Response>> Update(
        int binderId,
        [FromBody] UpdateBinder.Request body,
        CancellationToken ct)
    {
        var userId = User.GetUserId();

        // The URL wins, before anything reads the request. Nothing else is normalised: a null
        // criteria list on a sort is the validator's to refuse.
        var request = body with { BinderId = binderId };

        var validation = await new UpdateBinderValidator(binderContext, userId).ValidateAsync(request, ct);

        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation.Errors));
        }

        var response = await UpdateBinder.Handler(request, userId, binderContext, catalogContext, ct);

        return Ok(response);
    }

    /// <summary>
    /// Deletes a binder with its placed cards and its tray. There is no undo: the workspace asks
    /// first, and leaves for My Binders when this succeeds.
    /// </summary>
    /// <param name="binderId">The binder to delete. Must be one of the caller's own.</param>
    [HttpDelete("{binderId:int}")]
    public async Task<IActionResult> Delete(int binderId, CancellationToken ct)
    {
        var userId = User.GetUserId();

        // No body: the route's id is the whole request.
        var request = new DeleteBinder.Request { BinderId = binderId };

        var validation = await new DeleteBinderValidator(binderContext, userId).ValidateAsync(request, ct);

        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation.Errors));
        }

        await DeleteBinder.Handler(request, userId, binderContext, ct);

        return NoContent();
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
