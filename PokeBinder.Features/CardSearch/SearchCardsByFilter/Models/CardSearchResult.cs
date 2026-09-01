namespace PokeBinder.Features.CardSearch.SearchCardsByFilter.Models;

public class CardSearchResult
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Rarity { get; set; }

    public string? CardNumber { get; set; }

    public int TcgPlayerId { get; set; }

    public string? ImageUrl { get; set; }

    public string? SetName { get; set; }

    public string? GenerationName { get; set; }
}
