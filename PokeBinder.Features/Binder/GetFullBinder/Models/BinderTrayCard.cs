namespace PokeBinder.Features.Binder.GetFullBinder.Models;

/// <summary>
/// One card waiting in the binder's tray, and how many copies of it.
/// </summary>
public class BinderTrayCard
{
    public int Quantity { get; set; }

    /// <summary>
    /// The card itself, or null when the catalog no longer has it -- see BinderPlacement.Card.
    /// </summary>
    public BinderCardDetails? Card { get; set; }
}
