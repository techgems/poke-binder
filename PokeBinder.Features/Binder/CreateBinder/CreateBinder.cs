using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;

// "Binder" is a namespace here as well as an entity, so the entity is aliased rather than
// referred to by its full name at every use.
using BinderEntity = PokeBinder.Binders.DbContext.Entities.Binder;

namespace PokeBinder.Features.Binder.CreateBinder;

/// <summary>
/// Creates a binder: its name, its grid and how many pages it has. Editing one is UpdateBinder's,
/// since an edit can move the cards already placed and a new binder has none. What goes in its
/// pockets and what is staged for it are both SaveBinderChanges: the tray is not separable from the
/// pages, because placing a card spends a copy out of it.
///
/// A new binder is empty in both senses: no cards on its pages, and no tray entries. Neither needs
/// a row written here. The tray is scoped to the binder by id rather than being a record of its
/// own, so a binder gets its own tray -- and with it the user's context for managing that one
/// binder -- simply by existing.
///
/// The handler is the happy path only. CreateBinderValidator runs first and rejects everything that
/// could fail here, so this code never sees a nameless binder or an unknown size; the read below
/// uses First rather than FirstOrDefault to say so, and would throw rather than write something
/// wrong if a caller skipped validation.
///
/// The limits below are what a binder can be, so UpdateBinderValidator holds an edit to them too.
/// </summary>
public static class CreateBinder
{
    public const int MaxNameLength = 100;

    public const int MaxDescriptionLength = 500;

    /// <summary>
    /// Ceiling on pages. No binder on a shelf is this long; the point is that a typo in a page
    /// count cannot ask the binder view to lay out a million empty pockets.
    /// </summary>
    public const int MaxPages = 200;

    /// <summary>
    /// The binder as the user filled it in. The owner is deliberately not part of this: it comes
    /// from the signed-in principal, so a request cannot name someone else's user id.
    /// </summary>
    public record Request
    {
        /// <summary>Required. Trimmed by the handler; CreateBinderValidator rejects a blank one.</summary>
        public string? Name { get; init; }

        public string? Description { get; init; }

        /// <summary>The grid, from the seeded binderSizes list.</summary>
        public int BinderSizeId { get; init; }

        /// <summary>
        /// How many pages the binder has. Always carries a number: the create form prefills it
        /// from the chosen size's BinderSize.DefaultPages, so by the time it arrives here the user
        /// has either kept that recommendation or typed over it. A field left out of the payload
        /// binds as zero, which the validator rejects rather than quietly treating as the default
        /// again -- a page count that was never asked for is a bug in the form, not a value to
        /// guess at.
        /// </summary>
        public int Pages { get; init; }
    }

    /// <summary>
    /// The saved binder as it now stands. There is no failure to report: anything the user could
    /// get wrong was answered by CreateBinderValidator before this ran.
    /// </summary>
    public record Response(int BinderId, int Pages, int CardCount)
    {
        internal static Response From(BinderEntity binder) =>
            new(binder.Id, binder.Pages, binder.CardCount);
    }

    /// <param name="userId">The signed-in user, who owns the binder created.</param>
    public static async Task<Response> Handler(
        Request request,
        int userId,
        BinderDbContext context,
        CancellationToken ct = default)
    {
        // Trimming is tidying, not validating: the validator has already refused a name that is
        // nothing but spaces, and an untouched description box arrives as "" or whitespace, which
        // the nullable column should hold as null rather than as an empty string.
        var name = request.Name!.Trim();

        var description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();

        // Tracked, not AsNoTracking: it is attached to the binder below, and an untracked instance
        // would be taken for a new size row and inserted alongside the binder.
        var size = await context.BinderSizes.FirstAsync(s => s.Id == request.BinderSizeId, ct);

        var binder = new BinderEntity
        {
            Name = name,
            Description = description,
            UserId = userId,
            // Assigning the navigation rather than the id so the binder can report its CardCount
            // in the response without a second read.
            BinderSize = size,
            Pages = request.Pages,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };

        context.Binders.Add(binder);

        await context.SaveChangesAsync(ct);

        return Response.From(binder);
    }
}
