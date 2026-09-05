namespace PokeBinder.Binders.DbContext.Entities;

/// <summary>
/// One grid a binder can be built on: <see cref="X"/> pockets across a page by <see cref="Y"/>
/// pockets down it. A size does not say how long a binder is -- that is <see cref="Binder.Pages"/>,
/// because the same grid is sold in several lengths -- it only suggests a length through
/// <see cref="DefaultPages"/>.
/// </summary>
public class BinderSize
{
    public int Id { get; set; }

    /// <summary>What the grid is called, e.g. "3x3". The label the user picks from.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// What a page of this grid holds, in words, e.g. "9 cards per page" -- the blurb that sits
    /// under the name in a picker. Stored rather than built from <see cref="CardsPerPage"/> so the
    /// wording stays the seed's to choose.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Pockets across a page.</summary>
    public int X { get; set; }

    /// <summary>Pockets down a page.</summary>
    public int Y { get; set; }

    /// <summary>
    /// The page count binders of this grid are commonly sold with. It prefills the field when a
    /// binder is created and is a recommendation only: nothing stops a binder from having more or
    /// fewer pages than this.
    /// </summary>
    public int DefaultPages { get; set; }

    /// <summary>
    /// Pockets on one page. Derived and never stored -- it is <see cref="X"/> * <see cref="Y"/> and
    /// nothing else, so a column would only be a second copy to keep in sync.
    /// </summary>
    public int CardsPerPage => X * Y;

    /// <summary>
    /// What a binder of this grid holds at its recommended length. Useful for describing the size
    /// on the create form ("360 cards"); a real binder's capacity is <see cref="Binder.CardCount"/>.
    /// </summary>
    public int DefaultCardCount => CardsPerPage * DefaultPages;

    public ICollection<Binder> Binders { get; set; } = new List<Binder>();
}
