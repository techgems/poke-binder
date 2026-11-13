namespace PokeBinder.Emails;

/// <summary>
/// A template that cannot be parsed, or that asks for a variable or filter its model and Fluid do
/// not have. Templates are edited at runtime, so this is an editing mistake, not a bug in the app.
/// </summary>
public sealed class EmailTemplateException(string part, string error, Exception? inner = null)
    : EmailException($"The email template's {part} is invalid: {error}", inner)
{
    /// <summary>Which part of the template failed: subject, html or text.</summary>
    public string Part { get; } = part;

    public string Error { get; } = error;
}
