namespace PokeBinder.TcgCatalog.DomainModels.Responses;

public class SearchFiltersResponse
{
    public List<GenerationFilterOptionItem> Generations { get; set; } = [];

    public List<PokemonFilterOptionItem> Pokemon { get; set; } = [];

    public List<RarityBySetFilterOptionItem> RaritiesBySet { get; set; } = [];

    public List<CardTypeFilterOptionItem> CardTypes { get; set; } = [];
}

public class GenerationFilterOptionItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class PokemonFilterOptionItem
{
    public int Id { get; set; }

    public int PokedexNumber { get; set; }

    public string Name { get; set; } = string.Empty;

    public int GenerationId { get; set; }

    public string? AlternateName { get; set; }
}

public class RarityBySetFilterOptionItem
{
    public int Id { get; set; }

    public int SetId { get; set; }

    public string Rarity { get; set; } = string.Empty;
}

public class CardTypeFilterOptionItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
