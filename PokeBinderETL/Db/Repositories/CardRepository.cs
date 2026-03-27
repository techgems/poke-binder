using PokeBinder.ETL.Config;
using PokeBinder.ETL.CsvLoader;
using PokeBinder.ETL.CsvLoader.Models;
using PokeBinder.ETL.Db.Entities;
using PokeBinder.ETL.TcgPlayer;
using PokeBinder.ETL.Utils;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

namespace PokeBinder.ETL.Db.Repositories;

public class CardSimpleModel
{

    public int TcgPlayerId { get; set; }
    public string CardName { get; set; } = string.Empty;
}

public class CardRepository
{
    private TcgPlayerImgDownloadService _tcgPlayerImgDownloadService;
    private CardDbContext _cardDbContext;

    public CardRepository(IConfiguration _configuration, TcgPlayerImgDownloadService tcgPlayerImgDownloadService, CardDbContext cardDbContext)
    {
        _tcgPlayerImgDownloadService = tcgPlayerImgDownloadService;
        _cardDbContext = cardDbContext;
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
        var directorySet = Path.Combine("ImagesTCG", setDirectoryName);
        var fullDirectoryPath = Path.Combine(@"C:\", directorySet);

        var cards = _cardDbContext.Cards.Where(x => x.SetId == setId).ToList();

        var cardsNeedingDownload = new List<CardSimpleModel>();
        var imagesToUpdate = new List<DownloadedImage>();

        foreach (var card in cards)
        {
            var existingFilePath = FindExistingImagePath(card.TcgPlayerId, directorySet, fullDirectoryPath);

            /*If told not to download images, ignore the commented code.*/
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

    public async Task<Result> AddGenerationAndSetsFromConfig(GenerationConfig generationConfig, string generationName, string generationSlug, int gameId)
    {
        long releaseDateUnix = generationConfig.ReleaseDate.ToUnixTimeSeconds();

        long? endDateUnix = generationConfig.EndDate?.ToUnixTimeSeconds();

        var gen = _cardDbContext.Generations.FirstOrDefault(x => x.Slug == generationSlug);

        int? genId = null;

        if (gen is null)
        {
            var dbGeneration = new Generation()
            {
                Slug = generationSlug,
                StartDateUnix = releaseDateUnix,
                EndDateUnix = endDateUnix.GetValueOrDefault(),
                GameId = gameId,
                Name = generationName
            };

            _cardDbContext.Generations.Add(dbGeneration);
            await _cardDbContext.SaveChangesAsync();

            gen = dbGeneration;
        }

        var insertedGenerationId = gen.Id;

        var allDownloadedImages = new List<DownloadedImage>();

        foreach (var set in generationConfig.Sets)
        {
            var dbSet = _cardDbContext.Sets.FirstOrDefault(x => x.Code == set.Code);
            if (dbSet is null) {

                dbSet = new Set()
                {
                    FullName = set.FullName,
                    Code = set.Code,
                    GenerationId = insertedGenerationId,
                    Name = set.DisplayName,
                    PriorityOrder = set.PriorityOrder.GetValueOrDefault(),
                    ReleaseDateUnix = set.ReleaseDate.ToUnixTimeSeconds()
                };

                _cardDbContext.Sets.Add(dbSet);
                await _cardDbContext.SaveChangesAsync();
            }

            var insertedSetId = dbSet.Id;

            var cardList = MapCsvFilesToSingleCardList(set.CsvFiles, generationConfig.BaseDirectory);

            var insertedIds = AddCardsFromCsvModel(cardList, insertedSetId);
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

    private List<int> AddCardsFromCsvModel(List<ModernPokemonCSV> list, int setId)
    {
        var insertedIds = new List<int>();

        foreach (var card in list)
        {
            var cardExists = _cardDbContext.Cards.Any(x => x.TcgPlayerId == card.TcgPlayerId);

            if (cardExists)
            {
                continue;
            }

            if (string.IsNullOrEmpty(card.Rarity) || card.Rarity == "Code Card")
            {
                continue;
            }

            var dbCard = new Card()
            {
                SetId = setId,
                CardNumber = card.CardNumber,
                Name = card.CardName,
                Rarity = card.Rarity,
                TcgPlayerId = card.TcgPlayerId
            };

            _cardDbContext.Cards.Add(dbCard);
            _cardDbContext.SaveChanges();
            insertedIds.Add(card.TcgPlayerId);
        }

        Console.WriteLine($"Set with Id: {setId} inserted!");

        return insertedIds;
    }
}
