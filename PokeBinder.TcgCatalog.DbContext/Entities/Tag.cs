namespace PokeBinder.TcgCatalog.DbContext.Entities;

public class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ExtendedName { get; set; }

    public ICollection<CardTag> CardTags { get; set; } = [];
}
