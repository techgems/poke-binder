using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;

namespace PokeBinder.Features.Binder.SaveBinder;

/// <summary>
/// Everything that can be wrong with a save before it is attempted. This runs first and the
/// handler runs only if it passes, which is why the handler has no failure path: by the time it is
/// called, the name is a name, the size exists, and the binder being edited is one this user has.
///
/// It is built per request rather than resolved as a singleton because two of its rules need the
/// database and one needs the caller's identity -- the same two things the handler is given.
/// </summary>
/// <param name="context">Reads the rules that depend on stored state: the size list, the user's
/// binders, and the cards already placed in the one being edited.</param>
/// <param name="userId">
/// The signed-in user, from the principal. It is not on the request -- a payload that could name
/// its own owner could edit anyone's binder -- so the validator has to be told separately.
/// </param>
public class SaveBinderValidator : AbstractValidator<SaveBinder.Request>
{
    public SaveBinderValidator(BinderDbContext context, int userId)
    {
        RuleFor(request => request.Name)
            .NotEmpty()
                .WithMessage("Give the binder a name.")
            .MaximumLength(SaveBinder.MaxNameLength)
                .WithMessage($"A binder name can be at most {SaveBinder.MaxNameLength} characters.");

        RuleFor(request => request.Description)
            .MaximumLength(SaveBinder.MaxDescriptionLength)
                .WithMessage($"A binder description can be at most {SaveBinder.MaxDescriptionLength} characters.");

        // A request that omits the field binds it as zero, so this also catches a form that forgot
        // to send the page count the user was shown.
        RuleFor(request => request.Pages)
            .InclusiveBetween(1, SaveBinder.MaxPages)
                .WithMessage($"A binder has between 1 and {SaveBinder.MaxPages} pages.");

        RuleFor(request => request.BinderSizeId)
            .MustAsync((binderSizeId, ct) =>
                context.BinderSizes.AnyAsync(size => size.Id == binderSizeId, ct))
                .WithMessage("Choose a binder size.");

        // Editing an existing binder, rather than creating one.
        When(request => request.Id is not null, () =>
        {
            RuleFor(request => request.Id!.Value)
                // Someone else's binder answers the same way a missing one does. Telling the
                // caller which it was would confirm that a binder with that id exists.
                .MustAsync((binderId, ct) => context.Binders
                    .AnyAsync(binder => binder.Id == binderId && binder.UserId == userId, ct))
                .WithMessage("That binder no longer exists.")
                .OverridePropertyName(nameof(SaveBinder.Request.Id));

            // A smaller grid or fewer pages moves the last pocket inwards, and any card already
            // placed past the new last pocket would have nowhere to be. Rather than dropping those
            // cards as a side effect of renaming a binder, the save is refused and the user is
            // told what is in the way; emptying those slots is SaveBinderChanges' job.
            //
            // A rule rather than a Must so the message can carry the two counts, which are only
            // known once the query has run.
            RuleFor(request => request)
                .CustomAsync(async (request, validation, ct) =>
                {
                    var capacity = await context.BinderSizes
                        .Where(size => size.Id == request.BinderSizeId)
                        .Select(size => size.X * size.Y * request.Pages)
                        .FirstOrDefaultAsync(ct);

                    // No size, or a page count already reported above: those rules own the message.
                    if (capacity <= 0)
                    {
                        return;
                    }

                    // Slots are numbered from zero, so the pocket count is also the first index
                    // outside the binder. Scoped to the owner as well as to the binder: without
                    // that, a request naming someone else's binder id would be told how many cards
                    // sit past the pocket it named, which is a fact about a stranger's collection.
                    var strandedCards = await context.BinderCards
                        .CountAsync(card => card.BinderId == request.Id
                            && card.Binder!.UserId == userId
                            && card.IndexInBinder >= capacity, ct);

                    if (strandedCards == 0)
                    {
                        return;
                    }

                    validation.AddFailure(
                        nameof(SaveBinder.Request.Pages),
                        $"This binder holds {capacity} cards at that size, and {strandedCards} " +
                        $"{(strandedCards == 1 ? "card sits" : "cards sit")} past that. Move or " +
                        "remove them first.");
                });
        });
    }
}
