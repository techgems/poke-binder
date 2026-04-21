using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace PokeBinder.Binders.DbContext.DI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBinderDataAccess(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<BinderDbContext>(options =>
            options.UseSqlite(connectionString));

        return services;
    }
}
