using Microsoft.EntityFrameworkCore;
using PokeBinder.TcgCatalog.DbContext;
using System.Text;

namespace PokeBinder.ETL.Db.Repositories;

public class CardSetValidationService
{
    private const string PokemonCardType = "Pokemon";
    private const string EnergyCardType = "Energy";
    private const string UnknownTag = "UNKNOWN";

    private readonly TcgCatalogDbContext _dbContext;

    public CardSetValidationService(TcgCatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private record FlaggedCard(
        int SetId,
        string SetName,
        int CardId,
        int TcgPlayerId,
        string? CardName,
        string? CardNumber,
        string Reason);

    public void ValidateAndReport(string outputDirectory)
    {
        var flags = new List<FlaggedCard>();

        flags.AddRange(FindAllCardsWithUnknownType());
        flags.AddRange(FindPokemonCardsMissingHpOrStage());
        flags.AddRange(FindAllCardsWithUnknownSubType());
        //flags.AddRange(FindCardsMissingArtist());
        flags.AddRange(FindTrainerCardsWithEmptyText());

        WriteReport(flags, outputDirectory);
    }

    private List<FlaggedCard> FindPokemonCardsMissingHpOrStage()
    {
        var pokemonCards = _dbContext.Cards
            .AsNoTracking()
            .Where(c => c.CardType == PokemonCardType)
            .Select(c => new
            {
                c.Id,
                c.TcgPlayerId,
                c.Name,
                c.CardNumber,
                SetId = c.SetId ?? 0,
                SetName = c.Set != null ? c.Set.Name : "(unassigned)",
                HasText = c.PokemonCardText != null,
                HP = c.PokemonCardText != null ? (int?)c.PokemonCardText.HP : null,
                Stage = c.PokemonCardText != null ? c.PokemonCardText.Stage : null
            })
            .ToList();

        var flagged = new List<FlaggedCard>();

        foreach (var card in pokemonCards)
        {
            var missingHp = !card.HasText || card.HP is null or 0;
            var missingStage = !card.HasText || string.IsNullOrWhiteSpace(card.Stage);

            if (!missingHp && !missingStage)
                continue;

            string reason;
            if (!card.HasText)
                reason = "Missing PokemonCardText (HP and Stage unavailable)";
            else if (missingHp && missingStage)
                reason = "Missing HP and Stage";
            else if (missingHp)
                reason = "Missing HP";
            else
                reason = "Missing Stage";

            flagged.Add(new FlaggedCard(
                card.SetId,
                card.SetName,
                card.Id,
                card.TcgPlayerId,
                card.Name,
                card.CardNumber,
                reason));
        }

        return flagged;
    }

    private List<FlaggedCard> FindAllCardsWithUnknownType()
    {
        var flagged = _dbContext.Cards
            .AsNoTracking()
            .Where(c => c.CardType == UnknownTag)
            .Select(c => new FlaggedCard(
                c.SetId ?? 0,
                c.Set!.Name,
                c.Id,
                c.TcgPlayerId,
                c.Name,
                c.CardNumber,
                "CardType is UNKNOWN"))
            .ToList();
        return flagged;
    }

    private List<FlaggedCard> FindAllCardsWithUnknownSubType()
    {
        var flagged = _dbContext.Cards
            .AsNoTracking()
            .Where(c => c.CardType == UnknownTag)
            .Where(c => c.CardType == PokemonCardType
                        && (string.IsNullOrEmpty(c.CardSubtype) || c.CardSubtype == UnknownTag))
            .Select(c => new FlaggedCard(
                c.SetId ?? 0,
                c.Set!.Name,
                c.Id,
                c.TcgPlayerId,
                c.Name,
                c.CardNumber,
                string.IsNullOrEmpty(c.CardSubtype)
                    ? "Missing CardSubtype"
                    : "CardSubtype is UNKNOWN"))
            .ToList();

        return flagged;
    }

    private List<FlaggedCard> FindCardsMissingArtist()
    {
        var flagged = _dbContext.Cards
            .AsNoTracking()
            .Where(c => string.IsNullOrEmpty(c.Artist))
            .Select(c => new FlaggedCard(
                c.SetId ?? 0,
                c.Set != null ? c.Set.Name : "(unassigned)",
                c.Id,
                c.TcgPlayerId,
                c.Name,
                c.CardNumber,
                "Missing Artist"))
            .ToList();

        return flagged;
    }

    private List<FlaggedCard> FindTrainerCardsWithEmptyText()
    {
        var nonPokemonCards = _dbContext.Cards
            .AsNoTracking()
            .Where(c => c.CardType != PokemonCardType && c.CardType != EnergyCardType)
            .Select(c => new
            {
                c.Id,
                c.TcgPlayerId,
                c.Name,
                c.CardNumber,
                SetId = c.SetId ?? 0,
                SetName = c.Set != null ? c.Set.Name : "(unassigned)",
                HasText = c.NonPokemonCardText != null,
                Text = c.NonPokemonCardText != null ? c.NonPokemonCardText.Text : null
            })
            .ToList();

        var flagged = new List<FlaggedCard>();

        foreach (var card in nonPokemonCards)
        {
            if (card.HasText && !string.IsNullOrWhiteSpace(card.Text))
                continue;

            var reason = card.HasText
                ? "NonPokemonCardText.Text is null or whitespace"
                : "Missing NonPokemonCardText";

            flagged.Add(new FlaggedCard(
                card.SetId,
                card.SetName,
                card.Id,
                card.TcgPlayerId,
                card.Name,
                card.CardNumber,
                reason));
        }

        return flagged;
    }

    private void WriteReport(List<FlaggedCard> flags, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var filePath = Path.Combine(outputDirectory, $"card_set_validation_{timestamp}.log");

        var report = new StringBuilder();
        report.AppendLine($"Card Set Validation Report - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"Total flagged entries: {flags.Count}");
        report.AppendLine(new string('=', 80));
        report.AppendLine();

        if (flags.Count == 0)
        {
            report.AppendLine("No data integrity issues found.");
            File.WriteAllText(filePath, report.ToString());
            Console.WriteLine($"Validation report written to: {filePath}");
            return;
        }

        var bySet = flags
            .GroupBy(f => new { f.SetId, f.SetName })
            .OrderBy(g => g.Key.SetName);

        foreach (var setGroup in bySet)
        {
            var setFlags = setGroup.ToList();
            var uniqueCards = setFlags.Select(f => f.CardId).Distinct().Count();

            report.AppendLine($"=== Set: {setGroup.Key.SetName} (Id: {setGroup.Key.SetId}) ===");
            report.AppendLine($"Flagged entries: {setFlags.Count} across {uniqueCards} distinct card(s).");

            var byReason = setFlags
                .GroupBy(f => f.Reason)
                .OrderBy(g => g.Key);

            foreach (var reasonGroup in byReason)
            {
                report.AppendLine($"  [{reasonGroup.Key}] - {reasonGroup.Count()} card(s):");
                foreach (var card in reasonGroup.OrderBy(c => c.CardNumber).ThenBy(c => c.CardName))
                {
                    report.AppendLine(
                        $"    - {card.CardName ?? "(no name)"} " +
                        $"#{card.CardNumber ?? "?"} " +
                        $"[CardId: {card.CardId}, TcgPlayerId: {card.TcgPlayerId}]");
                }
            }

            report.AppendLine();
        }

        File.WriteAllText(filePath, report.ToString());
        Console.WriteLine($"Validation report written to: {filePath}");
    }
}
