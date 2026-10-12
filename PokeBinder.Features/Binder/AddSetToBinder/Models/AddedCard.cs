namespace PokeBinder.Features.Binder.AddSetToBinder.Models;

/// <summary>
/// A card the set put in the binder, as the workspace draws it -- the same shape as a search result,
/// so it can go straight into a pocket. Only the set's cards are sent: the workspace already holds
/// every card that was in the binder before.
/// </summary>
public class AddedCard
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
