namespace PokeBinder.TcgCatalog.DbContext.Entities;

/// <summary>
/// One rarity a set contains, and what it is worth there. Which rarities a set has is the
/// catalog's own fact -- written by the ETL, and by the set-loading page -- while the two order
/// columns are a judgement entered from /admin/setRarity.
/// </summary>
public class RarityBySetFilterOption
{
    public int Id { get; set; }

    public int SetId { get; set; }

    public string Rarity { get; set; } = string.Empty;

    /// <summary>
    /// Roughly how many packs of this set it takes before you are likely to have pulled this
    /// rarity. Bigger is harder, so bigger is rarer, and the unit is the same in every set -- 540
    /// packs in one and 20 in another mean the same thing, which is what makes this comparable
    /// across sets instead of a rank within one.
    /// <para>
    /// Null is unweighted: nobody has said yet what this rarity is worth here.
    /// </para>
    /// </summary>
    public int? PullRateRarityOrder { get; set; }

    /// <summary>
    /// A ranking by what the rarity is <em>called</em>, deliberately not tied to probability, so
    /// that the same name lands near itself across sets and a sort can keep all the Special
    /// Illustration Rares together rather than scattering them by how generous each set was.
    /// <para>
    /// Numbered the same way round as <see cref="PullRateRarityOrder"/> -- Common low, Special
    /// Illustration Rare high -- so one "rare first" label can sit over either of them. Null is
    /// unweighted.
    /// </para>
    /// </summary>
    public int? NameRarityOrder { get; set; }

    public Set Set { get; set; } = null!;
}
