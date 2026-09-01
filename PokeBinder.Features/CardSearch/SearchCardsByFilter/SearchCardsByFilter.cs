using PokeBinder.Features.CardSearch.SearchCardsByFilter.Models;

namespace PokeBinder.Features.CardSearch.SearchCardsByFilter;

public static class SearchCardsByFilter
{
    public record Request
    {
        public string? Name { get; set; }

        public string? Rarity { get; set; }

        public string? CardNumber { get; set; }

        public int? TcgPlayerId { get; set; }

        public int? SetId { get; set; }

        public int? GenerationId { get; set; }
    }

    public record Response(IReadOnlyList<CardSearchResult> Results);
}
