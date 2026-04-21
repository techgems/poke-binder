namespace PokeBinder.Binders.DbContext.Entities;

public class BinderTray
{
    public int BinderId { get; set; }

    public int CardId { get; set; }

    public int Quantity { get; set; }

    public Binder? Binder { get; set; }
}
