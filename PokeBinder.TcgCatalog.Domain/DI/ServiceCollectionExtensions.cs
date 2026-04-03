using Microsoft.Extensions.DependencyInjection;
using PokeBinder.TcgCatalog.Domain.Services;

namespace PokeBinder.TcgCatalog.Domain.DI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTcgCatalogDomain(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<FilterOptionService>();

        return services;
    }
}
