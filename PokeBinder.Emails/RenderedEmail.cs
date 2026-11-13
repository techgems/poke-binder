namespace PokeBinder.Emails;

/// <summary>An email ready to hand to a sender: every part rendered, nothing left to fill in.</summary>
public sealed record RenderedEmail(string Subject, string Html, string? Text);
