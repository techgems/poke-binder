using System;
using System.Collections.Generic;
using System.Text;

namespace PokeBinder.Features.CardSearch.GetSearchStarterFilters.Models;

/// <summary>
/// A card's super type (Pokemon, Trainer, Energy). There is no table behind this filter, so the
/// name doubles as the identifier.
/// </summary>
public class SuperTypeFilter
{
    public string Name { get; set; } = string.Empty;
}
