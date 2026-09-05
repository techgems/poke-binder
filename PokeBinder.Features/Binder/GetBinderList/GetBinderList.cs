using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;
using PokeBinder.Features.Binder.GetBinderList.Models;
using System.Linq.Expressions;

// "Binder" is a namespace here as well as an entity, so the entity is aliased rather than
// referred to by its full name at every use.
using BinderEntity = PokeBinder.Binders.DbContext.Entities.Binder;

namespace PokeBinder.Features.Binder.GetBinderList;

/// <summary>
/// The binders belonging to one user, as the list they navigate from. It answers "which binder do
/// I want?", not "what is in it": no cards and no tray entries are read, only counted. Opening one
/// is GetBinder's job.
/// </summary>
public static class GetBinderList
{
    public record Request();

    public record Response(IReadOnlyList<BinderListItem> Binders);

    /// <param name="userId">
    /// The signed-in user. A binder list is only ever the caller's own, so this comes from the
    /// principal rather than from the request.
    /// </param>
    public static async Task<Response> Handler(
        Request request,
        int userId,
        BinderDbContext context,
        CancellationToken ct = default)
    {
        var binders = await context.Binders
            .Where(binder => binder.UserId == userId)
            // Alphabetical, because this is a menu the user scans for a name they already have in
            // mind. NOCASE so that "ex binder" sorts next to "EX Binder" instead of after every
            // capitalised name, which is what SQLite's default binary collation would do. The id
            // breaks ties, so two binders sharing a name cannot swap places between reads.
            .OrderBy(binder => EF.Functions.Collate(binder.Name, "NOCASE"))
            .ThenBy(binder => binder.Id)
            .Select(MapListItem)
            .ToListAsync(ct);

        return new Response(binders);
    }

    /// <summary>
    /// The card counts are aggregates inside the projection, so SQLite counts the rows and returns
    /// two numbers per binder; nothing about the cards themselves crosses the wire or is
    /// materialised.
    /// </summary>
    private static readonly Expression<Func<BinderEntity, BinderListItem>> MapListItem =
        binder => new BinderListItem()
        {
            Id = binder.Id,
            Name = binder.Name,
            Description = binder.Description,
            CreatedAt = binder.CreatedAt,
            // The size is a required relationship, so the join it generates always matches.
            SizeName = binder.BinderSize!.Name,
            CardsPerPage = binder.BinderSize.X * binder.BinderSize.Y,
            Pages = binder.Pages,
            CardsAdded = binder.Cards.Count(),
            CardsMissing = binder.Cards.Count(card => card.IsMissing == true),
        };
}
