using Microsoft.Extensions.DependencyInjection;

namespace PokeBinder.TcgCatalog.Domain.DI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTcgCatalogDomain(this IServiceCollection services)
    {
        services.AddMemoryCache();

        return services;
    }
}
