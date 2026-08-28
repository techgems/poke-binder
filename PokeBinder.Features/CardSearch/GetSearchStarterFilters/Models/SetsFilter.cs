using System;
using System.Collections.Generic;
using System.Text;

namespace PokeBinder.Features.CardSearch.GetSearchStarterFilters.Models;

public class SetsFilter
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int GenerationId { get; set; }
}
