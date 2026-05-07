namespace PokeBinder.Binders.DbContext.Entities;

public class UserLogin
{
    public string LoginProvider { get; set; } = string.Empty;

    public string ProviderKey { get; set; } = string.Empty;

    public string? ProviderDisplayName { get; set; }

    public int UserId { get; set; }
}
