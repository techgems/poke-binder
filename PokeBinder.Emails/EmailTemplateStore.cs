using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;

namespace PokeBinder.Emails;

/// <summary>
/// Reads email templates out of PokeBinder.db's <c>emailTemplates</c> table, by the name the
/// code knows each email by. Not cached: a template saved from the editor is what the next email
/// uses.
/// </summary>
public sealed class EmailTemplateStore(BinderDbContext db)
{
    /// <summary>The template stored under <paramref name="name"/>, or null when there is none.</summary>
    public async Task<EmailTemplate?> GetAsync(string name, CancellationToken ct) =>
        await db.EmailTemplates
            .AsNoTracking()
            .Where(t => t.Name == name)
            .Select(t => new EmailTemplate(t.Subject, t.Html, t.Text))
            .SingleOrDefaultAsync(ct);
}
