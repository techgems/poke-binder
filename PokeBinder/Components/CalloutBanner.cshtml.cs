using TechGems.StaticComponents;

namespace PokeBinder.Components;

public class CalloutBanner : StaticComponent
{
    public static readonly string ActionSlot = "action";

    public string Title { get; set; } = "";

    public string? Description { get; set; }
}
