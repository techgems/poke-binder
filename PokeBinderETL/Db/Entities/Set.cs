namespace PokeBinder.ETL.Db.Entities;

public class Set
{
    public int Id { get; set; }

    public string Code { get; set; }

    public string Name { get; set; }

    public string FullName { get; set; }

    public long? ReleaseDateUnix { get; set; }

    public string? ImageUrl { get; set; }

    public int GenerationId { get; set; }

    public bool PriorityOrder { get; set; } = false;

    public long DateLoadedUnix { get; set; }
}
