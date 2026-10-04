using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;
using PokeBinder.Features.Binder.Shared.Arrangement;
using PokeBinder.Features.Binder.UpdateBinder.Models;

namespace PokeBinder.Features.Binder.UpdateBinder;

/// <summary>
/// Everything that can be wrong with a binder edit before it is attempted. This runs first and the
/// handler runs only if it passes, which is why the handler has no failure path.
///
/// <para>
/// What a binder can be -- the name's length, the description's, the page range -- is CreateBinder's
/// to say, since creating one and editing one have to agree. What is particular to an edit is the
/// cards already placed: the new size has to hold them, unless they are leaving.
/// </para>
/// </summary>
/// <param name="context">The size list, the user's binders, and the cards placed in the one being edited.</param>
/// <param name="userId">The signed-in user, from the principal rather than from the request.</param>
public class UpdateBinderValidator : AbstractValidator<UpdateBinder.Request>
{
    public UpdateBinderValidator(BinderDbContext context, int userId)
    {
        RuleFor(request => request.BinderId)
            // Someone else's binder answers the same way a missing one does, so a request cannot be
            // used to find out which binder ids exist.
            .MustAsync((binderId, ct) => context.Binders
                .AnyAsync(binder => binder.Id == binderId && binder.UserId == userId, ct))
            .WithMessage("That binder no longer exists.");

        RuleFor(request => request.Name)
            .NotEmpty()
                .WithMessage("Give the binder a name.")
            .MaximumLength(CreateBinder.CreateBinder.MaxNameLength)
                .WithMessage($"A binder name can be at most {CreateBinder.CreateBinder.MaxNameLength} characters.");

        RuleFor(request => request.Description)
            .MaximumLength(CreateBinder.CreateBinder.MaxDescriptionLength)
                .WithMessage($"A binder description can be at most {CreateBinder.CreateBinder.MaxDescriptionLength} characters.");

        RuleFor(request => request.Pages)
            .InclusiveBetween(1, CreateBinder.CreateBinder.MaxPages)
                .WithMessage($"A binder has between 1 and {CreateBinder.CreateBinder.MaxPages} pages.");

        RuleFor(request => request.BinderSizeId)
            .MustAsync((binderSizeId, ct) =>
                context.BinderSizes.AnyAsync(size => size.Id == binderSizeId, ct))
                .WithMessage("Choose a binder size.");

        RuleFor(request => request.Rearrange)
            .IsInEnum()
            .When(request => request.Rearrange is not null);

        // Cascade.Stop because the controller binds the request straight off the body: a sort that
        // sent null for its criteria arrives here as null, and the rules after NotNull count it.
        When(request => request.Rearrange == Rearrangement.Sort, () =>
        {
            RuleFor(request => request.Criteria)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                    .WithMessage("A sort has to say what it sorts by.")
                .MakeASort();

            RuleForEach(request => request.Criteria).ChildRules(SortCriteriaRules.Criterion);
        });

        // Criteria on anything but a sort are a client that has confused its choices, not something
        // to ignore quietly.
        RuleFor(request => request.Criteria)
            .Must(criteria => criteria is null || criteria.Count == 0)
            .When(request => request.Rearrange != Rearrangement.Sort)
            .WithMessage("Only a sort takes criteria.");

        // Whether the cards fit. A rule rather than a Must so the message can carry the counts,
        // which are only known once the queries have run. Scoped to the owner as well as to the
        // binder: without that, a request naming someone else's binder id would be told how many
        // cards are in it, which is a fact about a stranger's collection.
        RuleFor(request => request)
            .CustomAsync(async (request, validation, ct) =>
            {
                // Emptied, the binder holds nothing, so any size fits.
                if (request.Rearrange is Rearrangement.Tray or Rearrangement.Drop)
                {
                    return;
                }

                var capacity = await context.BinderSizes
                    .Where(size => size.Id == request.BinderSizeId)
                    .Select(size => size.X * size.Y * request.Pages)
                    .FirstOrDefaultAsync(ct);

                // No size, or a page count already reported above: those rules own the message.
                if (capacity <= 0)
                {
                    return;
                }

                var placed = context.BinderCards
                    .Where(card => card.BinderId == request.BinderId && card.Binder!.UserId == userId);

                // Laid out again from the first pocket, every card needs a pocket of its own.
                if (request.Rearrange is Rearrangement.Keep or Rearrangement.Sort)
                {
                    var count = await placed.CountAsync(ct);

                    if (count > capacity)
                    {
                        validation.AddFailure(
                            nameof(UpdateBinder.Request.Pages),
                            $"This binder holds {capacity} cards at that size, and {count} " +
                            $"{(count == 1 ? "card is" : "cards are")} placed in it. Add pages or " +
                            "pick a bigger grid.");
                    }

                    return;
                }

                // Left where they are, every card needs its own pocket still to exist. Slots are
                // numbered from zero, so the pocket count is also the first index outside the
                // binder. Refused rather than dropped: losing cards as a side effect of renaming a
                // binder is not something a request should be able to do without saying so.
                var stranded = await placed.CountAsync(card => card.IndexInBinder >= capacity, ct);

                if (stranded > 0)
                {
                    validation.AddFailure(
                        nameof(UpdateBinder.Request.Pages),
                        $"This binder holds {capacity} cards at that size, and {stranded} " +
                        $"{(stranded == 1 ? "card sits" : "cards sit")} past that. Choose what " +
                        "happens to the placed cards.");
                }
            });
    }
}
