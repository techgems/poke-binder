using Microsoft.EntityFrameworkCore;
using PokeBinder.Features.CardSearch.SearchCardsByFilter.Models;
using PokeBinder.TcgCatalog.DbContext;
using PokeBinder.TcgCatalog.DbContext.Entities;
using System.Linq.Expressions;

namespace PokeBinder.Features.CardSearch.SearchCardsByFilter;

public static class SearchCardsByFilter
{
    /// <summary>Page size used when the caller does not ask for one.</summary>
    public const int DefaultPageSize = 50;

    /// <summary>
    /// Ceiling on page size. The first search a user sees has no filters at all, which matches
    /// every card in the catalog, so the page size is capped rather than trusted.
    /// </summary>
    public const int MaxPageSize = 200;

    /// <summary>
    /// Every filter field mirrors one multi-select filter field in the UI. An empty list means the
    /// user narrowed nothing by that field, so it is left out of the query entirely.
    /// </summary>
    public record Request
    {
        /// <summary>Super type names, as they are stored on the card.</summary>
        public IReadOnlyList<string> SuperTypes { get; init; } = [];

        public IReadOnlyList<int> Generations { get; init; } = [];

        public IReadOnlyList<int> Series { get; init; } = [];

        public IReadOnlyList<int> Sets { get; init; } = [];

        /// <summary>Pokemon filter option ids, not pokedex numbers.</summary>
        public IReadOnlyList<int> Pokemon { get; init; } = [];

        /// <summary>Rarity-by-set row ids, each standing for one (set, rarity) pair.</summary>
        public IReadOnlyList<int> Rarities { get; init; } = [];

        public IReadOnlyList<int> CardTypes { get; init; } = [];

        /// <summary>1-based page to return. Anything below 1 is treated as the first page.</summary>
        public int PageNumber { get; init; } = 1;

        /// <summary>Rows per page, clamped to <see cref="MaxPageSize"/>.</summary>
        public int PageSize { get; init; } = DefaultPageSize;
    }

    public record Response(
        IReadOnlyList<CardSearchResult> Results,
        int PageNumber,
        int PageSize,
        bool HasMore);

    public static async Task<Response> Handler(
        Request request,
        TcgCatalogDbContext context,
        CancellationToken ct = default)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var pageNumber = Math.Max(request.PageNumber, 1);

        // Guard the offset arithmetic: an absurd page number would otherwise overflow into a
        // negative Skip rather than an empty page.
        var skip = (int)Math.Min((long)(pageNumber - 1) * pageSize, int.MaxValue);

        var rows = await ApplyFilters(context.Cards, request, context)
            // Paging needs a total order, otherwise SQLite is free to return a row on two
            // different pages. Name alone repeats heavily, so the unique id breaks the ties.
            .OrderBy(card => card.Name)
            .ThenBy(card => card.Id)
            .Select(MapResult)
            .Skip(skip)
            // Reading one row past the page is what tells us a next page exists. A total count
            // would mean a second query, and it would have to repeat the name scan the pokemon
            // and generation filters rely on.
            .Take(pageSize + 1)
            .ToListAsync(ct);

        var hasMore = rows.Count > pageSize;

        if (hasMore)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        return new Response(rows, pageNumber, pageSize, hasMore);
    }

    /// <summary>
    /// Composes every requested filter onto one queryable. The filter tables are reached through
    /// correlated subqueries rather than being read first, so the search stays a single round trip
    /// however many filter lists arrive.
    /// </summary>
    private static IQueryable<Card> ApplyFilters(
        IQueryable<Card> cards,
        Request request,
        TcgCatalogDbContext context)
    {
        if (request.SuperTypes.Count > 0)
        {
            cards = cards.Where(card => request.SuperTypes.Contains(card.CardType));
        }

        if (request.Series.Count > 0)
        {
            cards = cards.Where(card => card.Set != null && request.Series.Contains(card.Set.SeriesId));
        }

        if (request.Sets.Count > 0)
        {
            cards = cards.Where(card => card.SetId != null && request.Sets.Contains(card.SetId.Value));
        }

        // The card carries its card type as a subtype name, so the chosen ids are turned into names
        // inside the query instead of in a separate lookup.
        if (request.CardTypes.Count > 0)
        {
            cards = cards.Where(card => context.CardTypeFilterOptions
                .Where(option => request.CardTypes.Contains(option.Id))
                .Select(option => option.Name)
                .Contains(card.CardSubtype));
        }

        // A rarity option is a (set, rarity) pair, so both halves have to match: the same rarity
        // name means a different thing from one set to the next.
        if (request.Rarities.Count > 0)
        {
            cards = cards.Where(card => context.RarityBySetFilterOptions.Any(option =>
                request.Rarities.Contains(option.Id)
                && option.SetId == card.SetId
                && option.Rarity == card.Rarity));
        }

        // Nothing links a card to a pokemon by id — pkmnCardText.dexNumber is unpopulated — so the
        // only available join is the card's name. Both sides are padded with spaces so the name has
        // to appear as a whole word: plain containment would let Mew match every Mewtwo card. The
        // alternate name is the form that actually appears on cards (Charizard VMAX), so it counts
        // too. Tag-team cards naming several pokemon match each of them, which is correct.
        if (request.Pokemon.Count > 0)
        {
            cards = cards.Where(card => card.Name != null && context.PokemonFilterOptions
                .Where(option => request.Pokemon.Contains(option.Id))
                .Any(option =>
                    (" " + card.Name + " ").Contains(" " + option.Name + " ")
                    || (option.AlternateName != null
                        && (" " + card.Name + " ").Contains(" " + option.AlternateName + " "))));
        }

        // A generation reaches a card only through the pokemon that belong to it, so this matches
        // names the same way the pokemon filter above does.
        if (request.Generations.Count > 0)
        {
            cards = cards.Where(card => card.Name != null && context.PokemonFilterOptions
                .Where(option => request.Generations.Contains(option.GenerationId))
                .Any(option =>
                    (" " + card.Name + " ").Contains(" " + option.Name + " ")
                    || (option.AlternateName != null
                        && (" " + card.Name + " ").Contains(" " + option.AlternateName + " "))));
        }

        return cards;
    }

    private static readonly Expression<Func<Card, CardSearchResult>> MapResult =
        card => new CardSearchResult()
        {
            Id = card.Id,
            Name = card.Name,
            Rarity = card.Rarity,
            CardNumber = card.CardNumber,
            TcgPlayerId = card.TcgPlayerId,
            ImageUrl = card.ImageUrl,
            SetName = card.Set != null ? card.Set.Name : null,
        };
}
