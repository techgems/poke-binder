namespace PokeBinder.Features.CardAdmin.GetRarityBySetForAdminEdit.Models;

/// <summary>
/// One rarity of one set, as the editor shows it: what it is worth today, and the card count that
/// makes a sensible guess possible.
/// </summary>
public class RarityWeightRow
{
    /// <summary>
    /// The rarityBySetFilterOption row. It is what an edit names -- the editor cannot add a rarity
    /// or remove one, so every row it posts back is one that came from here.
    /// </summary>
    public int Id { get; set; }

    public string Rarity { get; set; } = string.Empty;

    /// <summary>Packs-to-pull. Null is unweighted; see the entity for which way the numbers run.</summary>
    public int? PullRateRarityOrder { get; set; }

    /// <summary>Rank by name. Null is unweighted.</summary>
    public int? NameRarityOrder { get; set; }

    /// <summary>
    /// How many cards of this rarity the set holds. Not a weight and never used as one -- two
    /// cards out of five hundred is a hint about a pull rate, not the pull rate -- but it is most
    /// of what makes the number guessable without a pull table to hand.
    /// </summary>
    public int CardCount { get; set; }

    /// <summary>Neither column filled in. The state every row starts in.</summary>
    public bool IsUnweighted => PullRateRarityOrder is null && NameRarityOrder is null;
}
