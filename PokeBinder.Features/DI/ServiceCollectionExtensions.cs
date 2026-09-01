using Microsoft.Extensions.DependencyInjection;
using PokeBinder.Binders.DbContext.DI;
using PokeBinder.TcgCatalog.DbContext.DI;

namespace PokeBinder.Features.DI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFeatures(
        this IServiceCollection services,
        string binderConnectionString,
        string tcgCatalogConnectionString)
    {
        services.AddBinderDataAccess(binderConnectionString);
        services.AddTcgCatalogDataAccess(tcgCatalogConnectionString);
        services.AddMemoryCache();

        return services;
    }
}
