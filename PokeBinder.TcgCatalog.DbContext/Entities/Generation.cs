namespace PokeBinder.TcgCatalog.DbContext.Entities;

public class Generation
{
    public int Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public long StartDateUnix { get; set; }

    public long? EndDateUnix { get; set; }

    public int GameId { get; set; }

    public Game Game { get; set; }

    public ICollection<Set> Sets { get; set; } = [];
}
