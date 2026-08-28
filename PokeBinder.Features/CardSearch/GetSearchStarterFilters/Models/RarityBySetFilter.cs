using System;
using System.Collections.Generic;
using System.Text;

namespace PokeBinder.Features.CardSearch.GetSearchStarterFilters.Models;

public class RarityBySetFilter
{
    public int Id { get; set; }

    public int SetId { get; set; }

    public string Rarity { get; set; } = string.Empty;
}
