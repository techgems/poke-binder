using PokeBinder.ETL.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokeBinder.ETL.Db.Repositories;

public static class CardInsertErrors
{
    public static readonly Error GenerationExists = new("Generation.Exists", "Generation shouldn't exist when inserting one for the first time.");
}
