using System;
using System.Collections.Generic;
using System.Text;

namespace PokeBinder.Features.CardSearch.GetSearchStarterFilters.Models;

public class SetsFilter
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SeriesId { get; set; }

    /// <summary>The set's image, as stored. Null for a set nobody has given one yet.</summary>
    public string? ImageUrl { get; set; }
}
