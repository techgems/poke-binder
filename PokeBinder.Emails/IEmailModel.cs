namespace PokeBinder.Emails;

/// <summary>
/// The model of one kind of email, and the name of the template it renders with. The template can
/// reach this record's public properties and nothing else, so it holds exactly what the email may
/// print -- never an entity.
/// </summary>
public interface IEmailModel
{
    /// <summary>The <c>emailTemplates.name</c> this email renders with.</summary>
    static abstract string TemplateName { get; }
}
