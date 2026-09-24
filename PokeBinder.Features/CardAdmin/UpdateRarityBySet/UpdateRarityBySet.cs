using Microsoft.EntityFrameworkCore;
using PokeBinder.Features.CardAdmin.UpdateRarityBySet.Models;
using PokeBinder.TcgCatalog.DbContext;
using PokeBinder.TcgCatalog.DbContext.Entities;

// The stamp slice's namespace and its class share a name, so the class is aliased rather than
// spelled out in full at the one place it is called.
using BumpStamps = PokeBinder.Features.CardAdmin.BumpFilterCacheStamps.BumpFilterCacheStamps;

namespace PokeBinder.Features.CardAdmin.UpdateRarityBySet;

/// <summary>
/// Writes what one set's rarities are worth: the two order columns on
/// <see cref="RarityBySetFilterOption"/>, as entered at /admin/setRarity.
///
/// <para>
/// <b>Edits only -- no inserts and no deletes.</b> Which rarities a set contains is the catalog's
/// fact, written by the ETL and by the set-loading page; this slice only says what they are worth.
/// So every row it writes is one that already existed in the named set, and a payload naming
/// anything else is refused by <see cref="UpdateRarityBySetValidator"/> rather than creating a
/// rarity the set does not have.
/// </para>
///
/// <para>
/// <b>The stamp moves in the same transaction as the rows.</b> Split across two, a crash in
/// between leaves edited weights in the database with every browser still holding the old stamp --
/// permanently stale, with nothing to surface it. Bumping the one group is enough for both caching
/// layers: the browsers compare their stored rows against the stamp, and the server's own memory
/// cache builds its keys from it, so a bumped group misses its old entry rather than serving it.
/// </para>
///
/// <para>
/// The handler is the happy path only. The validator runs first and refuses everything that could
/// fail here, which is why the rows below are looked up without a not-found branch.
/// </para>
/// </summary>
public static class UpdateRarityBySet
{
    /// <summary>
    /// Ceiling on either value. A pull rate is packs of opening -- the hardest rarity in the
    /// catalog is in the high hundreds -- so anything past this is a typo or a paste, and the
    /// point of the bound is that such a number would sit at the rare end of every sort for good.
    /// </summary>
    public const int MaxOrder = 100_000;

    /// <param name="SetId">
    /// The set being edited. Every row in <see cref="Request.Weights"/> has to belong to it: the
    /// page edits one set at a time, and a payload spanning two of them is a form that has lost
    /// track of which set is on screen.
    /// </param>
    public record Request
    {
        public int SetId { get; init; }

        /// <summary>
        /// The rows as the form posted them back. It need not be all of the set's rarities -- a
        /// row left out is simply not written -- but the name-order check is made against the set
        /// as it will stand afterwards, untouched rows included.
        /// </summary>
        public IReadOnlyList<RarityWeightEdit> Weights { get; init; } = [];
    }

    /// <param name="Updated">
    /// How many rows were written, so the page can say "8 rarities saved" without re-reading.
    /// Every posted row counts, including one whose values did not change: what was asked for was
    /// that the row hold these values, and it does.
    /// </param>
    public record Response(int Updated);

    public static async Task<Response> Handler(
        Request request,
        TcgCatalogDbContext context,
        CancellationToken ct = default)
    {
        var edits = request.Weights.ToDictionary(weight => weight.Id);

        // Tracked: these rows are being written. Scoped to the set as well as to the ids, so that
        // a payload the validator has already checked cannot be re-used against another set by a
        // caller that skipped it.
        var rows = await context.RarityBySetFilterOptions
            .Where(rarity => rarity.SetId == request.SetId && edits.Keys.Contains(rarity.Id))
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            var edit = edits[row.Id];

            // Null is written through rather than skipped: an emptied field means "this rarity is
            // unweighted again", which is a different state from the value that was there.
            row.PullRateRarityOrder = edit.PullRateRarityOrder;
            row.NameRarityOrder = edit.NameRarityOrder;
        }

        // The rows and the stamp, or neither. On the in-memory provider used by the tests this is
        // a no-op that logs a warning -- there is no transaction to take -- which costs the tests
        // the atomicity assertion and nothing else.
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        await context.SaveChangesAsync(ct);

        // Bumps and saves on the same context, inside the same transaction.
        await BumpStamps.Handler(
            new BumpStamps.Request { Groups = [FilterCacheGroup.RarityBySet] },
            context,
            ct);

        await transaction.CommitAsync(ct);

        return new Response(rows.Count);
    }
}
