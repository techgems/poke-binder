namespace PokeBinder.TcgCatalog.DbContext.Entities;

public class Set
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public long? ReleaseDateUnix { get; set; }

    public string? ImageUrl { get; set; }

    public int GenerationId { get; set; }

    public bool PriorityOrder { get; set; }

    public long DateLoadedUnix { get; set; }

    public Generation? Generation { get; set; }

    public ICollection<Card> Cards { get; set; } = [];
}
