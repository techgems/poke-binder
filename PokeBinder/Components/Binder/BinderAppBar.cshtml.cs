using TechGems.StaticComponents;

namespace PokeBinder.Components.Binder;

/// <summary>
/// The bar across the top of the binder area. It is Skeleton's AppBar written out as the markup
/// that component renders -- the styling comes from the data-scope/data-part attributes, so it
/// needs no JavaScript -- with the two ends left to the caller.
/// </summary>
public class BinderAppBar : StaticComponent
{
    /// <summary>The wordmark or logo at the lead edge. Falls back to the PokeBinder wordmark.</summary>
    public static readonly string BrandSlot = "brand";

    /// <summary>Actions and links at the trail edge: navigation, account, whatever the page needs.</summary>
    public static readonly string OptionsSlot = "options";

    /// <summary>
    /// Centred between the two slots -- what this screen is, e.g. "Binder Builder". Left out, the
    /// middle column simply stays empty and the two ends keep their positions.
    /// </summary>
    public string? Title { get; set; }
}
