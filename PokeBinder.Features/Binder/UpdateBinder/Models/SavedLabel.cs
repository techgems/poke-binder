namespace PokeBinder.Features.Binder.UpdateBinder.Models;

/// <summary>
/// One label on the binder as the Settings tab's Save-button sync carries it. The id is always
/// real: a new label is created through <c>CreateBinderCardLabel</c> before it can appear in this
/// list, so the sync is only ever rename, recolor, or delete-by-omission.
/// </summary>
public class SavedLabel
{
    /// <summary>The server's id for the label. The validator rejects an id not on this binder.</summary>
    public int Id { get; set; }

    /// <summary>The tag. Trimmed by the handler; the validator refuses a blank one or one past
    /// the cap on <see cref="PokeBinder.Features.Binder.CreateBinderCardLabel.CreateBinderCardLabel.MaxNameLength"/>.</summary>
    public string? Name { get; set; }

    /// <summary>One of the eight allowed names.</summary>
    public string? Color { get; set; }
}
