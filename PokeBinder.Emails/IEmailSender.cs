namespace PokeBinder.Emails;

/// <summary>
/// Delivers a rendered email through one provider -- Bird.com for now, SES in production. The
/// SES implementation lives in PokeBinder.AWS. Nothing about the provider reaches the templates,
/// so a new provider is a new implementation of this and nothing else.
/// </summary>
public interface IEmailSender
{
    /// <param name="to">The recipient's address.</param>
    /// <param name="email">The rendered email. The sender's own configuration decides the From address.</param>
    Task SendAsync(string to, RenderedEmail email, CancellationToken ct);
}
