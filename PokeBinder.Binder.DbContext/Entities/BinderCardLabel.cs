namespace PokeBinder.Binders.DbContext.Entities;

/// <summary>
/// One label the collector has created in a binder: a short name and one of eight colours,
/// applied to as many cards as they like through <see cref="BinderCardLabelAssignment"/>. Rename
/// and recolor propagate across every chip; deleting the label cascades to every assignment of
/// it.
/// </summary>
public class BinderCardLabel
{
    public int Id { get; set; }

    public int BinderId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public Binder? Binder { get; set; }

    /// <summary>
    /// The join rows for this label -- which cards it is on. Loaded by the slices that need the
    /// usage count or that strip assignments on a delete; the save path reads the other direction
    /// through <see cref="BinderCard.LabelAssignments"/>.
    /// </summary>
    public ICollection<BinderCardLabelAssignment> Assignments { get; set; } =
        new List<BinderCardLabelAssignment>();
}
