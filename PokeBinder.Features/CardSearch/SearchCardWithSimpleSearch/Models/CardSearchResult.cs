namespace PokeBinder.Features.CardSearch.SearchCardWithSimpleSearch.Models;

/// <summary>
/// A single card on a simple-search page. Deliberately the same shape as the filtered search's
/// result: the UI pours both searches into one list of cards, so the two have to stay
/// interchangeable on the wire. They are separate types all the same — each slice owns its own
/// contract, and either search is free to grow a field the other has no use for.
/// </summary>
public class CardSearchResult
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Rarity { get; set; }

    public string? CardNumber { get; set; }

    public int TcgPlayerId { get; set; }

    public string? ImageUrl { get; set; }

    public string? SetName { get; set; }
}
