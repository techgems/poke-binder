namespace PokeBinder.Features.Binder.GetFullBinder.Models;

/// <summary>
/// A card as the binder needs to draw it. Deliberately the same shape as the card searches return,
/// so the front end can render a placed card, a tray card and a search result with one component
/// and one type.
/// <para>
/// It comes from the catalog database, which is a separate file from the binder -- so these fields
/// are read in a second query and stitched onto the placements, not joined to them.
/// </para>
/// </summary>
public class BinderCardDetails
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Rarity { get; set; }

    public string? CardNumber { get; set; }

    public int TcgPlayerId { get; set; }

    /// <summary>Loadable URL for the art, already resolved from the ETL's local file path.</summary>
    public string? ImageUrl { get; set; }

    public string? SetName { get; set; }
}
