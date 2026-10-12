using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;
using PokeBinder.Features.Binder.Shared.Arrangement;
using PokeBinder.TcgCatalog.DbContext;

namespace PokeBinder.Features.Binder.AddSetToBinder;

/// <summary>
/// Everything that can be wrong with adding a set before it rewrites the binder. It runs first and
/// the handler runs only if it passes.
///
/// <para>
/// Both databases: the binder is the user's, and the set and its cards are the catalog's. What
/// matters most is the fit -- the set has to go in as the binder stands, or the request has to say
/// pages may be added, and even then a binder has a page limit.
/// </para>
/// </summary>
/// <param name="binderContext">The user's binders and the cards placed in them.</param>
/// <param name="catalogContext">The set and its cards.</param>
/// <param name="userId">The signed-in user, from the principal rather than from the request.</param>
public class AddSetToBinderValidator : AbstractValidator<AddSetToBinder.Request>
{
    public AddSetToBinderValidator(BinderDbContext binderContext, TcgCatalogDbContext catalogContext, int userId)
    {
        RuleFor(request => request.BinderId)
            // Someone else's binder answers the same way a missing one does, so a request cannot
            // be used to find out which binder ids exist.
            .MustAsync((binderId, ct) => binderContext.Binders
                .AnyAsync(binder => binder.Id == binderId && binder.UserId == userId, ct))
            .WithMessage("That binder no longer exists.");

        RuleFor(request => request.SetId)
            .MustAsync((setId, ct) => catalogContext.Sets.AnyAsync(set => set.Id == setId, ct))
            .WithMessage("That set is not in the catalog.");

        // Cascade.Stop because the controller binds the request straight off the body: a client
        // that sent null for a list arrives here as null, and the rules after NotNull count it.
        RuleFor(request => request.LeaveOut)
            .Cascade(CascadeMode.Stop)
            .NotNull()
                .WithMessage("Say which rarities to leave out, or none.");

        RuleFor(request => request.Criteria)
            .Cascade(CascadeMode.Stop)
            .NotNull()
                .WithMessage("Say what order the set goes in.")
            .MakeASort(SortCriteriaRules.SetKeys);

        RuleForEach(request => request.Criteria).ChildRules(SortCriteriaRules.Criterion);

        RuleFor(request => request.Existing).IsInEnum();

        // Whether the set fits. A rule rather than a Must so the message can carry the counts, which
        // are only known once the queries have run. Scoped to the owner: without that, a request
        // naming someone else's binder would be told how many pages it has.
        RuleFor(request => request)
            .CustomAsync(async (request, validation, ct) =>
            {
                // The rules that own a null list report it; this would only dereference it.
                if (request.LeaveOut is null)
                {
                    return;
                }

                var binder = await binderContext.Binders
                    .Where(b => b.Id == request.BinderId && b.UserId == userId)
                    .Select(b => new { b.Pages, CardsPerPage = b.BinderSize!.X * b.BinderSize.Y })
                    .FirstOrDefaultAsync(ct);

                // No binder: the BinderId rule owns the message.
                if (binder is null || binder.CardsPerPage <= 0)
                {
                    return;
                }

                var setCards = await AddSetToBinder.SetCards(catalogContext, request.SetId, request.LeaveOut)
                    .CountAsync(ct);

                if (setCards == 0)
                {
                    // An unknown set has no cards either, and the SetId rule owns that message.
                    if (request.LeaveOut.Count > 0)
                    {
                        validation.AddFailure(
                            nameof(AddSetToBinder.Request.LeaveOut),
                            "Every card in the set is left out.");
                    }

                    return;
                }

                var lastPlacedIndex = await binderContext.BinderCards
                    .Where(card => card.BinderId == request.BinderId)
                    .MaxAsync(card => (int?)card.IndexInBinder, ct);

                var plan = AddSetToBinder.PlanPlacement(
                    lastPlacedIndex, binder.CardsPerPage, setCards, request.EmptyFirst, request.Existing);

                if (plan.PagesNeeded > CreateBinder.CreateBinder.MaxPages)
                {
                    validation.AddFailure(
                        nameof(AddSetToBinder.Request.AddPages),
                        $"This would take {plan.PagesNeeded} pages, and a binder has at most " +
                        $"{CreateBinder.CreateBinder.MaxPages}. Leave out some rarities, or empty " +
                        "the binder first.");

                    return;
                }

                if (plan.PagesNeeded > binder.Pages && !request.AddPages)
                {
                    validation.AddFailure(
                        nameof(AddSetToBinder.Request.AddPages),
                        $"The set needs {plan.PagesNeeded} pages and the binder has {binder.Pages}. " +
                        "Add pages to fit it.");
                }
            });
    }
}
