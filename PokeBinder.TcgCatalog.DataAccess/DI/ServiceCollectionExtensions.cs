using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PokeBinder.TcgCatalog.DataAccess.Repositories;
using PokeBinder.TcgCatalog.DbContext;

namespace PokeBinder.TcgCatalog.DataAccess.DI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTcgCatalogDataAccess(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<TcgCatalogDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<CardRepository>();
        services.AddScoped<SetRepository>();
        services.AddScoped<SeriesRepository>();
        services.AddScoped<GenerationFilterOptionRepository>();
        services.AddScoped<PokemonFilterOptionRepository>();
        services.AddScoped<RarityBySetFilterOptionRepository>();
        services.AddScoped<CardTypeFilterOptionRepository>();

        return services;
    }
}
