using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;
using PokeBinder.TcgCatalog.DbContext;

namespace PokeBinder.Features.Binder.SaveBinderCards;

/// <summary>
/// Everything that can be wrong with a page of placements before it is stored. It runs first and
/// the handler runs only if it passes, which is why the handler writes without checking anything.
///
/// It needs both databases and the binder's own grid: a pocket index is only meaningful against the
/// binder's size and page count, and the cards being placed live in the catalog, which is a
/// separate SQLite file with no foreign key reaching into it.
/// </summary>
/// <param name="binderContext">The user's binders and their cards.</param>
/// <param name="catalogContext">The card catalog, read to confirm the posted cards exist.</param>
/// <param name="userId">The signed-in user, from the principal rather than from the request.</param>
public class SaveBinderCardsValidator : AbstractValidator<SaveBinderCards.Request>
{
    public SaveBinderCardsValidator(
        BinderDbContext binderContext,
        TcgCatalogDbContext catalogContext,
        int userId)
    {
        RuleFor(request => request.BinderId)
            // Someone else's binder answers the same way a missing one does, so a request cannot
            // be used to find out which binder ids exist.
            .MustAsync((binderId, ct) => binderContext.Binders
                .AnyAsync(binder => binder.Id == binderId && binder.UserId == userId, ct))
            .WithMessage("That binder no longer exists.");

        RuleFor(request => request.Cards)
            .NotNull()
            .Must(cards => cards.Count <= SaveBinderCards.MaxCards)
                .WithMessage($"A binder holds at most {SaveBinderCards.MaxCards} cards.")
            // One card per pocket: the stored row is keyed by binder and index, so a repeated index
            // is a client that has lost track of its own page -- and the second row would collide
            // on the primary key anyway.
            .Must(cards => cards.Select(card => card.IndexInBinder).Distinct().Count() == cards.Count)
                .WithMessage("Two cards are in the same pocket.");

        // How many pockets exist is a fact about this binder, so the bound cannot be a constant.
        RuleFor(request => request)
            .CustomAsync(async (request, validation, ct) =>
            {
                var capacity = await binderContext.Binders
                    .Where(binder => binder.Id == request.BinderId && binder.UserId == userId)
                    .Select(binder => binder.BinderSize!.X * binder.BinderSize.Y * binder.Pages)
                    .FirstOrDefaultAsync(ct);

                // No binder, or one that somehow has no pockets: the rule above owns that message.
                if (capacity <= 0)
                {
                    return;
                }

                // Pockets are numbered from zero, so the pocket count is also the first index
                // outside the binder.
                var outside = request.Cards
                    .Where(card => card.IndexInBinder < 0 || card.IndexInBinder >= capacity)
                    .ToList();

                if (outside.Count == 0)
                {
                    return;
                }

                validation.AddFailure(
                    nameof(SaveBinderCards.Request.Cards),
                    $"This binder has {capacity} pockets, and {outside.Count} " +
                    $"{(outside.Count == 1 ? "card is" : "cards are")} outside it.");
            });

        // One query for the whole batch rather than one per card: a full binder arrives with
        // hundreds of them.
        RuleFor(request => request.Cards)
            .CustomAsync(async (cards, validation, ct) =>
            {
                var cardIds = cards.Select(card => card.CardId).Distinct().ToList();

                if (cardIds.Count == 0)
                {
                    return;
                }

                var known = await catalogContext.Cards
                    .Where(card => cardIds.Contains(card.Id))
                    .Select(card => card.Id)
                    .ToListAsync(ct);

                var unknown = cardIds.Except(known).ToList();

                if (unknown.Count == 0)
                {
                    return;
                }

                validation.AddFailure(
                    nameof(SaveBinderCards.Request.Cards),
                    $"{(unknown.Count == 1 ? "Card" : "Cards")} {string.Join(", ", unknown)} " +
                    $"{(unknown.Count == 1 ? "is" : "are")} not in the catalog.");
            });
    }
}
