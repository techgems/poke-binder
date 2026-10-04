using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;

namespace PokeBinder.Features.Binder.CreateBinder;

/// <summary>
/// Everything that can be wrong with a new binder before it is created. This runs first and the
/// handler runs only if it passes, which is why the handler has no failure path: by the time it is
/// called, the name is a name and the size exists.
///
/// It is built per request rather than resolved as a singleton because the size rule needs the
/// database.
/// </summary>
/// <param name="context">Reads the size list.</param>
public class CreateBinderValidator : AbstractValidator<CreateBinder.Request>
{
    public CreateBinderValidator(BinderDbContext context)
    {
        RuleFor(request => request.Name)
            .NotEmpty()
                .WithMessage("Give the binder a name.")
            .MaximumLength(CreateBinder.MaxNameLength)
                .WithMessage($"A binder name can be at most {CreateBinder.MaxNameLength} characters.");

        RuleFor(request => request.Description)
            .MaximumLength(CreateBinder.MaxDescriptionLength)
                .WithMessage($"A binder description can be at most {CreateBinder.MaxDescriptionLength} characters.");

        // A request that omits the field binds it as zero, so this also catches a form that forgot
        // to send the page count the user was shown.
        RuleFor(request => request.Pages)
            .InclusiveBetween(1, CreateBinder.MaxPages)
                .WithMessage($"A binder has between 1 and {CreateBinder.MaxPages} pages.");

        RuleFor(request => request.BinderSizeId)
            .MustAsync((binderSizeId, ct) =>
                context.BinderSizes.AnyAsync(size => size.Id == binderSizeId, ct))
                .WithMessage("Choose a binder size.");
    }
}
