using System.Text.Json.Serialization;

namespace PokeBinder.TcgCatalog.DbContext.Entities;

public class PokemonCardText
{
    public int Id { get; set; }

    public int HP { get; set; }

    public string? Resistance { get; set; }

    public string? Weaknesses { get; set; }

    public string? FlavorText { get; set; }

    public string? Retreat { get; set; }

    public List<Attack>? AttackList { get; set; }

    public Ability? Ability { get; set; }

    public string? EvolvesFrom { get; set; }

    public string? DexNumber { get; set; }

    public string? Stage { get; set; }

    public int CardId { get; set; }

    public Card Card { get; set; } = null!;
}

public class Attack
{
    [JsonPropertyName("energyCost")]
    public string? EnergyCost { get; set; }

    [JsonPropertyName("attackText")]
    public string? AttackText { get; set; }

    [JsonPropertyName("attackValueText")]
    public string? AttackValueText { get; set; }
}

public class Ability
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
