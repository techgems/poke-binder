namespace PokeBinder.TcgCatalog.DbContext.Entities;

public class CardTag
{
    public int CardId { get; set; }

    public int TagId { get; set; }

    public Card Card { get; set; } = null!;

    public Tag Tag { get; set; } = null!;
}
