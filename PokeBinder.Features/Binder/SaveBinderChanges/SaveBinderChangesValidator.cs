using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;
using PokeBinder.TcgCatalog.DbContext;

namespace PokeBinder.Features.Binder.SaveBinderChanges;

/// <summary>
/// Everything that can be wrong with one debounced edit before it is stored. It runs first and the
/// handler runs only if it passes, which is why the handler writes without checking anything.
///
/// <para>
/// It needs both databases and the binder's own shape: a page number is only meaningful against the
/// binder's page count, a pocket index only against its grid, and the cards being placed live in
/// the catalog, which is a separate SQLite file with no foreign key reaching into it.
/// </para>
///
/// <para>
/// The rule that makes this validator different from a whole-binder one is the scope check: a
/// card's pocket has to fall inside the pages the request claims. That is stricter than a bound
/// against the binder's capacity, and it is the point -- under a partial contract, a card outside
/// the claim is a client that has lost track of which spread it is saving, and writing it would put
/// a card on a page the request never said it was touching.
/// </para>
/// </summary>
/// <param name="binderContext">The user's binders, their cards and their trays.</param>
/// <param name="catalogContext">The card catalog, read to confirm the posted cards exist.</param>
/// <param name="userId">The signed-in user, from the principal rather than from the request.</param>
public class SaveBinderChangesValidator : AbstractValidator<SaveBinderChanges.Request>
{
    public SaveBinderChangesValidator(
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

        // Cascade.Stop on each list, because the controller binds the request straight off the body
        // and normalises nothing: a client that sent null where a list belongs arrives here as
        // null, and every rule after NotNull in these chains counts or projects the value. Without
        // the stop, NotNull records its failure and the next rule dereferences the null anyway --
        // a 500 in place of the 400 the failure had already established.
        RuleFor(request => request.Pages)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            // An empty claim is not "nothing changed on the pages", it is a request that has not
            // said what it covers -- and the one thing this contract cannot leave unsaid.
            .Must(pages => pages.Count > 0)
                .WithMessage("A save has to name the pages it covers.")
            .Must(pages => pages.Count <= SaveBinderChanges.MaxPages)
                .WithMessage($"A save covers at most {SaveBinderChanges.MaxPages} pages.")
            .Must(pages => pages.Distinct().Count() == pages.Count)
                .WithMessage("The same page is claimed twice.");

        RuleFor(request => request.Cards)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            // One card per pocket: the stored row is keyed by binder and index, so a repeated index
            // is a client that has lost track of its own page -- and the second row would collide
            // on the primary key anyway. There is no separate ceiling on the list because the scope
            // check below is a tighter one: distinct pockets, all inside at most two pages.
            .Must(cards => cards.Select(card => card.IndexInBinder).Distinct().Count() == cards.Count)
                .WithMessage("Two cards are in the same pocket.");

        // The binder's shape is a fact about this binder, so neither bound can be a constant, and
        // both come out of one read: the page numbers are checked against its page count and the
        // pockets against the pages those numbers cover.
        RuleFor(request => request)
            .CustomAsync(async (request, validation, ct) =>
            {
                var shape = await binderContext.Binders
                    .Where(binder => binder.Id == request.BinderId && binder.UserId == userId)
                    .Select(binder => new
                    {
                        binder.Pages,
                        // CardsPerPage is computed on the entity, which SQL cannot call, so the
                        // same arithmetic is written out here to keep the projection translatable.
                        CardsPerPage = binder.BinderSize!.X * binder.BinderSize.Y,
                    })
                    .FirstOrDefaultAsync(ct);

                // No binder, or one that somehow has no pockets: the rule above owns that message.
                // Null lists are the same story -- this rule runs whatever the chains above found,
                // so it says nothing about input their NotNull has already refused.
                if (shape is null
                    || shape.CardsPerPage <= 0
                    || request.Pages is null
                    || request.Cards is null)
                {
                    return;
                }

                var beyond = request.Pages
                    .Where(page => page < 1 || page > shape.Pages)
                    .ToList();

                if (beyond.Count > 0)
                {
                    validation.AddFailure(
                        nameof(SaveBinderChanges.Request.Pages),
                        $"This binder has {shape.Pages} " +
                        $"{(shape.Pages == 1 ? "page" : "pages")}, and this save claims " +
                        $"{(beyond.Count == 1 ? "page" : "pages")} {string.Join(", ", beyond)}.");

                    // The pocket check below is arithmetic over these page numbers, so it would
                    // only report a second failure about a range that was never real.
                    return;
                }

                var claimed = SaveBinderChanges.PocketsOnPages(request.Pages, shape.CardsPerPage);

                var strays = request.Cards
                    .Where(card => !claimed.Contains(card.IndexInBinder))
                    .ToList();

                if (strays.Count == 0)
                {
                    return;
                }

                validation.AddFailure(
                    nameof(SaveBinderChanges.Request.Cards),
                    $"{(strays.Count == 1 ? "One card sits" : $"{strays.Count} cards sit")} " +
                    "outside the pages this save claims.");
            });

        // No ceiling on how many cards a tray may hold or how many copies of one: how big a tray is
        // sensible is the workspace's business, and tray.svelte.ts is where that is decided. What
        // is left here is the two things the table itself cannot survive being wrong about.
        RuleFor(request => request.Tray)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            // The tray is keyed by card, so the same card twice is a client that has lost track of
            // its own state -- and the second row would collide on the primary key anyway.
            .Must(tray => tray.Select(card => card.CardId).Distinct().Count() == tray.Count)
                .WithMessage("The same card appears twice in the tray.");

        RuleForEach(request => request.Tray).ChildRules(card =>
        {
            // Not a limit but the meaning of the column: zero does not mean "remove me", because a
            // removed card is left out of the snapshot entirely. binderTray carries a check
            // constraint of quantity > 0, so a row at zero or below is refused by the database --
            // and refused as an exception, which is a 500 where this is a 400 that says what to fix.
            card.RuleFor(trayCard => trayCard.Quantity)
                .GreaterThan(0)
                .WithMessage("A card in the tray has at least one copy; leave a card out of the " +
                             "tray to remove it.");
        });

        // One query for both halves rather than one per card: the tray alone can arrive with a
        // hundred of them, and the cards on a spread of a large grid with hundreds more.
        RuleFor(request => request)
            .CustomAsync(async (request, validation, ct) =>
            {
                // As above: a null list is the NotNull rule's to report, not this one's.
                if (request.Cards is null || request.Tray is null)
                {
                    return;
                }

                var cardIds = request.Cards
                    .Select(card => card.CardId)
                    .Concat(request.Tray.Select(card => card.CardId))
                    .Distinct()
                    .ToList();

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

                // Reported against the request rather than one of the two lists: the id may have
                // come from either, and saying which would mean splitting the query that made
                // asking once worthwhile.
                validation.AddFailure(
                    nameof(SaveBinderChanges.Request.Cards),
                    $"{(unknown.Count == 1 ? "Card" : "Cards")} {string.Join(", ", unknown)} " +
                    $"{(unknown.Count == 1 ? "is" : "are")} not in the catalog.");
            });
    }
}
