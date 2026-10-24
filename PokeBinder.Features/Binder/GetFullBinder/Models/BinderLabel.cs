namespace PokeBinder.Features.Binder.GetFullBinder.Models;

/// <summary>
/// One label the binder owns, as the preload carries it. The client lands these in its labels map
/// at load, keyed by id; the ids on <see cref="BinderPlacement.LabelIds"/> point back at this list.
/// </summary>
public class BinderLabel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;
}
