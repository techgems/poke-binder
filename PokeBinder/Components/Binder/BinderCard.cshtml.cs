using TechGems.StaticComponents;

namespace PokeBinder.Components.Binder;

/// <summary>
/// One binder on the "your binders" list: enough to recognise it and open it. The card never shows
/// the cards inside -- GetBinderList counts them rather than listing them -- so everything here is
/// a scalar the page already has.
/// </summary>
public class BinderCard : StaticComponent
{
    public int BinderId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>The grid's name, e.g. "3x3".</summary>
    public string SizeName { get; set; } = string.Empty;

    public int Pages { get; set; }

    /// <summary>Cards placed in the binder, including the ones flagged as missing.</summary>
    public int CardsAdded { get; set; }

    /// <summary>Pockets in the whole binder.</summary>
    public int CardCount { get; set; }

    /// <summary>How many of <see cref="CardsAdded"/> are flagged as not yet owned.</summary>
    public int CardsMissing { get; set; }
}
