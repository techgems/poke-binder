using TechGems.StaticComponents;

namespace PokeBinder.Pages.Components.Binder;

public enum BinderIconKind
{
    Plus,
    Grid,
    Search,
    Share,
    Undo,
    Redo,
    Trash,
}

public class BinderIcon : StaticComponent
{
    public BinderIconKind Icon { get; set; }

    public string Size { get; set; } = "h-5 w-5";
}
