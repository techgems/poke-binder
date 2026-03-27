namespace PokeBinder.ETL.Db.Entities;

public class Generation
{
    public int Id { get; set; }

    public string Slug { get; set; }

    public string Name { get; set; }

    public long StartDateUnix { get; set; }

    public long? EndDateUnix { get; set; }

    public int GameId { get; set; }
}
