using Microsoft.Extensions.DependencyInjection;
using PokeBinder.TcgCatalog.Domain.Services;

namespace PokeBinder.TcgCatalog.Domain;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTcgCatalogDomain(this IServiceCollection services)
    {
        services.AddScoped<CardSearchService>();

        return services;
    }
}
