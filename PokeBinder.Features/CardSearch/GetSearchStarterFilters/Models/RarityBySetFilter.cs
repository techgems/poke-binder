using System;
using System.Collections.Generic;
using System.Text;

namespace PokeBinder.Features.CardSearch.GetSearchStarterFilters.Models;

public class RarityBySetFilter
{
    public int Id { get; set; }

    public int SetId { get; set; }

    public string Rarity { get; set; } = string.Empty;

    /// <summary>
    /// Roughly how many packs of this set it takes before you are likely to have pulled this
    /// rarity -- bigger is harder, so bigger is rarer, in the same unit in every set. Null is a
    /// rarity nobody has weighted yet, which is most of them until /admin/setRarity has been
    /// through the catalog.
    /// </summary>
    public int? PullRateRarityOrder { get; set; }

    /// <summary>
    /// A ranking by what the rarity is called rather than by how hard it is to pull, so a sort can
    /// keep the same name together across sets. Numbered the same way round as
    /// <see cref="PullRateRarityOrder"/>; null is unweighted.
    /// </summary>
    public int? NameRarityOrder { get; set; }
}
