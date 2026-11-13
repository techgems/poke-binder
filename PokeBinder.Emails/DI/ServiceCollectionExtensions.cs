using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PokeBinder.Emails.Bird;

namespace PokeBinder.Emails.DI;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// The <see cref="Mailer"/>, and the renderer and template store behind it. The store reads
    /// <c>BinderDbContext</c>, so the binder data access must be registered too. No
    /// <see cref="IEmailSender"/>: register the provider's alongside it.
    /// </summary>
    public static IServiceCollection AddEmails(this IServiceCollection services)
    {
        // Holds the per-model Fluid options, which are built once per model type.
        services.AddSingleton<EmailRenderer>();
        services.AddScoped<EmailTemplateStore>();
        services.AddScoped<Mailer>();

        return services;
    }

    /// <summary>
    /// Bird as the <see cref="IEmailSender"/>, configured from the <c>Bird</c> section. The key is
    /// checked when the app starts, so a missing one stops the app there rather than at the first
    /// email. <c>Bird:FromMail</c> is not: the sending domain may not be set up yet, and a send
    /// without it throws <see cref="EmailSendException"/> instead.
    /// </summary>
    public static IServiceCollection AddBirdEmailSender(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BirdOptions>()
            .Bind(configuration.GetSection(BirdOptions.Section))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "Bird:ApiKey is not set.")
            .Validate(o => string.IsNullOrWhiteSpace(o.ApiKey) || o.Region is not null,
                "Bird:ApiKey is not a Bird API key: it should start bk_{region}_.")
            .ValidateOnStart();

        services.AddHttpClient<IEmailSender, BirdEmailSender>((provider, client) =>
        {
            var bird = provider.GetRequiredService<IOptions<BirdOptions>>().Value;
            client.BaseAddress = bird.BaseAddress;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bird.ApiKey);
        });

        return services;
    }
}
