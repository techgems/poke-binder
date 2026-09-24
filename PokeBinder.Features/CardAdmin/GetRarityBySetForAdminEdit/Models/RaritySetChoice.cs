namespace PokeBinder.Features.CardAdmin.GetRarityBySetForAdminEdit.Models;

/// <summary>
/// One set as the editor's set picker lists it. The code travels with the name because two sets
/// can read alike at a glance -- "ME: Ascended Heroes" against "ME: Phantasmal Flames" -- and the
/// code is what the catalog and the CSV files call them.
/// </summary>
public class RaritySetChoice
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// How many of this set's rarities still carry no weight at all -- neither column filled in.
    /// It is what turns the picker into a worklist: 53 sets have to be gone through by hand, and
    /// the only question the person sitting down to it has is which ones are left.
    /// </summary>
    public int UnweightedCount { get; set; }
}
