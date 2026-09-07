namespace PokeBinder.Features.Binder.SaveBinderCards.Models;

/// <summary>
/// One card sitting in one pocket of the binder.
/// </summary>
public class PlacedCard
{
    public int CardId { get; set; }

    /// <summary>
    /// Which pocket, counted from zero across the whole binder rather than per page: pocket 9 of a
    /// 3x3 binder is the first pocket of page two. The page and the slot on it are arithmetic over
    /// this and the grid, so storing them separately would only create a second thing to keep true.
    /// </summary>
    public int IndexInBinder { get; set; }

    /// <summary>
    /// Marks a pocket the collector has reserved for a card they do not own yet. Carried on the
    /// request because a save replaces the binder's cards wholesale -- leaving it out would clear
    /// the flag on every card every time anything moved.
    /// </summary>
    public bool IsMissing { get; set; }
}
