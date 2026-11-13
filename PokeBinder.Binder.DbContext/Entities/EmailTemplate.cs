namespace PokeBinder.Binders.DbContext.Entities;

/// <summary>
/// One kind of email the app sends, as the Liquid source that is edited at runtime.
/// PokeBinder.Emails renders it; this is only the stored row.
/// </summary>
public class EmailTemplate
{
    public int Id { get; set; }

    /// <summary>What the code asks for, e.g. "sign-in". Unique, and the same on every database.</summary>
    public string Name { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Html { get; set; } = string.Empty;

    /// <summary>The plain-text alternative, or null for an email that goes out as HTML only.</summary>
    public string? Text { get; set; }

    /// <summary>When the template was last saved, in Unix seconds.</summary>
    public long UpdatedAt { get; set; }
}
