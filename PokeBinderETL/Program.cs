#define IS_DATA_LOAD
//#define IS_IMAGE_DOWNLOAD

using PokeBinder.ETL.Config;
using PokeBinder.ETL.CsvLoader;
using PokeBinder.ETL.Db;
using PokeBinder.ETL.Db.Repositories;
using PokeBinder.ETL.TcgPlayer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PokeBinder.TcgCatalog.DbContext.DI;

namespace PokeBinder.ETL
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var environmentVariable = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentVariable}.json", true, true)
                .AddCommandLine(args);

            var isDevelopment = string.IsNullOrEmpty(environmentVariable) || environmentVariable.ToLower() == "development";

            if (isDevelopment) //only add secrets in development
            {
                configurationBuilder.AddUserSecrets<Program>();
            }

            IConfiguration Configuration = configurationBuilder.Build();

            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
            builder.Configuration.AddConfiguration(Configuration);


            //Register HttpClients
            builder.Services.AddHttpClient("JustTcg", (serviceProvider, client) =>
            {
                var apiKey = Configuration["JustTCG:ApiKey"];
                var url = Configuration["JustTCG:Url"];

                client.DefaultRequestHeaders.Add("x-api-key", apiKey);
                client.BaseAddress = new Uri(url!);
            });

            builder.Services.AddHttpClient("TcgPlayerCDN", (serviceProvider, client) =>
            {
                client.BaseAddress = new Uri("https://tcgplayer-cdn.tcgplayer.com/");
            });

            //https://tcgplayer-cdn.tcgplayer.com/product/{tcgPlayerId}_in_1000x1000.jpg

            builder.Services.AddTcgCatalogDataAccess(Configuration["Sqlite"]!);

            builder.Services.AddScoped<TcgPlayerImgDownloadService>();
            builder.Services.AddScoped<CardUpsertService>();
            builder.Services.AddScoped<FilterOptionUpsertService>();

            using IHost host = builder.Build();

            using var serviceScope = host.Services.CreateScope();
            IServiceProvider provider = serviceScope.ServiceProvider;

            var tcgPlayerImgDownloadService = provider.GetRequiredService<TcgPlayerImgDownloadService>();
            var cardRepository = provider.GetRequiredService<CardUpsertService>();
            var filterOptionSeedRepository = provider.GetRequiredService<FilterOptionUpsertService>();


#if IS_DATA_LOAD

            //TODO: ETL NOW HAS A NEW JOB THAT IT DIDN'T HAVE BEFORE, WHICH IS TO ADD SEED DATA FOR FILTER OPTION TABLES. THIS INCLUDES THINGS LIKE CARD TYPES, RARITIES, SETS, ETC.
            //THESE CURRENTLY RESIDE IN THE FLUENT MIGRATOR PROJECT, BUT THE WAY THEY CREATE THE DATA IS FUZZY, BY USING A SQLITE SEQUENCE, WHEN NONE IS REQUIRED.

            var smSetDateAndFileList = Configuration.GetSection("SunAndMoon").Get<GenerationConfig>()!;
            var swSetDateAndFileList = Configuration.GetSection("SwordAndShield").Get<GenerationConfig>()!;
            var svSetDataAndFileList = Configuration.GetSection("ScarletAndViolet").Get<GenerationConfig>()!;
            var meSetDateAndFileList = Configuration.GetSection("MegaEvolution").Get<GenerationConfig>()!;
            
            cardRepository.InsertGames();

            var pokemonEnglishGameId = 1;
            
            
            await cardRepository.AddGenerationAndSetsFromConfig(smSetDateAndFileList, "Sun & Moon", "sun-and-moon", pokemonEnglishGameId);
            await cardRepository.AddGenerationAndSetsFromConfig(swSetDateAndFileList, "Sword & Shield", "sword-and-shield", pokemonEnglishGameId);
            await cardRepository.AddGenerationAndSetsFromConfig(svSetDataAndFileList, "Scarlet & Violet", "scarlet-violet", pokemonEnglishGameId);
            await cardRepository.AddGenerationAndSetsFromConfig(meSetDateAndFileList, "Mega Evolution", "mega-evolution", pokemonEnglishGameId);

            filterOptionSeedRepository.SeedAll();

#endif

#if IS_IMAGE_DOWNLOAD
            await cardRepository.SyncImagesForAllCards();
#endif
            //await host.StartAsync();
        }
    }
}