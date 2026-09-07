using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;
using PokeBinder.Binders.DbContext.Entities;
using PokeBinder.Features.Binder.GetNewBinderOptions.Models;
using System.Linq.Expressions;

namespace PokeBinder.Features.Binder.GetNewBinderOptions;

/// <summary>
/// Everything the create-a-binder form has to be handed before it can be drawn. Today that is only
/// the list of grids, which is why the response has one member -- but the slice is named for the
/// question the UI is asking ("what are my choices?") rather than for today's answer, so a form
/// that grows a second choice grows this response instead of gaining a second round trip.
///
/// None of it is scoped to a user: these are the same options for everyone.
/// </summary>
public static class GetNewBinderOptions
{
    public record Request();

    /// <summary>One member per choice the form offers. Add to it rather than adding a slice.</summary>
    public record Response(IReadOnlyList<BinderSizeOption> Sizes);

    public static async Task<Response> Handler(
        Request request,
        BinderDbContext context,
        CancellationToken ct = default)
    {
        var sizes = await context.BinderSizes
            // Smallest grid first: the list reads as a progression rather than as insertion order,
            // and the two 20-pocket grids end up side by side where they can be compared.
            .OrderBy(size => size.X * size.Y)
            .ThenBy(size => size.Name)
            .Select(MapOption)
            .ToListAsync(ct);

        return new Response(sizes);
    }

    private static readonly Expression<Func<BinderSize, BinderSizeOption>> MapOption =
        size => new BinderSizeOption()
        {
            Id = size.Id,
            Name = size.Name,
            Description = size.Description,
            // CardsPerPage is computed on the entity, which SQL cannot call, so the same
            // arithmetic is written out here to keep the projection translatable.
            CardsPerPage = size.X * size.Y,
            DefaultPages = size.DefaultPages,
        };
}
