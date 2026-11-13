namespace PokeBinder.TcgCatalog.DbContext.Entities;

public class Set
{
    public int Id { get; set; }

    /// <summary>TCGplayer's code for the set (<c>ME02</c>, <c>ME-AH</c>). Unique, and every set has one.</summary>
    public string TcgPlayerCode { get; set; } = string.Empty;

    /// <summary>
    /// TCGdex's id for the set (<c>me02</c>, <c>me02.5</c>), or null for a set TCGdex does not have.
    /// Unique where set.
    /// </summary>
    public string? TcgDexCode { get; set; }

    public string Name { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public long ReleaseDateUnix { get; set; }

    public string? ImageUrl { get; set; }

    public int SeriesId { get; set; }

    public bool PriorityOrder { get; set; }

    public long DateLoadedUnix { get; set; }

    public Series? Series { get; set; }

    public ICollection<Card> Cards { get; set; } = [];
}
