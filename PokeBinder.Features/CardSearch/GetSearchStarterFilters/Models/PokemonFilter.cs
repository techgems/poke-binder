using System;
using System.Collections.Generic;
using System.Text;

namespace PokeBinder.Features.CardSearch.GetSearchStarterFilters.Models;

public class PokemonFilter
{
    public int Id { get; set; }

    public int PokedexNumber { get; set; }

    public string Name { get; set; } = string.Empty;

    public int GenerationId { get; set; }

    public string? AlternateName { get; set; }
}
