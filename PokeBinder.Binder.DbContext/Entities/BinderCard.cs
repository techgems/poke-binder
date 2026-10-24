namespace PokeBinder.Binders.DbContext.Entities;

public class BinderCard
{
    public int BinderId { get; set; }

    public int CardId { get; set; }

    public int IndexInBinder { get; set; }

    public bool? IsMissing { get; set; }

    public Binder? Binder { get; set; }

    /// <summary>
    /// Labels applied to this pocket: at most three rows (schema CHECK), ordered by
    /// <see cref="BinderCardLabelAssignment.OrderIndex"/>. Loaded by the save and get-full-binder
    /// paths; a pocket emptied through <c>SaveBinderChanges</c> deletes its <see cref="BinderCard"/>
    /// row and these cascade with it.
    /// </summary>
    public ICollection<BinderCardLabelAssignment> LabelAssignments { get; set; } =
        new List<BinderCardLabelAssignment>();
}
