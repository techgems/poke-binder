using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;
using PokeBinder.Binders.DbContext.Entities;
using PokeBinder.Features.Binder.CreateBinderCardLabel;
using PokeBinder.Features.Binder.Shared.Arrangement;
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

        /// <summary>
        /// The binder's labels as they should stand after this save. A sync, not a CRUD:
        /// <list type="bullet">
        ///   <item>A non-null id already on the binder is an update: a different name or colour
        ///   lands as a rename or recolor.</item>
        ///   <item>A null id is an insert -- the Settings tab stages new labels locally until
        ///   Save, so a row the panel added arrives here with its id unset and the handler
        ///   creates it with a real one.</item>
        ///   <item>An id in the DB but absent from the payload is a delete-by-omission, which
        ///   cascades through the join to remove every chip of it.</item>
        /// </list>
        /// A null outer list leaves the labels alone, which is what a client that opened Settings
        /// without touching the Labels panel sends. The dedicated <c>CreateBinderCardLabel</c>
        /// endpoint is still the modal's path -- that one commits a chip-applied label on close,
        /// which happens before any Settings Save would.
        /// </summary>
        public IReadOnlyList<SavedLabel>? Labels { get; init; }
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
            // Include the label assignments so the rearrangement helpers carry each card's chips
            // along with it. Tray and Drop empty the pockets; the cascade on the join strips the
            // assignments when the binderCards rows go.
            var stored = await binderContext.BinderCards
                .Where(card => card.BinderId == binder.Id)
                .Include(card => card.LabelAssignments)
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

        if (request.Labels is not null)
        {
            await SyncLabelsAsync(request.Labels, binder.Id, binderContext, ct);
        }

        await binderContext.SaveChangesAsync(ct);

        return new Response(binder.Id);
    }

    /// <summary>
    /// Reconciles the binder's labels against the Settings-tab payload.
    /// <list type="bullet">
    ///   <item>Non-null id in DB and in payload → update name and colour.</item>
    ///   <item>Non-null id in DB but not in payload → delete; the FK's ON DELETE CASCADE strips
    ///   the join rows and takes every chip of it with the row.</item>
    ///   <item>Non-null id in payload but not in DB → the validator has already refused this;
    ///   the lookup below falls through and does nothing.</item>
    ///   <item>Null id in payload → insert. New labels in the Settings panel stage locally until
    ///   Save, so they arrive here with their id unset.</item>
    /// </list>
    /// Deletes are applied before inserts so a payload that renames one label and gives another
    /// its old name does not trip the <c>(binderId, name)</c> unique index on the way through.
    /// Nothing is saved here: the caller commits with whatever else it is writing.
    /// </summary>
    private static async Task SyncLabelsAsync(
        IReadOnlyList<SavedLabel> payload,
        int binderId,
        BinderDbContext context,
        CancellationToken ct)
    {
        var stored = await context.BinderCardLabels
            .Where(label => label.BinderId == binderId)
            .ToListAsync(ct);

        var byId = stored.ToDictionary(label => label.Id);

        foreach (var entry in payload)
        {
            if (entry.Id is null)
            {
                // Insert carried below, after the deletes have cleared any name clashes.
                continue;
            }

            if (!byId.Remove(entry.Id.Value, out var existing))
            {
                // Validator-refused case; nothing to do here.
                continue;
            }

            var name = (entry.Name ?? string.Empty).Trim();

            if (!string.Equals(existing.Name, name, StringComparison.Ordinal))
            {
                existing.Name = name;
            }

            if (!string.Equals(existing.Color, entry.Color, StringComparison.Ordinal))
            {
                existing.Color = entry.Color!;
            }
        }

        // Whatever is left is an id the payload left out. Delete cascades to the join rows.
        context.BinderCardLabels.RemoveRange(byId.Values);

        foreach (var entry in payload)
        {
            if (entry.Id is not null) continue;

            context.BinderCardLabels.Add(new BinderCardLabel
            {
                BinderId = binderId,
                Name = (entry.Name ?? string.Empty).Trim(),
                Color = entry.Color!,
            });
        }
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
