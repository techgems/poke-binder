namespace PokeBinder.Features.Binder.SaveBinderChanges.Models;

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
    /// <para>
    /// It has to fall inside the pages the request claims -- see
    /// <see cref="SaveBinderChanges.Request.Pages"/>. A card outside them is a client that has lost
    /// track of which spread it is saving, not a card on another page.
    /// </para>
    /// </summary>
    public int IndexInBinder { get; set; }

    /// <summary>
    /// Marks a pocket the collector has reserved for a card they do not own yet. Carried on the
    /// request because the save replaces the claimed pages wholesale -- a client that leaves it out
    /// clears the flag on every card it sends, so a client that does not offer the flag still has
    /// to round-trip what it was given.
    /// </summary>
    public bool IsMissing { get; set; }

    /// <summary>
    /// Zero to three labels on this pocket, in the order they appear in the chip strip. Each id is
    /// one the binder already has -- the Create-and-apply flow calls <c>CreateBinderCardLabel</c>
    /// first and lands the real id here. Carried wholesale like <see cref="IsMissing"/>: within
    /// the claim, whatever labels the pocket had are replaced by this list, and leaving it empty
    /// is "this pocket has no labels". The validator refuses an id not on this binder, duplicates
    /// in one pocket, and a list past three.
    /// </summary>
    public IReadOnlyList<int> LabelIds { get; set; } = [];
}
