using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PokeBinder.Components.Admin;
using PokeBinder.Features.CardAdmin.GetRarityBySetForAdminEdit;
using PokeBinder.Features.CardAdmin.GetRarityBySetForAdminEdit.Models;
using PokeBinder.Features.CardAdmin.UpdateRarityBySet;
using PokeBinder.Features.CardAdmin.UpdateRarityBySet.Models;
using PokeBinder.TcgCatalog.DbContext;

namespace PokeBinder.Pages.Admin;

/// <summary>
/// What each rarity is worth in each set: the two numbers a binder is sorted by, entered by hand
/// one set at a time.
///
/// <para>
/// Nothing derives these values, and nothing can -- a pull rate is a fact about a print run and a
/// name ordering is a judgement -- so this page is where they come from. It edits only: which
/// rarities a set contains is written when the set is loaded, and a row cannot be added or
/// removed from here.
/// </para>
///
/// <para>
/// All three handlers answer with the <see cref="RaritySetEditor"/> component rather than with a
/// redirect, so choosing a set, saving and being refused all land as the same element swapped in
/// place. The page renders that component through its tag helper and the handlers render its view
/// directly -- same view, same model, so what htmx swaps in cannot drift from what it replaced.
/// </para>
/// </summary>
[Authorize]
public class SetRarityModel : PageModel
{
    private readonly TcgCatalogDbContext _catalog;

    public SetRarityModel(TcgCatalogDbContext catalog)
    {
        _catalog = catalog;
    }

    /// <summary>
    /// The posted rows. A page-level type rather than the slice's own
    /// <see cref="RarityWeightEdit"/>, for one reason: the slice's request holds an
    /// <c>IReadOnlyList</c>, which ASP.NET's form binder cannot fill -- it needs a list it can add
    /// to. The shape is the same and <see cref="ToRequest"/> is the whole of the translation.
    /// </summary>
    public class RarityWeightInput
    {
        public int Id { get; set; }

        public int? PullRateRarityOrder { get; set; }

        public int? NameRarityOrder { get; set; }
    }

    /// <summary>
    /// The set the posted rows belong to, from the editor's hidden field rather than from the
    /// picker: the picker is what the user is looking at now, and a save is for the set whose rows
    /// were rendered.
    /// </summary>
    [BindProperty]
    public int SetId { get; set; }

    /// <summary>
    /// The rows as the form posted them. Bound under this exact name because the validator
    /// reports its failures as <c>Weights[2].PullRateRarityOrder</c>, and those keys are what put
    /// a message under the right input.
    /// </summary>
    [BindProperty]
    public List<RarityWeightInput> Weights { get; set; } = [];

    public IReadOnlyList<RaritySetChoice> Sets { get; private set; } = [];

    public GetRarityBySetForAdminEdit.SelectedSet? Selected { get; private set; }

    public IReadOnlyDictionary<string, string[]> Errors { get; private set; } =
        new Dictionary<string, string[]>();

    public string? Saved { get; private set; }

    /// <param name="setId">
    /// Optional, so the page can be linked to with a set already open. An id that is not a set
    /// renders the picker, which is the only honest answer to a link that has gone stale.
    /// </param>
    public async Task OnGetAsync(int? setId, CancellationToken ct)
    {
        await LoadAsync(setId, ct);
    }

    /// <summary>The picker changed: the same editor, now showing that set's rows.</summary>
    public async Task<IActionResult> OnGetSelectAsync(int? setId, CancellationToken ct)
    {
        await LoadAsync(setId, ct);

        return Editor();
    }

    /// <summary>
    /// Writes the posted rows. On a refusal the editor comes back carrying what was typed rather
    /// than what is stored -- a form that silently discards the numbers somebody just entered is
    /// worse than the mistake it is reporting.
    /// </summary>
    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        var request = ToRequest();

        var validation = await new UpdateRarityBySetValidator(_catalog).ValidateAsync(request, ct);

        if (!validation.IsValid)
        {
            Errors = validation.Errors
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

            await LoadAsync(SetId, ct);

            KeepWhatWasTyped();

            return Editor();
        }

        var response = await UpdateRarityBySet.Handler(request, _catalog, ct);

        await LoadAsync(SetId, ct);

        Saved = response.Updated == 1
            ? "1 rarity saved."
            : $"{response.Updated} rarities saved.";

        return Editor();
    }

    private UpdateRarityBySet.Request ToRequest() =>
        new()
        {
            SetId = SetId,
            Weights = Weights
                .Select(weight => new RarityWeightEdit
                {
                    Id = weight.Id,
                    PullRateRarityOrder = weight.PullRateRarityOrder,
                    NameRarityOrder = weight.NameRarityOrder,
                })
                .ToList(),
        };

    private async Task LoadAsync(int? setId, CancellationToken ct)
    {
        var response = await GetRarityBySetForAdminEdit.Handler(
            new GetRarityBySetForAdminEdit.Request(setId),
            _catalog,
            ct);

        Sets = response.Sets;
        Selected = response.Selected;
    }

    /// <summary>
    /// Puts the posted values back over the stored ones, leaving the rarity names and card counts
    /// as the catalog has them. A row that was not posted keeps what is stored, which is the same
    /// thing the save itself would have done with it.
    ///
    /// <para>
    /// <b>And keeps the posted order.</b> The failures are keyed by where a row sat in the payload
    /// -- <c>Weights[2].NameRarityOrder</c> -- while a fresh read orders the rows by what is
    /// stored, which is exactly what the refused save was trying to change. Re-rendering in the
    /// stored order would leave every message one row out of place.
    /// </para>
    /// </summary>
    private void KeepWhatWasTyped()
    {
        if (Selected is null)
        {
            return;
        }

        var stored = Selected.Rarities.ToDictionary(row => row.Id);

        var rows = new List<RarityWeightRow>();

        foreach (var weight in Weights)
        {
            // A posted row the set does not have. The validator has already refused the save over
            // it; there is no name or card count to draw it with, so it is left out.
            if (!stored.Remove(weight.Id, out var row))
            {
                continue;
            }

            row.PullRateRarityOrder = weight.PullRateRarityOrder;
            row.NameRarityOrder = weight.NameRarityOrder;

            rows.Add(row);
        }

        // Anything the form did not post, in the order the catalog gave it, after the rest.
        rows.AddRange(Selected.Rarities.Where(row => stored.ContainsKey(row.Id)));

        Selected = Selected with { Rarities = rows };
    }

    private IActionResult Editor() =>
        Partial(RaritySetEditor.ViewPath, new RaritySetEditor
        {
            Sets = Sets,
            Selected = Selected,
            Errors = Errors,
            Saved = Saved,
        });
}
