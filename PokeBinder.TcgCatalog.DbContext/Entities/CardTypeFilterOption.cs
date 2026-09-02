namespace PokeBinder.TcgCatalog.DbContext.Entities;

public class CardTypeFilterOption
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>App-relative URL of the energy symbol art, or null when the type has no art.</summary>
    public string? ImageUrl { get; set; }
}
