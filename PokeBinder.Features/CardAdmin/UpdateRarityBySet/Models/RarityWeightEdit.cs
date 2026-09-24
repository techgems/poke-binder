namespace PokeBinder.Features.CardAdmin.UpdateRarityBySet.Models;

/// <summary>
/// One row of the editor as it comes back: which rarityBySetFilterOption row it is, and the two
/// values for it.
///
/// <para>
/// The rarity's name is not here, and neither is its set. Both are the catalog's own facts and
/// neither is editable, so sending them would only create a way for the form and the database to
/// disagree about what row <see cref="Id"/> names.
/// </para>
/// </summary>
public record RarityWeightEdit
{
    public int Id { get; init; }

    /// <summary>
    /// Packs-to-pull for this rarity in this set. Null is the field left empty, which means
    /// unweighted -- and clearing a value that was there is a real edit, not a no-op, so null is
    /// written through rather than skipped.
    /// </summary>
    public int? PullRateRarityOrder { get; init; }

    /// <summary>Rank by rarity name. Null is the field left empty, as above.</summary>
    public int? NameRarityOrder { get; init; }
}
