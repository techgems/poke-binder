using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PokeBinder.ETL.Config;
using PokeBinder.ETL.CsvLoader;
using PokeBinder.ETL.CsvLoader.Models;
using PokeBinder.ETL.TcgPlayer;
using PokeBinder.ETL.Utils;
using PokeBinder.TcgCatalog.DbContext;
using PokeBinder.TcgCatalog.DbContext.Entities;
using System.Collections.Concurrent;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PokeBinder.ETL.Db.Repositories;

public class CardSimpleModel
{

    public int TcgPlayerId { get; set; }
    public string CardName { get; set; } = string.Empty;
}

public class CardRepository
{
    private TcgPlayerImgDownloadService _tcgPlayerImgDownloadService;
    private TcgCatalogDbContext _cardDbContext;
    private string _baseImageDirectory = string.Empty;

    public CardRepository(IConfiguration _configuration, TcgPlayerImgDownloadService tcgPlayerImgDownloadService, TcgCatalogDbContext cardDbContext)
    {
        _tcgPlayerImgDownloadService = tcgPlayerImgDownloadService;
        _cardDbContext = cardDbContext;
        _baseImageDirectory = _configuration["ImagesDirectory"]!;
    }

    /// <summary>
    /// Incrementally syncs images for every set in the database.
    /// Scans the image folders on disk, downloads only missing images, and populates
    /// the ImageUrl field for every card that has an image on disk.
    /// </summary>
    public async Task SyncImagesForAllCards()
    {
        var setIds = GetAllSetIds();
        foreach (var setId in setIds)
        {
            await SyncImagesForSet(setId);
        }
    }

    /// <summary>
    /// Incrementally syncs images for a single set.
    /// 1. Scans the set's image folder to find which images already exist on disk.
    /// 2. Downloads only the images that are missing.
    /// 3. Updates the ImageUrl in the database for every card whose image is on disk
    ///    (both previously-existing and newly-downloaded), skipping cards that already
    ///    have their ImageUrl populated.
    /// </summary>
    private async Task SyncImagesForSet(int setId)
    {
        var set = _cardDbContext.Sets.Where(x => x.Id == setId).Select(x => new { x.Id, x.Name }).First();

        var setDirectoryName = set.Name.Replace(":", " -");
        var directorySet = Path.Combine(_baseImageDirectory, setDirectoryName);

        var cards = _cardDbContext.Cards.Where(x => x.SetId == setId).ToList();

        var cardsNeedingDownload = new List<CardSimpleModel>();
        var imagesToUpdate = new List<DownloadedImage>();

        foreach (var card in cards)
        {
            var existingFilePath = FindExistingImagePath(card.TcgPlayerId, directorySet, directorySet);

            if(existingFilePath is null)
            {
                if (card.HasImageDownloadAttempt)
                    continue;

                cardsNeedingDownload.Add(new CardSimpleModel { TcgPlayerId = card.TcgPlayerId, CardName = card.Name! });
                continue;
            }

            if (existingFilePath is not null && string.IsNullOrEmpty(card.ImageUrl))
            {
                imagesToUpdate.Add(new DownloadedImage(card.TcgPlayerId, existingFilePath));
            }
        }

        if (cardsNeedingDownload.Count > 0)
        {
            var newlyDownloaded = await _tcgPlayerImgDownloadService.DownloadImagesFromTcgPlayerIdList(
                new ConcurrentBag<CardSimpleModel>(cardsNeedingDownload),
                directorySet);

            imagesToUpdate.AddRange(newlyDownloaded);
        }

        if (imagesToUpdate.Count > 0)
        {
            UpdateCardImageUrls(imagesToUpdate);
        }

        Console.WriteLine($"Set '{set.Name}': {cards.Count} cards, {cardsNeedingDownload.Count} downloaded, {imagesToUpdate.Count} URLs updated.");
    }

    private string? FindExistingImagePath(int tcgPlayerId, string directorySet, string fullDirectoryPath)
    {
        var w1000FileName = $"{tcgPlayerId}{TcgPlayerImgDownloadService.W1000}";
        var w200FileName = $"{tcgPlayerId}{TcgPlayerImgDownloadService.W200}";

        if (File.Exists(Path.Combine(fullDirectoryPath, w1000FileName)))
            return Path.Combine(directorySet, w1000FileName);

        if (File.Exists(Path.Combine(fullDirectoryPath, w200FileName)))
            return Path.Combine(directorySet, w200FileName);

        return null;
    }

    private List<int> GetAllSetIds()
    {
        var sets = _cardDbContext.Sets.Select(x => x.Id).ToList();

        return sets;
    }

    public Result InsertGames() {         
        
        var gamesToInsert = new List<Game>()
        {
            new Game() { Name = "Pokemon TCG", Slug = "pokemon" },
            new Game() { Name = "Pokemon TCG: Japan", Slug = "pokemon-japan" }
        };

        _cardDbContext.Games.UpsertRange(gamesToInsert).On(g => g.Slug).Run();

        return Result.Success();
    }

    public async Task<Result> AddGenerationAndSetsFromConfig(GenerationConfig generationConfig, string generationName, string generationSlug, int gameId)
    {
        long releaseDateUnix = generationConfig.ReleaseDate.ToUnixTimeSeconds();

        long? endDateUnix = generationConfig.EndDate?.ToUnixTimeSeconds();

        var gen = new Generation()
        {
            Slug = generationSlug,
            StartDateUnix = releaseDateUnix,
            EndDateUnix = endDateUnix.GetValueOrDefault(),
            GameId = gameId,
            Name = generationName
        };

        var upsertedGen = _cardDbContext.Generations.Upsert(gen).On(g => g.Slug).RunAndReturn().First(); //Upsert doesn't require SaveChanges.

        var insertedGenerationId = upsertedGen.Id;

        var allDownloadedImages = new List<DownloadedImage>();

        var setsToInsert = generationConfig.Sets.Select(set => new Set()
        {
            FullName = set.FullName,
            Code = set.Code,
            GenerationId = insertedGenerationId,
            Name = set.DisplayName,
            PriorityOrder = set.PriorityOrder.GetValueOrDefault(),
            ReleaseDateUnix = set.ReleaseDate.ToUnixTimeSeconds()
        }).ToList();

        var upsertedSets = _cardDbContext.Sets.UpsertRange(setsToInsert).On(x => x.Code).RunAndReturn().ToList();

        foreach (var set in generationConfig.Sets)
        {
            var setFromDb = upsertedSets.First(x => x.Code == set.Code);

            var cardList = MapCsvFilesToSingleCardList(set.CsvFiles, generationConfig.BaseDirectory);

            AddCardsFromCsvModel(cardList, setFromDb.Id, setFromDb.Name);
        }

        return Result.Success();

    }

    private List<ModernPokemonCSV> MapCsvFilesToSingleCardList(List<string> csvFiles, string baseDirectory)
    {
        var cardList = csvFiles
            .SelectMany(csvFile =>
            {
                var fullPath = Path.IsPathRooted(csvFile)
                    ? csvFile
                    : Path.Combine(baseDirectory, csvFile);
                return CardSetCsvLoader.ProcessSetCSV(fullPath);
            })
            .ToList();

        return cardList;
    }

    private void UpdateCardImageUrls(IEnumerable<DownloadedImage> downloadedImages)
    {
        var updatedCount = 0;
        foreach (var download in downloadedImages)
        {
            var card = _cardDbContext.Cards.FirstOrDefault(x => x.TcgPlayerId == download.TcgPlayerId);
            if (card is null) continue;

            if (!download.attemptHasFailed)
            {
                card.ImageUrl = download.FilePath;
            }

            card.HasImageDownloadAttempt = true;
            _cardDbContext.Cards.Update(card);
            updatedCount++;
        }

        _cardDbContext.SaveChanges();
        Console.WriteLine($"Updated {updatedCount} card image URLs.");
    }

    private void AddCardsFromCsvModel(List<ModernPokemonCSV> list, int setId, string setName)
    {
        var setDirectoryName = setName.Replace(":", " -");
        var directorySet = Path.Combine(_baseImageDirectory, setDirectoryName);

        var cardList = 
            list
            .Where(card => !string.IsNullOrEmpty(card.Rarity) && card.Rarity != "Code Card")
            .Select(card => {
                var imageUrl = FindExistingImagePath(card.TcgPlayerId, directorySet, directorySet);

                var c = new Card()
                {
                    SetId = setId,
                    CardNumber = card.CardNumber,
                    Name = card.CardName,
                    Rarity = card.Rarity,
                    Stage = card.Stage,
                    TcgPlayerId = card.TcgPlayerId,
                    ImageUrl = imageUrl
                };

                return c;
            }).ToList();

        _cardDbContext.Cards.UpsertRange(cardList).On(x => x.TcgPlayerId).Run();

        Console.WriteLine($"Set with Id: {setId} inserted!");
    }
}
