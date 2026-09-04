using Microsoft.EntityFrameworkCore;
using PokeBinder.Features.CardImages;
using PokeBinder.Features.CardSearch.SearchCardWithSimpleSearch.Models;
using PokeBinder.TcgCatalog.DbContext;
using PokeBinder.TcgCatalog.DbContext.Entities;
using System.Linq.Expressions;

namespace PokeBinder.Features.CardSearch.SearchCardWithSimpleSearch;

/// <summary>
/// The simple search: two typed terms instead of the seven multi-select lists the filtered search
/// takes. A card name that is matched anywhere in the name, and a card number that has to match
/// exactly.
/// </summary>
public static class SearchCardWithSimpleSearch
{
    /// <summary>Page size used when the caller does not ask for one.</summary>
    public const int DefaultPageSize = 50;

    /// <summary>Ceiling on page size, so one request can never ask for the whole catalog.</summary>
    public const int MaxPageSize = 200;

    /// <summary>
    /// Escape character for the LIKE patterns below. SQLite has no default one, so it is declared
    /// on every LIKE this slice builds.
    /// </summary>
    private const string LikeEscape = "\\";

    /// <summary>
    /// Both terms are optional and independent. Supplying both narrows to cards that satisfy the
    /// two of them, which is how a user finds one printing of a name that spans many sets.
    /// </summary>
    public record Request
    {
        /// <summary>
        /// Matched anywhere in the card's name, case-insensitively. Blank or whitespace means the
        /// user typed nothing, so the name is not looked at.
        /// </summary>
        public string? CardName { get; init; }

        /// <summary>
        /// Matched against the whole card number, not part of it: numbers are short and repeat
        /// across sets, so a contains match on "12" would return most of the catalog. This is the
        /// number as printed on the card ("4", "SV49", "TG12"), not the card's id.
        /// </summary>
        public string? CardNumber { get; init; }

        /// <summary>1-based page to return. Anything below 1 is treated as the first page.</summary>
        public int PageNumber { get; init; } = 1;

        /// <summary>Rows per page, clamped to <see cref="MaxPageSize"/>.</summary>
        public int PageSize { get; init; } = DefaultPageSize;
    }

    public record Response(
        IReadOnlyList<CardSearchResult> Results,
        int PageNumber,
        int PageSize,
        bool HasMore);

    public static async Task<Response> Handler(
        Request request,
        TcgCatalogDbContext context,
        CardImageUrls imageUrls,
        CancellationToken ct = default)
    {
        // The terms arrive in the query string, where an omitted number binds as zero rather than
        // keeping the default above, so zero has to mean "not asked for" instead of clamping up to
        // a one-row page.
        var pageSize = request.PageSize <= 0
            ? DefaultPageSize
            : Math.Min(request.PageSize, MaxPageSize);

        var pageNumber = Math.Max(request.PageNumber, 1);

        var cardName = Normalize(request.CardName);
        var cardNumber = Normalize(request.CardNumber);

        // With both fields empty there is nothing to search for. The filtered search answers the
        // same situation with page one of the whole catalog, because there every filter left empty
        // is a deliberate "any", and its first screen is meant to be browsable. Two empty text
        // boxes are not a request to browse, so they cost no query at all.
        if (cardName is null && cardNumber is null)
        {
            return new Response([], pageNumber, pageSize, false);
        }

        // Guard the offset arithmetic: an absurd page number would otherwise overflow into a
        // negative Skip rather than an empty page.
        var skip = (int)Math.Min((long)(pageNumber - 1) * pageSize, int.MaxValue);

        var rows = await ApplyTerms(context.Cards, cardName, cardNumber)
            // Paging needs a total order, otherwise SQLite is free to return a row on two
            // different pages. A name search is the case that repeats most — every printing of
            // one pokemon shares a name — so the unique id breaks the ties.
            .OrderBy(card => card.Name)
            .ThenBy(card => card.Id)
            .Select(MapResult)
            .Skip(skip)
            // Reading one row past the page is what tells us a next page exists, without paying
            // for a second query to count the matches.
            .Take(pageSize + 1)
            .ToListAsync(ct);

        var hasMore = rows.Count > pageSize;

        if (hasMore)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        // The card stores a local file path, so it becomes a URL once the rows are back rather than
        // inside the query — string surgery in SQL would buy nothing here.
        foreach (var row in rows)
        {
            row.ImageUrl = imageUrls.ToPublicUrl(row.ImageUrl);
        }

        return new Response(rows, pageNumber, pageSize, hasMore);
    }

    /// <summary>
    /// Trims a typed term and turns "the user left this box alone" into null, so the handler has
    /// one thing to test rather than three (null, empty, spaces).
    /// </summary>
    private static string? Normalize(string? term) =>
        string.IsNullOrWhiteSpace(term) ? null : term.Trim();

    /// <summary>
    /// Adds whichever terms were actually typed. Both go through LIKE rather than through
    /// <c>string.Contains</c> or <c>==</c>: those translate to <c>instr</c> and <c>=</c>, which are
    /// case-sensitive in SQLite, and a search box that misses "charizard" because the catalog
    /// stores "Charizard" is broken. LIKE is case-insensitive for ASCII, which covers card numbers
    /// entirely and card names except for their accented letters.
    /// </summary>
    private static IQueryable<Card> ApplyTerms(
        IQueryable<Card> cards,
        string? cardName,
        string? cardNumber)
    {
        if (cardName is not null)
        {
            var pattern = $"%{EscapeLikeWildcards(cardName)}%";

            cards = cards.Where(card => EF.Functions.Like(card.Name, pattern, LikeEscape));
        }

        // No wildcards in this pattern, so the LIKE is an exact match on the whole value; it is
        // only a LIKE at all to get the case-insensitive comparison described above.
        if (cardNumber is not null)
        {
            var pattern = EscapeLikeWildcards(cardNumber);

            cards = cards.Where(card => EF.Functions.Like(card.CardNumber, pattern, LikeEscape));
        }

        return cards;
    }

    /// <summary>
    /// Neutralizes the LIKE wildcards in a typed term. Without this, a card number containing an
    /// underscore would match any single character in its place, and a lone "%" in the name box
    /// would match the entire catalog.
    /// </summary>
    private static string EscapeLikeWildcards(string term) => term
        .Replace(LikeEscape, LikeEscape + LikeEscape)
        .Replace("%", LikeEscape + "%")
        .Replace("_", LikeEscape + "_");

    private static readonly Expression<Func<Card, CardSearchResult>> MapResult =
        card => new CardSearchResult()
        {
            Id = card.Id,
            Name = card.Name,
            Rarity = card.Rarity,
            CardNumber = card.CardNumber,
            TcgPlayerId = card.TcgPlayerId,
            ImageUrl = card.ImageUrl,
            SetName = card.Set != null ? card.Set.Name : null,
        };
}
