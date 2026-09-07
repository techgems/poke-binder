namespace PokeBinder.Features.Binder.GetFullBinder.Models;

/// <summary>
/// The binder itself: what it is called and what shape it is. Everything a client needs to lay out
/// an empty binder, before a single card is placed in it.
/// </summary>
public class BinderSummary
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Unix time in seconds, UTC.</summary>
    public long CreatedAt { get; set; }

    public int Pages { get; set; }

    public int BinderSizeId { get; set; }

    /// <summary>The grid's name, e.g. "3x3".</summary>
    public string SizeName { get; set; } = string.Empty;

    /// <summary>What a page of it holds, in words, e.g. "9 cards per page".</summary>
    public string SizeDescription { get; set; } = string.Empty;

    /// <summary>Pockets across a page.</summary>
    public int X { get; set; }

    /// <summary>Pockets down a page.</summary>
    public int Y { get; set; }

    /// <summary>Pockets on one page: x * y.</summary>
    public int CardsPerPage { get; set; }

    /// <summary>Pockets in the whole binder: cards per page times pages.</summary>
    public int CardCount { get; set; }
}
