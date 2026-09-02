using Microsoft.EntityFrameworkCore;
using PokeBinder.Features.CardSearch.SearchCardsByFilter.Models;
using PokeBinder.TcgCatalog.DbContext;
using PokeBinder.TcgCatalog.DbContext.Entities;
using System.Linq.Expressions;

namespace PokeBinder.Features.CardSearch.SearchCardsByFilter;

public static class SearchCardsByFilter
{
    /// <summary>
    /// Every field mirrors one multi-select filter field in the UI. An empty list means the user
    /// narrowed nothing by that field, so it is left out of the query entirely.
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
    }

    public record Response(IReadOnlyList<CardSearchResult> Results);

    public static async Task<Response> Handler(
        Request request,
        TcgCatalogDbContext context,
        CancellationToken ct = default)
    {
        var results = await ApplyFilters(context.Cards, request, context)
            // Without an order the results shift between calls, so paging or diffing them later
            // would be unreliable.
            .OrderBy(card => card.Name)
            .ThenBy(card => card.Id)
            .Select(MapResult)
            .ToListAsync(ct);

        return new Response(results);
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
