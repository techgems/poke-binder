namespace PokeBinder.TcgCatalog.DbContext.Entities;

public class GenerationFilterOption
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<PokemonFilterOption> PokemonFilterOptions { get; set; } = [];
}
