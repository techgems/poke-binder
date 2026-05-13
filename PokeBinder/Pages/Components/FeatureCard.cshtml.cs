using TechGems.StaticComponents;

namespace PokeBinder.Pages.Components;

public enum FeatureCardColor
{
    Emerald,
    Sky,
    Fuchsia,
    Amber,
}

public class FeatureCard : StaticComponent
{
    public static readonly string IconSlot = "icon";

    public string Title { get; set; } = "";

    public string? Description { get; set; }

    public FeatureCardColor Color { get; set; } = FeatureCardColor.Emerald;
}
