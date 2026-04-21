namespace PokeBinder.Binders.DbContext.Entities;

public class Binder
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public long CreatedAt { get; set; }

    public int UserId { get; set; }

    public int BinderSizeId { get; set; }

    public BinderSize? BinderSize { get; set; }

    public ICollection<BinderCard> Cards { get; set; } = new List<BinderCard>();

    public ICollection<BinderTray> Tray { get; set; } = new List<BinderTray>();
}
