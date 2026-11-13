namespace PokeBinder.Emails;

/// <summary>
/// Any reason an email did not go out: no template, a broken template, or a sender that would not
/// take it. Callers that only need to know the email failed catch this one.
/// </summary>
public abstract class EmailException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>No row in <c>emailTemplates</c> carries the name a message asked for.</summary>
public sealed class EmailTemplateMissingException(string name)
    : EmailException($"There is no email template named '{name}'. Scripts/SeedEmailTemplates.sql adds the ones the app sends.")
{
    public string Name { get; } = name;
}
