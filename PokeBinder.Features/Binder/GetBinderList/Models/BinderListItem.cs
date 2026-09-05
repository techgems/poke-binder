namespace PokeBinder.Features.Binder.GetBinderList.Models;

/// <summary>
/// One binder on the list a user picks from. Enough to recognise a binder and decide to open it,
/// and no more: the cards it holds and the tray staged for it are counted here, never listed. A
/// user with thirty full binders would otherwise pay for ten thousand rows to render a menu.
/// </summary>
public class BinderListItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Unix time in seconds, UTC.</summary>
    public long CreatedAt { get; set; }

    /// <summary>The grid's name, e.g. "3x3".</summary>
    public string SizeName { get; set; } = string.Empty;

    /// <summary>Pockets on one page: the grid's x * y.</summary>
    public int CardsPerPage { get; set; }

    public int Pages { get; set; }

    /// <summary>
    /// Cards the user has put in the binder, counted rather than listed. Counts every filled
    /// pocket, including the ones flagged as missing below -- a slot the user has assigned a card
    /// to is a card they have added to the binder, whether or not it is in their hands yet.
    /// </summary>
    public int CardsAdded { get; set; }

    /// <summary>
    /// The subset of <see cref="CardsAdded"/> flagged as not yet owned, so a list entry can read
    /// "142 cards, 8 still missing" without a second query.
    /// </summary>
    public int CardsMissing { get; set; }

    /// <summary>
    /// Pockets in the whole binder. Derived here for the same reason it is derived on the entity:
    /// it is <see cref="CardsPerPage"/> * <see cref="Pages"/>, so sending it as its own column
    /// would only add a number that can disagree with the two it comes from.
    /// </summary>
    public int CardCount => CardsPerPage * Pages;
}
