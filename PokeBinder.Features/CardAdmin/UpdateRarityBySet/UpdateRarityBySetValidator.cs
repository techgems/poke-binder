using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Features.CardAdmin.UpdateRarityBySet.Models;
using PokeBinder.TcgCatalog.DbContext;

namespace PokeBinder.Features.CardAdmin.UpdateRarityBySet;

/// <summary>
/// Everything that can be wrong with an edit to one set's rarity weights. It runs first and the
/// handler runs only if it passes, which is why the handler writes without checking anything.
///
/// <para>
/// The rule that gives this validator its shape is uniqueness, and it applies to
/// <c>NameRarityOrder</c> alone. That column is a ranking of the rarity names within one set, so
/// two rarities holding the same number is a ranking that does not say which of them comes first.
/// It is checked against the set <em>as it will stand after the save</em> -- posted rows and
/// untouched rows together -- because a form that posts half the set can still collide with the
/// half it did not send.
/// </para>
///
/// <para>
/// <b>Pull rates may repeat.</b> A pull rate is a measurement rather than a ranking: two rarities
/// in a set really can take about the same number of packs, and refusing to record that would
/// make the person entering it round one of them to a number they know is wrong. What it costs is
/// that a sort by pull rate meets ties, so the comparator needs a tie-break -- which it needs in
/// any case for the rarities nobody has weighted.
/// </para>
/// </summary>
/// <param name="context">
/// The catalog: the set has to exist, the rows have to belong to it, and the rows the payload does
/// not carry are still part of the name-order check.
/// </param>
public class UpdateRarityBySetValidator : AbstractValidator<UpdateRarityBySet.Request>
{
    public UpdateRarityBySetValidator(TcgCatalogDbContext context)
    {
        RuleFor(request => request.SetId)
            .MustAsync((setId, ct) => context.Sets.AnyAsync(set => set.Id == setId, ct))
                .WithMessage("That set no longer exists.");

        // Cascade.Stop for the reason every list rule in this codebase has it: the rules after
        // NotNull count and project the value, so without the stop a null list records its failure
        // and then throws on the next rule anyway -- a 500 in place of the failure already found.
        RuleFor(request => request.Weights)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(weights => weights.Count > 0)
                .WithMessage("There is nothing to save.")
            // One row per rarity. A repeated id is a form that rendered the same row twice, and
            // the second copy would silently win.
            .Must(weights => weights.Select(weight => weight.Id).Distinct().Count() == weights.Count)
                .WithMessage("The same rarity was sent twice.");

        RuleForEach(request => request.Weights).ChildRules(weight =>
        {
            // Null is the field left empty, which is itself a value: unweighted. Zero and
            // negatives are not -- a second way to say "unweighted" is exactly what would leave a
            // comparator with two answers.
            weight.RuleFor(row => row.PullRateRarityOrder)
                .InclusiveBetween(1, UpdateRarityBySet.MaxOrder)
                    .When(row => row.PullRateRarityOrder is not null)
                    .WithMessage($"A pull rate is between 1 and {UpdateRarityBySet.MaxOrder} packs, or empty.");

            weight.RuleFor(row => row.NameRarityOrder)
                .InclusiveBetween(1, UpdateRarityBySet.MaxOrder)
                    .When(row => row.NameRarityOrder is not null)
                    .WithMessage($"A name order is between 1 and {UpdateRarityBySet.MaxOrder}, or empty.");
        });

        // One read of the set's rows answers both of the remaining questions -- do these rows
        // belong to this set, and does the result collide -- so they share a rule rather than
        // reading the same eight rows twice.
        RuleFor(request => request)
            .CustomAsync(async (request, validation, ct) =>
            {
                // Everything the list rule above already refused. This rule runs whatever that one
                // found -- FluentValidation's cascade stops a chain, not the validator -- and each
                // of these would come back out of here as an exception instead: a null list throws
                // on the first projection, and a repeated id throws in the dictionaries below.
                // Saying nothing leaves the message to the rule that owns it.
                if (request.Weights is null
                    || request.Weights.Count == 0
                    || request.Weights.Select(weight => weight.Id).Distinct().Count() != request.Weights.Count)
                {
                    return;
                }

                var stored = await context.RarityBySetFilterOptions
                    .AsNoTracking()
                    .Where(rarity => rarity.SetId == request.SetId)
                    .Select(rarity => new
                    {
                        rarity.Id,
                        rarity.Rarity,
                        rarity.PullRateRarityOrder,
                        rarity.NameRarityOrder,
                    })
                    .ToDictionaryAsync(rarity => rarity.Id, ct);

                // No inserts: a row this set does not have is refused rather than created. The
                // same rule covers the cross-set case, which is the same mistake from the other
                // side -- a form posting back the rows of the set it was showing a moment ago.
                var strangers = request.Weights
                    .Where(weight => !stored.ContainsKey(weight.Id))
                    .ToList();

                if (strangers.Count > 0)
                {
                    validation.AddFailure(
                        nameof(UpdateRarityBySet.Request.Weights),
                        $"{strangers.Count} of the rarities sent {(strangers.Count == 1 ? "is" : "are")} " +
                        "not in this set. Reload the page and try again.");

                    // The uniqueness check below names the rarities that collide, and these rows
                    // have no name to give. Reloading is the answer to this one anyway.
                    return;
                }

                var edits = request.Weights.ToDictionary(weight => weight.Id);

                // The set's name orders as they will stand: the posted values where a row was
                // posted, the stored ones everywhere else. Pull rates are not gathered because
                // nothing below checks them -- two rarities in one set may share one.
                var after = stored.Values
                    .Select(row => edits.TryGetValue(row.Id, out var edit)
                        ? (row.Id, row.Rarity, Name: edit.NameRarityOrder)
                        : (row.Id, row.Rarity, Name: row.NameRarityOrder))
                    .ToList();

                // Where each row sits in the payload, so a collision is reported on the field the
                // person just typed in rather than on the form as a whole.
                var indexes = request.Weights
                    .Select((weight, index) => (weight.Id, Index: index))
                    .ToDictionary(entry => entry.Id, entry => entry.Index);

                // Unweighted rows are exempt: null is not a value two rarities are sharing, it is
                // two rarities that have not been given one.
                var collisions = after
                    .Where(row => row.Name is not null)
                    .GroupBy(row => row.Name!.Value)
                    .Where(group => group.Count() > 1);

                foreach (var collision in collisions)
                {
                    var names = string.Join(" and ", collision.Select(row => row.Rarity).Order());

                    // Reported on every posted row in the collision, so a clash between a posted
                    // row and an untouched one still lands on a field that can be changed.
                    foreach (var row in collision.Where(row => indexes.ContainsKey(row.Id)))
                    {
                        validation.AddFailure(
                            $"{nameof(UpdateRarityBySet.Request.Weights)}[{indexes[row.Id]}]." +
                            nameof(RarityWeightEdit.NameRarityOrder),
                            $"{names} cannot share a name order of {collision.Key} in one set.");
                    }
                }
            });
    }
}
