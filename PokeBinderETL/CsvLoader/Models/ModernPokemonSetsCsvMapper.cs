using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeBinder.ETL.CsvLoader.Models;

public class ModernPokemonCSV()
{
    public int TcgPlayerId { get; set; }
    public string CardName { get; set; }
    public string CardNumber { get; set; }
    public string Rarity { get; set; }
    public string SubType { get; set; }
    public string CardType { get; set; }
    public string HP { get; set; }
};

public class ModernPokemonSetsCsvMapper : CsvHelper.Configuration.ClassMap<ModernPokemonCSV>
{
    public ModernPokemonSetsCsvMapper()
    {
        Map(c => c.TcgPlayerId).Name("productId");
        Map(c => c.CardName).Name("name");
        Map(c => c.CardNumber).Name("extNumber");
        Map(c => c.Rarity).Name("extRarity");
        Map(c => c.SubType).Name("subTypeName");
        Map(c => c.HP).Name("extHP");
        Map(c => c.CardType).Name("extCardType");
    }
}