using Microsoft.Extensions.Configuration;
using PokeBinder.ETL.CsvLoader;
using PokeBinder.ETL.CsvLoader.Models;
using PokeBinder.ETL.TcgPlayer;
using PokeBinder.TcgCatalog.DbContext.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokeBinder.ETL.Db.Repositories;

public class CardCsvUtilsService
{
    private readonly string _baseImageDirectory = string.Empty;

    /// <summary>
    /// Use in fields that shouldn't be null in the database but we don't have a value for yet. 
    /// </summary>
    private readonly string _unknownTag = "UNKNOWN";

    public CardCsvUtilsService(IConfiguration _configuration)
    {
        _baseImageDirectory = _configuration["ImagesDirectory"]!;
    }

    public List<ModernPokemonCSV> MapCsvFilesToSingleCardList(List<string> csvFiles, string baseDirectory)
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

    private string MapCsvCardTypeToDbCardType(ModernPokemonCSV csvCard)
    {
        var dbCardType = "";

        if (string.IsNullOrEmpty(csvCard.CompleteCardType))
            return _unknownTag;

        dbCardType = csvCard switch
        {
            { CompleteCardType: var cardType } when cardType.Contains("Energy") => "Energy",
            { CompleteCardType: var cardType } when cardType.Contains("Item") || cardType.Contains("Supporter") || cardType.Contains("Stadium") || cardType.Contains("Tool") || cardType.Contains("Trainer") => "Trainer",
            { HP: var hp, Stage: var stage } when hp.GetValueOrDefault() != 0 && stage is not null => "Pokemon",
            _ => _unknownTag
        };

        return dbCardType;
    }

    private string MapSubType(string mappedCardType, string csvFullType) 
    { 
        if(mappedCardType == _unknownTag)
            return _unknownTag;

        return csvFullType;
    }

    public List<ModernPokemonCSV> ExcludeSealedProduct(List<ModernPokemonCSV> list)
    {
        var cardTypeList =
            list
            .Where(card => !string.IsNullOrEmpty(card.Rarity) && card.Rarity != "Code Card")
            .ToList();

        return cardTypeList;
    }


    public List<Card> MapCsvCardsToDbUpsertLists(List<ModernPokemonCSV> list, int setId, string setName)
    {
        var setDirectoryName = setName.Replace(":", " -");
        var directorySet = Path.Combine(_baseImageDirectory, setDirectoryName);

        var cardList = new List<Card>();

        foreach (var card in list)
        {
            var imageUrl = FindExistingImagePath(card.TcgPlayerId, directorySet, directorySet);

            var cardType = MapCsvCardTypeToDbCardType(card);
            var cardSubtype = MapSubType(cardType, card.CompleteCardType!);

            var c = new Card()
            {
                SetId = setId,
                CardNumber = card.CardNumber,
                Name = card.CardName,
                Rarity = card.Rarity,
                CardType = cardType,
                CardSubtype = cardSubtype,
                TcgPlayerId = card.TcgPlayerId,
                ImageUrl = imageUrl
            };

            cardList.Add(c);
        }

        return cardList;
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
}
