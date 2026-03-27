namespace PokeBinder.TcgCatalog.DomainModels.Queries;

public class CardSearchQuery
{
    public string? Name { get; set; }

    public string? Rarity { get; set; }

    public string? CardNumber { get; set; }

    public int? TcgPlayerId { get; set; }

    public int? SetId { get; set; }

    public int? GenerationId { get; set; }
}
