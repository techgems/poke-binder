namespace PokeBinder.Emails.Messages;

/// <summary>
/// The passwordless sign-in email. In a template: <c>{{ email }}</c>, <c>{{ signInLink }}</c>,
/// <c>{{ expiresInMinutes }}</c>.
/// </summary>
/// <param name="Email">The address the email goes to, which is also the account it signs in.</param>
/// <param name="SignInLink">The one-time link. Single use, and dead after <paramref name="ExpiresInMinutes"/>.</param>
/// <param name="ExpiresInMinutes">How long the link works for.</param>
public sealed record SignInEmail(string Email, string SignInLink, int ExpiresInMinutes) : IEmailModel
{
    public static string TemplateName => "sign-in";
}
