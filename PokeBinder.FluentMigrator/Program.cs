using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PokeBinder.FluentMigrator;

public class Program
{
    //TODO: FLUENT MIGRATOR is cool, but it has limited support for SQLite.
    //IN THE FUTURE THE IDEA IS TO MOVE TO DbUp instead, which has allows migrations in SQL, which will have better support for SQLite.
    public static void Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var tcgCatalogConnString = configuration.GetConnectionString("TcgCatalog")!;

        EnsureDataDirectoryExists(tcgCatalogConnString);

        Console.WriteLine();

        Console.WriteLine("Running TCG Catalog database migrations...");
        RunMigrations(tcgCatalogConnString, DatabaseTags.TcgCatalog);
        Console.WriteLine("TCG Catalog database migrations complete.");
        
    }

    private static void RunMigrations(string connectionString, string tag)
    {
        var services = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(runner => runner
                .AddSQLite()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(Program).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .Configure<global::FluentMigrator.Runner.Initialization.RunnerOptions>(opts =>
            {
                opts.Tags = [tag];
            })
            .BuildServiceProvider(false);

        using var scope = services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
    }

    private static void EnsureDataDirectoryExists(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dbPath = builder.DataSource;

        if (string.IsNullOrEmpty(dbPath))
            return;

        var directory = Path.GetDirectoryName(Path.GetFullPath(dbPath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
