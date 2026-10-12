namespace PokeBinder.Features.Binder.Shared.Arrangement;

/// <summary>
/// One pocket of the binder after a write that laid it out again -- a reorder, a resize, a set
/// added: which card it holds now, and whether that card's pocket is flagged missing -- the flag travels with its card, since a reserved pocket holds a
/// real card id and sorts like any other.
/// </summary>
/// <param name="IndexInBinder">The pocket, counted from zero across the whole binder.</param>
public record ReorderedCard(int IndexInBinder, int CardId, bool IsMissing);
