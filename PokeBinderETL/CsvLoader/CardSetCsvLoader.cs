using PokeBinder.ETL.CsvLoader.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeBinder.ETL.CsvLoader;

public static class CardSetCsvLoader
{
    public static List<ModernPokemonCSV> ProcessSetCSV(string fileLocation)
    {
        try
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
            };


            using var reader = new StreamReader(fileLocation);
            using var csv = new CsvReader(reader, config);

            csv.Context.RegisterClassMap<ModernPokemonSetsCsvMapper>();

            var deserializedRecords = csv.GetRecords<ModernPokemonCSV>();

            return deserializedRecords.ToList();
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error processing CSV file: {ex.Message}");
            throw;
        }
    }
}
