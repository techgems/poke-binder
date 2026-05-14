using TechGems.StaticComponents;

namespace PokeBinder.Pages.Components.Binder;

public class BinderToolbarButton : StaticComponent
{
    public string Label { get; set; } = "";

    public bool Destructive { get; set; } = false;

    public bool Disabled { get; set; } = false;
}
