//#define IS_DATA_LOAD
//#define IS_IMAGE_DOWNLOAD
#define IS_VALIDATE

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

            builder.Services.AddHttpClient("TcgPlayerCDN", (serviceProvider, client) =>
            {
                client.BaseAddress = new Uri("https://tcgplayer-cdn.tcgplayer.com/");
            });

            //https://tcgplayer-cdn.tcgplayer.com/product/{tcgPlayerId}_in_1000x1000.jpg

            builder.Services.AddTcgCatalogDataAccess(Configuration["Sqlite"]!);

            builder.Services.AddScoped<TcgPlayerImgDownloadService>();
            builder.Services.AddScoped<CardUpsertService>();
            builder.Services.AddScoped<FilterOptionUpsertService>();
            builder.Services.AddScoped<CardCsvUtilsService>();
            builder.Services.AddScoped<CardSetValidationService>();

            using IHost host = builder.Build();

            using var serviceScope = host.Services.CreateScope();
            IServiceProvider provider = serviceScope.ServiceProvider;

            var tcgPlayerImgDownloadService = provider.GetRequiredService<TcgPlayerImgDownloadService>();
            var cardUpsertService = provider.GetRequiredService<CardUpsertService>();
            var filterOptionSeedRepository = provider.GetRequiredService<FilterOptionUpsertService>();

#if IS_DATA_LOAD

            var originalSeriesConfig = Configuration.GetSection("OriginalSeries").Get<GenerationConfig>()!;
            var neoSeriesConfig = Configuration.GetSection("NeoSeries").Get<GenerationConfig>()!;
            var legendaryCollectionConfig = Configuration.GetSection("LegendaryCollection").Get<GenerationConfig>()!;
            var eCardConfig = Configuration.GetSection("eCard").Get<GenerationConfig>()!;
            var exSeriesConfig = Configuration.GetSection("EXSeries").Get<GenerationConfig>()!;
            var diamondAndPearlConfig = Configuration.GetSection("DiamondAndPearl").Get<GenerationConfig>()!;
            var platinumConfig = Configuration.GetSection("Platinum").Get<GenerationConfig>()!;
            var heartGoldSoulSilverConfig = Configuration.GetSection("HeartGoldSoulSilver").Get<GenerationConfig>()!;
            var blackAndWhiteConfig = Configuration.GetSection("BlackAndWhite").Get<GenerationConfig>()!;
            var xAndYConfig = Configuration.GetSection("XY").Get<GenerationConfig>()!;
            var sunAndMoonConfig = Configuration.GetSection("SunAndMoon").Get<GenerationConfig>()!;
            var swordAndShieldConfig = Configuration.GetSection("SwordAndShield").Get<GenerationConfig>()!;
            var scarletAndVioletConfig = Configuration.GetSection("ScarletAndViolet").Get<GenerationConfig>()!;
            var megaEvolutionsConfig = Configuration.GetSection("MegaEvolution").Get<GenerationConfig>()!;
            
            cardUpsertService.InsertGames();

            var pokemonEnglishGameId = 1;

            await cardUpsertService.AddGenerationAndSetsFromConfig(originalSeriesConfig, "Original Series", "original-series", pokemonEnglishGameId);
            await cardUpsertService.AddGenerationAndSetsFromConfig(neoSeriesConfig, "Neo Series", "neo-series", pokemonEnglishGameId);
            await cardUpsertService.AddGenerationAndSetsFromConfig(legendaryCollectionConfig, "Legendary Collection", "legendary-collection", pokemonEnglishGameId);
            await cardUpsertService.AddGenerationAndSetsFromConfig(eCardConfig, "e-Card Series", "e-card-series", pokemonEnglishGameId);
            await cardUpsertService.AddGenerationAndSetsFromConfig(exSeriesConfig, "EX Series", "ex-series", pokemonEnglishGameId);
            await cardUpsertService.AddGenerationAndSetsFromConfig(diamondAndPearlConfig, "Diamond & Pearl", "diamond-and-pearl", pokemonEnglishGameId);
            await cardUpsertService.AddGenerationAndSetsFromConfig(platinumConfig, "Platinum", "platinum", pokemonEnglishGameId);
            await cardUpsertService.AddGenerationAndSetsFromConfig(heartGoldSoulSilverConfig, "HeartGold & SoulSilver", "heartgold-soulsilver", pokemonEnglishGameId);
            await cardUpsertService.AddGenerationAndSetsFromConfig(blackAndWhiteConfig, "Black & White", "black-and-white", pokemonEnglishGameId);
            await cardUpsertService.AddGenerationAndSetsFromConfig(xAndYConfig, "X & Y", "xy", pokemonEnglishGameId);
            await cardUpsertService.AddGenerationAndSetsFromConfig(sunAndMoonConfig, "Sun & Moon", "sun-and-moon", pokemonEnglishGameId);
            await cardUpsertService.AddGenerationAndSetsFromConfig(swordAndShieldConfig, "Sword & Shield", "sword-and-shield", pokemonEnglishGameId);
            await cardUpsertService.AddGenerationAndSetsFromConfig(scarletAndVioletConfig, "Scarlet & Violet", "scarlet-violet", pokemonEnglishGameId);
            await cardUpsertService.AddGenerationAndSetsFromConfig(megaEvolutionsConfig, "Mega Evolution", "mega-evolution", pokemonEnglishGameId);

            filterOptionSeedRepository.SeedAll();

#endif

#if IS_IMAGE_DOWNLOAD
            await cardRepository.SyncImagesForAllCards();
#endif

#if IS_VALIDATE
            var cardSetValidationService = provider.GetRequiredService<CardSetValidationService>();
            var validationLogDirectory = Path.Combine(AppContext.BaseDirectory, "ValidationLogs");
            cardSetValidationService.ValidateAndReport(validationLogDirectory);
#endif
            //await host.StartAsync();
        }
    }
}