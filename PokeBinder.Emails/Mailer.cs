namespace PokeBinder.Emails;

/// <summary>
/// Sends one kind of email: loads its template, renders it against the model and hands the result
/// to whichever <see cref="IEmailSender"/> is registered. What the app calls; the pieces behind it
/// are not.
/// </summary>
public sealed class Mailer(EmailTemplateStore templates, EmailRenderer renderer, IEmailSender sender)
{
    /// <exception cref="EmailException">
    /// The template is missing or broken, or the sender refused the email. Nothing was sent.
    /// </exception>
    public async Task SendAsync<TModel>(string to, TModel model, CancellationToken ct)
        where TModel : class, IEmailModel
    {
        var template = await templates.GetAsync(TModel.TemplateName, ct)
            ?? throw new EmailTemplateMissingException(TModel.TemplateName);

        var email = await renderer.RenderAsync(template, model);
        await sender.SendAsync(to, email, ct);
    }
}
