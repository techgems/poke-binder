using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;
using PokeBinder.TcgCatalog.DbContext;

namespace PokeBinder.Features.Binder.SaveBinderTray;

/// <summary>
/// Everything that can be wrong with a tray snapshot before it is stored. It runs first and the
/// handler runs only if it passes, which is why the handler writes without checking anything.
///
/// It needs both databases: the binder and its tray are in one, the cards the tray points at are
/// in the catalog. They are separate SQLite files with no foreign key between them, which is
/// exactly why the card ids have to be checked here -- nothing else will.
/// </summary>
/// <param name="binderContext">The user's binders and their trays.</param>
/// <param name="catalogContext">The card catalog, read to confirm the posted cards exist.</param>
/// <param name="userId">The signed-in user, from the principal rather than from the request.</param>
public class SaveBinderTrayValidator : AbstractValidator<SaveBinderTray.Request>
{
    public SaveBinderTrayValidator(
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
            .Must(cards => cards.Count <= SaveBinderTray.MaxCards)
                .WithMessage($"A tray holds at most {SaveBinderTray.MaxCards} cards.")
            // The tray is keyed by card, so the same card twice is a client that has lost track of
            // its own state -- and the second row would collide on the primary key anyway.
            .Must(cards => cards.Select(card => card.CardId).Distinct().Count() == cards.Count)
                .WithMessage("The same card appears twice in the tray.");

        RuleForEach(request => request.Cards).ChildRules(card =>
        {
            // Zero does not mean "remove me": a removed card is left out of the snapshot entirely.
            // Saying so keeps a client from inventing a second way to delete.
            card.RuleFor(trayCard => trayCard.Quantity)
                .InclusiveBetween(1, SaveBinderTray.MaxQuantity)
                .WithMessage($"A card's quantity is between 1 and {SaveBinderTray.MaxQuantity}; " +
                             "leave a card out of the tray to remove it.");
        });

        // One query for the whole batch rather than one per card: a debounced tray can arrive with
        // a hundred of them.
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
                    nameof(SaveBinderTray.Request.Cards),
                    $"{(unknown.Count == 1 ? "Card" : "Cards")} {string.Join(", ", unknown)} " +
                    $"{(unknown.Count == 1 ? "is" : "are")} not in the catalog.");
            });
    }
}
