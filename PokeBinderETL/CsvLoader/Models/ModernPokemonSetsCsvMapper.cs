using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeBinder.ETL.CsvLoader.Models;

public class ModernPokemonCSV()
{
    public int TcgPlayerId { get; set; }
    public string CardName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public string? Rarity { get; set; } = null;
    public string? CompleteCardType { get; set; } = null;
    public int? HP { get; set; } = null;
    public string? Stage { get; set; } = null;

    public string? NonPokemonCardText { get; set; } = null;
};

public class ModernPokemonSetsCsvMapper : CsvHelper.Configuration.ClassMap<ModernPokemonCSV>
{
    public ModernPokemonSetsCsvMapper()
    {
        Map(c => c.TcgPlayerId).Name("productId");
        Map(c => c.CardName).Name("name");
        Map(c => c.CardNumber).Name("extNumber");
        Map(c => c.Rarity).Name("extRarity");
        Map(c => c.HP).Name("extHP");
        Map(c => c.CompleteCardType).Name("extCardType");
        Map(c => c.Stage).Name("extStage");
        Map(c => c.NonPokemonCardText).Name("extCardText");
    }
}