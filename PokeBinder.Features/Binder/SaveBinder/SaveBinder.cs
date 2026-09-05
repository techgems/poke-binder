using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;
using PokeBinder.Binders.DbContext.Entities;

// "Binder" is a namespace here as well as an entity, so the entity is aliased rather than
// referred to by its full name at every use.
using BinderEntity = PokeBinder.Binders.DbContext.Entities.Binder;

namespace PokeBinder.Features.Binder.SaveBinder;

/// <summary>
/// Creates a binder, or saves an edit to one the user already has. This slice owns the binder
/// itself -- its name, its grid and how many pages it has. What goes in its pockets is
/// SaveBinderChanges, and what is staged for it is SaveBinderTray.
///
/// A new binder is empty in both senses: no cards on its pages, and no tray entries. Neither needs
/// a row written here. The tray is scoped to the binder by id rather than being a record of its
/// own, so a binder gets its own tray -- and with it the user's context for managing that one
/// binder -- simply by existing.
///
/// The handler is the happy path only. SaveBinderValidator runs first and rejects everything that
/// could fail here, so this code never sees a nameless binder, an unknown size or someone else's
/// binder; the reads below use First rather than FirstOrDefault to say so, and would throw rather
/// than write something wrong if a caller skipped validation.
/// </summary>
public static class SaveBinder
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
        /// <summary>
        /// The binder being edited, or null to create one. An id that is not the caller's own
        /// binder is refused rather than created under a new id.
        /// </summary>
        public int? Id { get; init; }

        /// <summary>Required. Trimmed by the handler; SaveBinderValidator rejects a blank one.</summary>
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
    /// get wrong was answered by SaveBinderValidator before this ran.
    /// </summary>
    /// <param name="Created">True if this call created the binder, false if it edited one.</param>
    public record Response(int BinderId, int Pages, int CardCount, bool Created)
    {
        internal static Response From(BinderEntity binder, bool created) =>
            new(binder.Id, binder.Pages, binder.CardCount, created);
    }

    /// <param name="userId">
    /// The signed-in user. Owns a created binder, and is the only one who can edit one.
    /// </param>
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

        return request.Id is null
            ? await Create(name, description, size, request.Pages, userId, context, ct)
            : await Update(request.Id.Value, name, description, size, request.Pages, userId, context, ct);
    }

    private static async Task<Response> Create(
        string name,
        string? description,
        BinderSize size,
        int pages,
        int userId,
        BinderDbContext context,
        CancellationToken ct)
    {
        var binder = new BinderEntity
        {
            Name = name,
            Description = description,
            UserId = userId,
            // Assigning the navigation rather than the id so the binder can report its CardCount
            // in the response without a second read.
            BinderSize = size,
            Pages = pages,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };

        context.Binders.Add(binder);

        await context.SaveChangesAsync(ct);

        return Response.From(binder, created: true);
    }

    private static async Task<Response> Update(
        int binderId,
        string name,
        string? description,
        BinderSize size,
        int pages,
        int userId,
        BinderDbContext context,
        CancellationToken ct)
    {
        var binder = await context.Binders
            .FirstAsync(b => b.Id == binderId && b.UserId == userId, ct);

        binder.Name = name;
        binder.Description = description;
        binder.BinderSize = size;
        binder.Pages = pages;

        await context.SaveChangesAsync(ct);

        return Response.From(binder, created: false);
    }
}
