using System.Text.Json.Serialization;

namespace PokeBinder.Features.Binder.ReorderBinder.Models;

/// <summary>What a criterion sorts by.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CriterionKey>))]
public enum CriterionKey
{
    /// <summary>The set's release date. Keeps every set's cards together.</summary>
    Set,

    /// <summary>The rarity's weight in the card's own set, by whichever <see cref="RarityKey"/> is chosen.</summary>
    Rarity,

    /// <summary>The card's name.</summary>
    Name,
}

/// <summary>
/// Which way the underlying value runs. The workspace labels these for what they do to each
/// criterion -- "newest first" is <see cref="Desc"/> on a release date, "rare first" is
/// <see cref="Desc"/> on a weight -- so the wire carries the direction and not the label.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SortDirection>))]
public enum SortDirection
{
    Asc,
    Desc,
}

/// <summary>
/// Which of the catalog's two rarity orderings the Rarity criterion reads. One or the other, never
/// both: they are one criterion with a choice of key, not two criteria competing for a slot.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<RarityKey>))]
public enum RarityKey
{
    /// <summary><c>pullRateRarityOrder</c>: roughly how many packs it takes to pull.</summary>
    PullRate,

    /// <summary><c>nameRarityOrder</c>: a ranking by what the rarity is called.</summary>
    Name,
}

/// <summary>One criterion of a reorder.</summary>
public record SortCriterion
{
    public CriterionKey Key { get; init; }

    public SortDirection Direction { get; init; }

    /// <summary>Required when <see cref="Key"/> is <see cref="CriterionKey.Rarity"/>, and refused otherwise.</summary>
    public RarityKey? RarityKey { get; init; }
}
