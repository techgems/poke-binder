using TechGems.StaticComponents;

namespace PokeBinder.Components.Binder;

/// <summary>
/// A dialog driven entirely by Alpine: no JavaScript of its own, no server round trip to open. The
/// content between the tags is the dialog body, so the page decides what is inside while the
/// component owns the backdrop, the panel, the heading and the ways out (close button, backdrop
/// click, Escape).
/// </summary>
public class BinderModal : StaticComponent
{
    /// <summary>
    /// The Alpine expression that says whether the dialog is open -- a property on an x-data scope
    /// the caller owns, e.g. "newBinderOpen". It is read and written, so it must be assignable.
    /// </summary>
    public string OpenState { get; set; } = "open";

    public string Title { get; set; } = string.Empty;

    /// <summary>Optional line under the title.</summary>
    public string? Subtitle { get; set; }
}
