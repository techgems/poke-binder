namespace PokeBinder.Features.Binder.SaveBinderTray.Models;

/// <summary>
/// One card sitting in the tray, as the front end holds it: which card, and how many copies.
/// </summary>
public class TrayCard
{
    public int CardId { get; set; }

    /// <summary>
    /// Copies of this card in the tray. Never zero -- a card the user took out is absent from the
    /// request rather than present with nothing in it.
    /// </summary>
    public int Quantity { get; set; }
}
