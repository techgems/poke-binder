namespace PokeBinder.Features.Utils;

/// <summary>
/// Builds the LIKE patterns the card searches match names and numbers with.
/// <para>
/// Searching goes through LIKE rather than <c>string.Contains</c> or <c>==</c>, which SQLite
/// translates to a case-sensitive <c>instr</c> and <c>=</c>: a search box that missed "charizard"
/// because the catalog stores "Charizard" would be broken. LIKE is case-insensitive for ASCII,
/// which covers card numbers entirely and card names except for their accented letters.
/// </para>
/// </summary>
public static class SqlLiteLikePatterns
{
    /// <summary>
    /// The escape character every pattern here is built with. SQLite has no default one, so it has
    /// to be named on the LIKE itself:
    /// <c>EF.Functions.Like(card.Name, pattern, LikePatterns.EscapeCharacter)</c>.
    /// </summary>
    public const string EscapeCharacter = "\\";

    /// <summary>
    /// A pattern matching <paramref name="term"/> anywhere in the value.
    /// </summary>
    public static string Contains(string term) => $"%{Escape(term)}%";

    /// <summary>
    /// A pattern matching <paramref name="term"/> and nothing else — the whole value, not part of
    /// it. Only worth using over <c>==</c> for the case-insensitive comparison LIKE brings.
    /// </summary>
    public static string Exact(string term) => Escape(term);

    /// <summary>
    /// Neutralizes the LIKE wildcards in a typed term. Without this, a card number holding an
    /// underscore would match any character in its place, and a lone "%" in a name box would match
    /// the entire catalog.
    /// </summary>
    private static string Escape(string term) => term
        .Replace(EscapeCharacter, EscapeCharacter + EscapeCharacter)
        .Replace("%", EscapeCharacter + "%")
        .Replace("_", EscapeCharacter + "_");
}
