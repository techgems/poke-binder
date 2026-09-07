namespace PokeBinder.Features.Binder.GetFullBinder.Models;

/// <summary>
/// One card in one pocket, with the card it is.
/// </summary>
public class BinderPlacement
{
    /// <summary>
    /// The pocket, counted from zero across the whole binder. This is what is stored and what
    /// SaveBinderCards takes back; the two below are it, divided by the grid.
    /// </summary>
    public int IndexInBinder { get; set; }

    /// <summary>Which page the pocket is on, counted from one, the way a user counts pages.</summary>
    public int Page { get; set; }

    /// <summary>
    /// The pocket's place on that page, counted from zero, so it indexes a grid directly.
    /// </summary>
    public int SlotOnPage { get; set; }

    /// <summary>A pocket reserved for a card the collector does not own yet.</summary>
    public bool IsMissing { get; set; }

    /// <summary>
    /// The card itself, or null when the binder points at a card the catalog no longer has -- the
    /// two live in separate databases with no foreign key between them, so this is possible and a
    /// missing row must not take the whole binder down with it.
    /// </summary>
    public BinderCardDetails? Card { get; set; }
}
