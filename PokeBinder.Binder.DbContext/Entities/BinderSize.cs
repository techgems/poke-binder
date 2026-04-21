namespace PokeBinder.Binders.DbContext.Entities;

public class BinderSize
{
    public int Id { get; set; }

    public int CardCount { get; set; }

    public string Description { get; set; } = string.Empty;

    public int X { get; set; }

    public int Y { get; set; }

    public int Pages { get; private set; }

    public ICollection<Binder> Binders { get; set; } = new List<Binder>();
}
