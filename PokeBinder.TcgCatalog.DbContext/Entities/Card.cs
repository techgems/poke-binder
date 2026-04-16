namespace PokeBinder.TcgCatalog.DbContext.Entities;

public class Card
{
    public int Id { get; set; }

    public int TcgPlayerId { get; set; }

    public int? ScrydexId { get; set; } = null;

    public int? SetId { get; set; }

    public string? Name { get; set; }

    public string? Rarity { get; set; }

    public string? CardNumber { get; set; }

    public string? ImageUrl { get; set; }

    public string CardType { get; set; } = string.Empty;

    public string CardSubtype { get; set; } = string.Empty;

    public string? Artist { get; set; }

    public string? MaskImageOneUrl { get; set; }

    public string? MaskImageTwoUrl { get; set; }

    public bool HasImageDownloadAttempt { get; set; }

    public Set? Set { get; set; }

    public PokemonCardText? PokemonCardText { get; set; }

    public NonPokemonCardText? NonPokemonCardText { get; set; }
}
