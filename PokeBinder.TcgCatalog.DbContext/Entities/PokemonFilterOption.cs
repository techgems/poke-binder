namespace PokeBinder.TcgCatalog.DbContext.Entities;

public class PokemonFilterOption
{
    public int Id { get; set; }

    public int PokedexNumber { get; set; }

    public string Name { get; set; } = string.Empty;

    public int GenerationId { get; set; }

    public string? AlternateName { get; set; }

    public GenerationFilterOption Generation { get; set; } = null!;
}
