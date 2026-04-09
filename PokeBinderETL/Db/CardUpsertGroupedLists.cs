using PokeBinder.TcgCatalog.DbContext.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokeBinder.ETL.Db;

public class CardUpsertGroupedLists
{
    public List<Card> CardEntities { get; set; } = new List<Card>();

    public List<PokemonCardText> PkmnCardTextEntities { get; set; } = new List<PokemonCardText>();

    public List<NonPokemonCardText> NonPkmnCardTextEntities { get; set; } = new List<NonPokemonCardText>();
}
