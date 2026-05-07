namespace PokeBinder.Binders.DbContext.Entities;

public class UserToken
{
    public int UserId { get; set; }

    public string LoginProvider { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Value { get; set; }
}
