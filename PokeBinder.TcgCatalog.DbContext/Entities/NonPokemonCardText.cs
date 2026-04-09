namespace PokeBinder.TcgCatalog.DbContext.Entities;

public class NonPokemonCardText
{
    public int Id { get; set; }

    public string? Text { get; set; }

    public int CardId { get; set; }

    public Card Card { get; set; }
}
