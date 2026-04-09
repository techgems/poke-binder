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
            { CompleteCardType: var cardName } when cardName.Contains("Energy") => "Energy",
            { CompleteCardType: var cardName } when cardName.Contains("Item") || cardName.Contains("Supporter") || cardName.Contains("Stadium") || cardName.Contains("Tool") => "Trainer",
            { HP: var hp, Stage: var stage } when hp.HasValue && stage is not null => "Pokemon",
        };

        return dbCardType;
    }

    public List<ModernPokemonCSV> ExcludeSealedProduct(List<ModernPokemonCSV> list)
    {
        var cardTypeList =
            list
            .Where(card => !string.IsNullOrEmpty(card.Rarity) && card.Rarity != "Code Card")
            .ToList();

        return cardTypeList;
    }

    //TODO: IMPLEMENT AN UPSERT TO ADD CARD TEXT DATA
    public List<PokemonCardText> MapCsvCardsToPokemonCardTextList(List<ModernPokemonCSV> list, int setId, string setName)
    {
        var cardTextList =
            list
            .Where(card => MapCsvCardTypeToDbCardType(card) == "Pokemon")
            .Select(card => {
                var c = new PokemonCardText()
                {
                    HP = card.HP.HasValue ? card.HP.Value.ToString() : null,
                    Stage = card.Stage
                };
                return c;
            }).ToList();

        return cardTextList;
    }

    public CardUpsertGroupedLists MapCsvCardsToDbUpsertLists(List<ModernPokemonCSV> list, int setId, string setName)
    {
        var setDirectoryName = setName.Replace(":", " -");
        var directorySet = Path.Combine(_baseImageDirectory, setDirectoryName);

        var fullUpsertObject = new CardUpsertGroupedLists();

        foreach (var card in list)
        {
            var imageUrl = FindExistingImagePath(card.TcgPlayerId, directorySet, directorySet);

            var cardType = MapCsvCardTypeToDbCardType(card);
            var cardSubtype = cardType == _unknownTag ? _unknownTag : card.CompleteCardType!;

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

            

            fullUpsertObject.CardEntities.Add(c);

            if(cardType == "Trainer" || cardType == "Energy" && card.CardName.Contains("Basic"))
            {
                var nonPkmnCardText = new NonPokemonCardText()
                {
                    Text = card.NonPokemonCardText
                };
                fullUpsertObject.NonPkmnCardTextEntities.Add(nonPkmnCardText);
                continue;
            }

            if(cardType == "Pokemon")
            {
                var pokemonCardText = new PokemonCardText()
                {
                    HP = card.HP.HasValue ? card.HP.Value.ToString() : null,
                    Stage = card.Stage
                };

                fullUpsertObject.PkmnCardTextEntities.Add(pokemonCardText);
                continue;
            }
        }

        return fullUpsertObject;
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
