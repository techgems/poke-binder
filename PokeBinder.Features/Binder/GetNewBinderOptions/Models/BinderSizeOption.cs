namespace PokeBinder.Features.Binder.GetNewBinderOptions.Models;

/// <summary>
/// One grid offered on the create-a-binder form. The page count is not chosen here -- it is a
/// number the user can overwrite -- so this carries the recommendation rather than a rule.
/// </summary>
public class BinderSizeOption
{
    public int Id { get; set; }

    /// <summary>What the grid is called, e.g. "3x3".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>What a page of it holds, in words, e.g. "9 cards per page".</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Pockets on one page: the grid's x * y.</summary>
    public int CardsPerPage { get; set; }

    /// <summary>The page count this grid is usually sold with; prefills the form's page field.</summary>
    public int DefaultPages { get; set; }
}
