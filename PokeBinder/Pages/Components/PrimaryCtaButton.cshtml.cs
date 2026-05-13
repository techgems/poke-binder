using TechGems.StaticComponents;

namespace PokeBinder.Pages.Components;

public enum CtaVariant
{
    Primary,
    Outline,
    White,
}

public class PrimaryCtaButton : StaticComponent
{
    public string? Page { get; set; }

    public string ButtonType { get; set; } = "submit";

    public bool FullWidth { get; set; } = false;

    public CtaVariant Variant { get; set; } = CtaVariant.Primary;
}
