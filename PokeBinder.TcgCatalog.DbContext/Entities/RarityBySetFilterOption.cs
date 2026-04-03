namespace PokeBinder.TcgCatalog.DbContext.Entities;

public class RarityBySetFilterOption
{
    public int Id { get; set; }

    public int SetId { get; set; }

    public string Rarity { get; set; } = string.Empty;

    public Set Set { get; set; } = null!;
}
