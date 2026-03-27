namespace PokeBinder.ETL.Db.Entities;

public class Card
{
    public int Id { get; set; }

    public int TcgPlayerId { get; set; }

    public int? SetId { get; set; }

    public string? Name { get; set; }

    public string? Rarity { get; set; }

    public string? CardNumber { get; set; }

    public string? ImageUrl { get; set; }

    public bool HasImageDownloadAttempt { get; set; }
}