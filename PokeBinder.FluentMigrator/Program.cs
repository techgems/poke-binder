using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PokeBinder.FluentMigrator;

public class Program
{
    public static void Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var applicationConnString = configuration.GetConnectionString("Application")!;
        var tcgCatalogConnString = configuration.GetConnectionString("TcgCatalog")!;

        EnsureDataDirectoryExists(applicationConnString);
        EnsureDataDirectoryExists(tcgCatalogConnString);

        Console.WriteLine("Running Application database migrations...");
        RunMigrations(applicationConnString, DatabaseTags.Application);
        Console.WriteLine("Application database migrations complete.");

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

    private static bool IsTcgCatalogPopulated(string connectionString)
    {
        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'VersionInfo' AND name NOT LIKE 'sqlite_%';";
            var tableCount = Convert.ToInt64(command.ExecuteScalar());

            return tableCount > 0;
        }
        catch
        {
            return false;
        }
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
