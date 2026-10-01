namespace PokeBinder.Features.Binder.ReorderBinder.Models;

/// <summary>
/// One pocket of the binder after a reorder: which card it holds now, and whether that card's
/// pocket is flagged missing -- the flag travels with its card, since a reserved pocket holds a
/// real card id and sorts like any other.
/// </summary>
/// <param name="IndexInBinder">The pocket, counted from zero across the whole binder.</param>
public record ReorderedCard(int IndexInBinder, int CardId, bool IsMissing);
