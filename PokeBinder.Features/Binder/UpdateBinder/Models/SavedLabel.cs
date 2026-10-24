namespace PokeBinder.Features.Binder.UpdateBinder.Models;

/// <summary>
/// One label on the binder as the Settings tab's Save-button sync carries it. The id is either
/// a positive server id (update) or null (insert for a label the Settings panel created locally
/// in this session). The sync is a reconciliation: anything in the DB that is not in the payload
/// is a delete-by-omission.
/// </summary>
public class SavedLabel
{
    /// <summary>
    /// The server's id for the label when it already exists on the binder, or null when the
    /// Settings panel has staged this as a new row for the server to create. The validator
    /// rejects a non-null id that is not on this binder.
    /// </summary>
    public int? Id { get; set; }

    /// <summary>The tag. Trimmed by the handler; the validator refuses a blank one or one past
    /// the cap on <see cref="PokeBinder.Features.Binder.CreateBinderCardLabel.CreateBinderCardLabel.MaxNameLength"/>.</summary>
    public string? Name { get; set; }

    /// <summary>One of the eight allowed names.</summary>
    public string? Color { get; set; }
}
