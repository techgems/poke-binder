using System;
using System.Collections.Generic;
using System.Text;

namespace PokeBinder.TcgCatalog.DbContext.Entities;

public class Game
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<Series> Generations { get; set; } = [];
}