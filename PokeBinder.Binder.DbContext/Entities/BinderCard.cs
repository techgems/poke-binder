namespace PokeBinder.Binders.DbContext.Entities;

public class BinderCard
{
    public int BinderId { get; set; }

    public int CardId { get; set; }

    public int IndexInBinder { get; set; }

    public bool? IsMissing { get; set; }

    public Binder? Binder { get; set; }
}
