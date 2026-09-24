using PokeBinder.Features.CardAdmin.GetRarityBySetForAdminEdit;
using PokeBinder.Features.CardAdmin.GetRarityBySetForAdminEdit.Models;
using PokeBinder.Features.CardAdmin.UpdateRarityBySet;
using TechGems.StaticComponents;

namespace PokeBinder.Components.Admin;

/// <summary>
/// The rarity-weight editor: the set picker and, once a set is chosen, one editable row per
/// rarity it contains.
///
/// <para>
/// The picker is inside the swapped element rather than outside it, because it carries how many
/// rarities each set still has no weight for -- a worklist, and one that is wrong the moment a
/// save lands if the swap leaves it behind.
/// </para>
///
/// <para>
/// Like <see cref="FilterCacheStampList"/>, this is one component for the whole element rather
/// than a component per row: the page renders it through its tag helper and the handlers render
/// the same view directly, so what htmx swaps in cannot drift from what it replaced.
/// </para>
/// </summary>
public class RaritySetEditor : StaticNode
{
    /// <summary>The element htmx replaces. Both the picker and the save button target it.</summary>
    public const string SwapTargetId = "rarity-set-editor";

    /// <summary>
    /// This component's view, for the handlers that answer an htmx request with the editor rather
    /// than with a whole page. Named here so a moved file is a compile error and not a blank swap.
    /// </summary>
    public const string ViewPath = "~/Components/Admin/RaritySetEditor.cshtml";

    /// <summary>Every set, newest first, with how much of each is still unweighted.</summary>
    public IReadOnlyList<RaritySetChoice> Sets { get; set; } = [];

    /// <summary>The chosen set and its rows, or null when nothing is chosen yet.</summary>
    public GetRarityBySetForAdminEdit.SelectedSet? Selected { get; set; }

    /// <summary>
    /// Validation failures from the last save, keyed exactly as the form names its fields --
    /// <c>Weights[2].PullRateRarityOrder</c> -- so a message lands under the input that caused it.
    /// The validator writes those same keys, which is the whole reason the form uses them.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; set; } =
        new Dictionary<string, string[]>();

    /// <summary>What to say after a save that worked. Null the rest of the time.</summary>
    public string? Saved { get; set; }

    /// <summary>The ceiling both inputs carry, so the browser refuses a paste before the server does.</summary>
    public static int MaxOrder => UpdateRarityBySet.MaxOrder;

    /// <summary>
    /// The name a row's field is posted under. It is the shape ASP.NET's model binder fills a
    /// list from and the shape the validator reports failures against, and those two only line up
    /// because both are built from this.
    /// </summary>
    public static string FieldName(int index, string property) => $"Weights[{index}].{property}";

    /// <summary>The failures for one field, or empty. Every field can show more than one.</summary>
    public IReadOnlyList<string> ErrorsFor(string field) =>
        Errors.TryGetValue(field, out var messages) ? messages : [];

    /// <summary>Failures that belong to no single input: the set, the payload as a whole.</summary>
    public IReadOnlyList<string> FormErrors() =>
        Errors
            .Where(entry => !entry.Key.StartsWith("Weights["))
            .SelectMany(entry => entry.Value)
            .ToList();

    /// <summary>
    /// How a set reads in the picker. The count is the point of it -- which sets are still to do
    /// is the only question somebody opening this page has.
    /// </summary>
    public static string SetLabel(RaritySetChoice set) =>
        set.UnweightedCount == 0
            ? $"{set.Name} ({set.Code})"
            : $"{set.Name} ({set.Code}) -- {set.UnweightedCount} unweighted";
}
