namespace PokeBinder.Binders.DbContext.Entities;

/// <summary>
/// One binder a user has built: a grid (<see cref="BinderSize"/>) repeated over
/// <see cref="Pages"/> pages, the cards placed in its pockets (<see cref="Cards"/>), and the
/// staging area those cards are placed from (<see cref="Tray"/>).
/// </summary>
public class Binder
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Unix time in seconds, UTC.</summary>
    public long CreatedAt { get; set; }

    public int UserId { get; set; }

    public int BinderSizeId { get; set; }

    /// <summary>
    /// How many pages this binder has. Stored per binder rather than per size: the grid says how
    /// a page is laid out, the binder says how many of those pages it has.
    /// </summary>
    public int Pages { get; set; }

    public BinderSize? BinderSize { get; set; }

    /// <summary>
    /// Cards placed in the binder's pockets, keyed by the slot they sit in.
    /// </summary>
    public ICollection<BinderCard> Cards { get; set; } = new List<BinderCard>();

    /// <summary>
    /// This binder's tray: the cards the user has pulled aside for it but not yet placed on a page.
    /// It is scoped to the binder, so each binder is managed in its own context, and it starts
    /// empty -- a new binder needs no tray row of its own, only tray entries keyed by its id.
    /// </summary>
    public ICollection<BinderTray> Tray { get; set; } = new List<BinderTray>();

    /// <summary>
    /// How many pockets this binder has in total. Derived and never stored: it is the grid's
    /// pockets per page times <see cref="Pages"/>, so storing it would create a value that goes
    /// stale the moment either factor is edited.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <see cref="BinderSize"/> has not been loaded, so there is no grid to multiply by.
    /// </exception>
    public int CardCount => BinderSize is null
        ? throw new InvalidOperationException(
            $"Binder {Id}: {nameof(BinderSize)} must be loaded before reading {nameof(CardCount)}.")
        : BinderSize.CardsPerPage * Pages;
}
