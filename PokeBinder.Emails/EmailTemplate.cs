namespace PokeBinder.Emails;

/// <summary>
/// The Liquid source of one email, as whoever edits templates wrote it. Each part is rendered
/// against the same model.
/// </summary>
/// <param name="Subject">Rendered as plain text, so nothing in it is HTML-encoded.</param>
/// <param name="Html">Rendered HTML-encoded: a value prints escaped unless the template asks for
/// <c>| raw</c>.</param>
/// <param name="Text">The plain-text alternative, or null for an email that sends HTML only.</param>
public sealed record EmailTemplate(string Subject, string Html, string? Text);
