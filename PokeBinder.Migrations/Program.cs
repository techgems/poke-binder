using System.Reflection;
using DbUp;
using DbUp.Engine;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace PokeBinder.Migrations;

public class Program
{
    public static int Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var tcgCatalogConnString = configuration.GetConnectionString("TcgCatalog")!;
        var applicationConnString = configuration.GetConnectionString("Application")!;

        EnsureDatabaseDirectoryExists(tcgCatalogConnString);
        EnsureDatabaseDirectoryExists(applicationConnString);

        Console.WriteLine("Running TCG Catalog database migrations...");
        var tcgResult = RunMigrations(tcgCatalogConnString, "PokeBinder.Migrations.Scripts.TcgCatalog.");

        if (!tcgResult)
            return -1;

        Console.WriteLine("TCG Catalog database migrations complete.");

        Console.WriteLine("Running PokeBinder database migrations...");
        var appResult = RunMigrations(applicationConnString, "PokeBinder.Migrations.Scripts.PokeBinder.");

        if (!appResult)
            return -1;

        Console.WriteLine("PokeBinder database migrations complete.");
        return 0;
    }

    private static bool RunMigrations(string connectionString, string scriptPrefix)
    {
        var upgrader = DeployChanges.To
            .SqliteDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), script => script.StartsWith(scriptPrefix))
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(result.Error);
            Console.ResetColor();
            return false;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Success!");
        Console.ResetColor();
        return true;
    }

    private static void EnsureDatabaseDirectoryExists(string connectionString)
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
