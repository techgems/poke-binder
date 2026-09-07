using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PokeBinder.Auth;
using PokeBinder.Binders.DbContext;
using PokeBinder.Features.Binder.SaveBinderCards;
using PokeBinder.Features.Binder.SaveBinderCards.Models;
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
    /// Stores where the cards sit in a binder. The body is every placed card, not the ones that
    /// moved: sending it again unchanged writes nothing, which is what lets the client debounce
    /// this call and retry it without tracking what it has already sent.
    /// <para>
    /// That also makes it a whole-binder call. A body carrying only the page on screen says every
    /// other page is empty, and the save will believe it — load with GetFullBinder and send back
    /// what it gave you. PUT rather than POST for the same reason: the request replaces the cards
    /// at this URL and says so. An empty array is valid and empties the binder.
    /// </para>
    /// </summary>
    /// <param name="binderId">Whose cards to replace. Must be one of the caller's own binders.</param>
    [HttpPut("{binderId:int}")]
    public async Task<ActionResult<SaveBinderCards.Response>> Save(
        int binderId,
        [FromBody] IReadOnlyList<PlacedCard> cards,
        CancellationToken ct)
    {
        var userId = User.GetUserId();

        // The binder comes from the route and the cards from the body, so the request the slice
        // sees is assembled here rather than posted whole. One id, from one place: a body that
        // named a different binder than the URL could not disagree with it.
        var request = new SaveBinderCards.Request
        {
            BinderId = binderId,
            Cards = cards ?? [],
        };

        var validation = await new SaveBinderCardsValidator(binderContext, catalogContext, userId)
            .ValidateAsync(request, ct);

        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation.Errors));
        }

        var response = await SaveBinderCards.Handler(request, userId, binderContext, ct);

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
