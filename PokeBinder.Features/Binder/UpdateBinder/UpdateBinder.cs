using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;
using PokeBinder.Binders.DbContext.Entities;
using PokeBinder.Features.Binder.Shared.Arrangement;
using PokeBinder.Features.Binder.ReorderBinder.Models;
using PokeBinder.Features.Binder.UpdateBinder.Models;
using PokeBinder.TcgCatalog.DbContext;
using PlacedCardEntity = PokeBinder.Binders.DbContext.Entities.BinderCard;

namespace PokeBinder.Features.Binder.UpdateBinder;

/// <summary>
/// Saves an edit to one of the user's binders: its name and description, its grid and its page
/// count -- and, when the new size moves the cards already placed, what happens to them. Creating a
/// binder is CreateBinder's; this is the settings tab's.
///
/// <para>
/// <b>A resize and what it does to the cards are one request and one transaction.</b> Pockets are
/// one absolute index across the binder, so a new grid or page count changes which page and which
/// place every card is on. Split in two, a crash between the resize and the rearrangement leaves a
/// binder of the new size with its cards still laid out for the old one -- or, emptying into the
/// tray, a card both placed and waiting.
/// </para>
///
/// <para>
/// The handler is the happy path only. UpdateBinderValidator runs first and refuses everything that
/// could fail here: a nameless binder, an unknown size, someone else's binder, a sort with no
/// criteria, and a size the cards do not fit in.
/// </para>
/// </summary>
public static class UpdateBinder
{
    public record Request
    {
        /// <summary>The binder being edited. Must be one of the caller's own.</summary>
        public int BinderId { get; init; }

        /// <summary>Required. Trimmed by the handler; the validator refuses a blank one.</summary>
        public string? Name { get; init; }

        /// <summary>Blank is stored as no description at all.</summary>
        public string? Description { get; init; }

        /// <summary>The grid, from the seeded binderSizes list.</summary>
        public int BinderSizeId { get; init; }

        public int Pages { get; init; }

        /// <summary>
        /// What happens to the placed cards. Null leaves every card in the pocket it is in, which
        /// is only allowed while every one of those pockets still exists at the new size.
        /// </summary>
        public Rearrangement? Rearrange { get; init; }

        /// <summary>
        /// What a <see cref="Rearrangement.Sort"/> sorts by, most significant first. Required for a
        /// sort and refused with anything else.
        /// </summary>
        public IReadOnlyList<SortCriterion>? Criteria { get; init; }
    }

    public record Response(int BinderId);

    /// <param name="userId">
    /// The signed-in user. The validator has already established that the binder is theirs; the
    /// read below is scoped by it anyway, so a handler called without its validator cannot edit
    /// another user's binder.
    /// </param>
    public static async Task<Response> Handler(
        Request request,
        int userId,
        BinderDbContext binderContext,
        TcgCatalogDbContext catalogContext,
        CancellationToken ct = default)
    {
        var binder = await binderContext.Binders
            .FirstAsync(b => b.Id == request.BinderId && b.UserId == userId, ct);

        // Trimming is tidying, not validating: the validator has already refused a name that is
        // nothing but spaces, and an untouched description box arrives as "" or whitespace, which
        // the nullable column should hold as null rather than as an empty string.
        binder.Name = request.Name!.Trim();
        binder.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        binder.BinderSizeId = request.BinderSizeId;
        binder.Pages = request.Pages;

        if (request.Rearrange is { } rearrange)
        {
            var stored = await binderContext.BinderCards
                .Where(card => card.BinderId == binder.Id)
                .OrderBy(card => card.IndexInBinder)
                .ToListAsync(ct);

            var ordered = rearrange switch
            {
                Rearrangement.Keep => BinderArrangement.InPocketOrder(stored),
                Rearrangement.Sort => await BinderArrangement.SortAsync(stored, request.Criteria!, catalogContext, ct),
                // Tray and Drop both leave the binder empty; Tray first puts every card back.
                _ => [],
            };

            if (rearrange == Rearrangement.Tray)
            {
                await ReturnToTrayAsync(stored, binder.Id, binderContext, ct);
            }

            BinderArrangement.LayOutFromFirstPocket(stored, ordered, binder.Id, binderContext);
        }

        await binderContext.SaveChangesAsync(ct);

        return new Response(binder.Id);
    }

    /// <summary>
    /// Adds a copy to the tray for every placed card, beside whatever the tray already holds. A
    /// card the binder held twice comes back as two copies of one tray row, the shape the tray keeps
    /// everywhere else.
    /// </summary>
    private static async Task ReturnToTrayAsync(
        List<PlacedCardEntity> stored,
        int binderId,
        BinderDbContext context,
        CancellationToken ct)
    {
        var tray = await context.BinderTray
            .Where(entry => entry.BinderId == binderId)
            .ToDictionaryAsync(entry => entry.CardId, ct);

        foreach (var copies in stored.GroupBy(card => card.CardId))
        {
            if (tray.TryGetValue(copies.Key, out var entry))
            {
                entry.Quantity += copies.Count();

                continue;
            }

            context.BinderTray.Add(new BinderTray
            {
                BinderId = binderId,
                CardId = copies.Key,
                Quantity = copies.Count(),
            });
        }
    }
}
