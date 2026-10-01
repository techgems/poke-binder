using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;
using PokeBinder.Features.Binder.ReorderBinder.Models;

namespace PokeBinder.Features.Binder.ReorderBinder;

/// <summary>
/// Everything that can be wrong with a reorder before it rewrites the binder. It runs first and the
/// handler runs only if it passes.
///
/// <para>
/// Only the binder database: the request carries no card ids, so there is nothing to check against
/// the catalog. What the catalog does not know about a placed card sorts last rather than being an
/// error, because the cards are already in the binder and refusing to sort them would not remove
/// them.
/// </para>
/// </summary>
/// <param name="binderContext">The user's binders.</param>
/// <param name="userId">The signed-in user, from the principal rather than from the request.</param>
public class ReorderBinderValidator : AbstractValidator<ReorderBinder.Request>
{
    public ReorderBinderValidator(BinderDbContext binderContext, int userId)
    {
        RuleFor(request => request.BinderId)
            // Someone else's binder answers the same way a missing one does, so a request cannot
            // be used to find out which binder ids exist.
            .MustAsync((binderId, ct) => binderContext.Binders
                .AnyAsync(binder => binder.Id == binderId && binder.UserId == userId, ct))
            .WithMessage("That binder no longer exists.");

        // Cascade.Stop because the controller binds the request straight off the body: a client
        // that sent null for the list arrives here as null, and the rules after NotNull count it.
        RuleFor(request => request.Criteria)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(criteria => criteria.Count is >= 1 and <= ReorderBinder.MaxCriteria)
                .WithMessage($"A reorder sorts by at least one criterion and at most {ReorderBinder.MaxCriteria}.")
            .Must(criteria => criteria.All(criterion => criterion is not null))
                .WithMessage("A criterion is empty.")
            // A criterion a second time could only break ties it has already broken.
            .Must(criteria => criteria.Select(criterion => criterion.Key).Distinct().Count() == criteria.Count)
                .WithMessage("The same criterion is used twice.");

        RuleForEach(request => request.Criteria).ChildRules(criterion =>
        {
            criterion.RuleFor(c => c.Key).IsInEnum();
            criterion.RuleFor(c => c.Direction).IsInEnum();

            // One criterion with a choice of key: Rarity has to say which ordering it reads, and a
            // key on anything else is a client that has confused its criteria.
            criterion.RuleFor(c => c.RarityKey)
                .NotNull()
                    .WithMessage("Rarity has to say which ordering it sorts by: pull rate or name order.")
                .IsInEnum()
                .When(c => c.Key == CriterionKey.Rarity);

            criterion.RuleFor(c => c.RarityKey)
                .Null()
                .When(c => c.Key != CriterionKey.Rarity)
                .WithMessage("Only Rarity sorts by a rarity ordering.");
        });
    }
}
