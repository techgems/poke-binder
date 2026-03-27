using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace PokeBinder.TcgCatalog.DataAccess;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTcgCatalogDataAccess(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<TcgCatalogDbContext>(options =>
            options.UseSqlite(connectionString));

        return services;
    }
}
