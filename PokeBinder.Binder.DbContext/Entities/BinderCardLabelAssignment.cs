namespace PokeBinder.Binders.DbContext.Entities;

/// <summary>
/// One label on one card: the join row between <see cref="BinderCard"/> and
/// <see cref="BinderCardLabel"/>. <see cref="OrderIndex"/> is where in the chip strip the label
/// sits (0, 1, 2); the schema's CHECK caps it at two, which is where the three-labels-per-card
/// rule lives at the database level.
/// </summary>
public class BinderCardLabelAssignment
{
    /// <summary>Composite reference to <see cref="BinderCard.BinderId"/>.</summary>
    public int BinderId { get; set; }

    /// <summary>Composite reference to <see cref="BinderCard.IndexInBinder"/>.</summary>
    public int IndexInBinder { get; set; }

    public int LabelId { get; set; }

    /// <summary>Position in the pocket's chip strip, 0..2.</summary>
    public int OrderIndex { get; set; }

    public BinderCard? Card { get; set; }

    public BinderCardLabel? Label { get; set; }
}
