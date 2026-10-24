using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;

namespace PokeBinder.Features.Binder.CreateBinderCardLabel;

/// <summary>
/// Everything that can be wrong with a new label before it is created. Runs first; the handler only
/// runs if it passes, which is why the handler writes without checking anything.
/// </summary>
/// <param name="context">Reads the user's binders and the labels already on them.</param>
/// <param name="userId">The signed-in user, from the principal rather than from the request.</param>
public class CreateBinderCardLabelValidator : AbstractValidator<CreateBinderCardLabel.Request>
{
    public CreateBinderCardLabelValidator(BinderDbContext context, int userId)
    {
        RuleFor(request => request.BinderId)
            // Someone else's binder answers the same way a missing one does, so the response cannot
            // be used to find out which binder ids exist.
            .MustAsync((binderId, ct) => context.Binders
                .AnyAsync(binder => binder.Id == binderId && binder.UserId == userId, ct))
            .WithMessage("That binder no longer exists.");

        RuleFor(request => request.Name)
            .NotEmpty()
                .WithMessage("Give the label a name.")
            .MaximumLength(CreateBinderCardLabel.MaxNameLength)
                .WithMessage($"A label name can be at most {CreateBinderCardLabel.MaxNameLength} characters.");

        RuleFor(request => request.Color)
            .NotEmpty()
                .WithMessage("Pick a colour.")
            .Must(color => color is not null && CreateBinderCardLabel.AllowedColors.Contains(color))
                .WithMessage("Pick a colour from the eight shown.");

        // Unique per binder: "Trade" on this binder cannot be created twice. Compared trimmed, with
        // the same rule the picker's duplicate hint and the schema's UNIQUE index enforce.
        RuleFor(request => request)
            .CustomAsync(async (request, validation, ct) =>
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return;
                }

                var name = request.Name.Trim();

                var taken = await context.BinderCardLabels
                    .AnyAsync(label => label.BinderId == request.BinderId && label.Name == name, ct);

                if (taken)
                {
                    validation.AddFailure(
                        nameof(CreateBinderCardLabel.Request.Name),
                        $"This binder already has a label called '{name}'.");
                }
            });
    }
}
