namespace PokeBinder.Features.Binder.Shared.Arrangement;

/// <summary>
/// One pocket of the binder after a write that laid it out again -- a reorder, a resize, a set
/// added: which card it holds now, and whether that card's pocket is flagged missing, and the
/// labels that followed the card. The missing flag and the labels travel with their card, since a
/// reserved pocket holds a real card id and sorts like any other and a chip is tied to the card
/// the collector put it on.
/// </summary>
/// <param name="IndexInBinder">The pocket, counted from zero across the whole binder.</param>
/// <param name="LabelIds">
/// The labels on this pocket, in chip-strip order (0, 1, 2). Zero to three ids. Empty when the card
/// has no labels, or when it is one of a set's cards on its way in -- new cards start unlabelled,
/// as substep-1 and -4's "copy does not carry it" rule says.
/// </param>
public record ReorderedCard(int IndexInBinder, int CardId, bool IsMissing, IReadOnlyList<int> LabelIds)
{
    // The record's auto-generated equality uses EqualityComparer<T>.Default on each field, and the
    // default for IReadOnlyList<int> is reference equality -- two separately-built empty lists do
    // not compare equal. Overriding here keeps `first.Cards.Equals(second.Cards)` working the way
    // every test and every "nothing changed" check expects.
    public virtual bool Equals(ReorderedCard? other) =>
        other is not null
            && IndexInBinder == other.IndexInBinder
            && CardId == other.CardId
            && IsMissing == other.IsMissing
            && LabelIds.SequenceEqual(other.LabelIds);

    public override int GetHashCode() =>
        HashCode.Combine(IndexInBinder, CardId, IsMissing, LabelIds.Count);
}
