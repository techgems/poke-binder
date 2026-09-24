using Microsoft.EntityFrameworkCore;
using PokeBinder.TcgCatalog.DbContext;
using PokeBinder.TcgCatalog.DbContext.Entities;

namespace PokeBinder.Features.Tests.CardAdmin;

/// <summary>
/// A catalog with two sets that both contain the same rarity names, which is the situation the
/// weights exist for: "Special Illustration Rare" is not worth the same in both, and nothing about
/// the name says so.
///
/// <para>
/// The EF in-memory provider, never Sqlite, with a database name of its own per fixture -- see
/// "Rules for tests" in CLAUDE.md. The in-memory store has no transactions, so the explicit one
/// the write slice takes is a no-op here; that costs these tests the atomicity assertion and
/// nothing else, and the warning EF raises for it is ignored below rather than left to throw.
/// </para>
/// </summary>
public sealed class RarityWeightFixture : IDisposable
{
    public const int AscendedHeroesId = 20;

    public const int AnniversaryId = 21;

    /// <summary>A set with no rarity rows at all, for the "nothing to edit" case.</summary>
    public const int EmptySetId = 22;

    public RarityWeightFixture()
    {
        var options = new DbContextOptionsBuilder<TcgCatalogDbContext>()
            .UseInMemoryDatabase($"catalog-{Guid.NewGuid()}")
            .ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        Context = new TcgCatalogDbContext(options);
    }

    public TcgCatalogDbContext Context { get; }

    /// <summary>
    /// Two sets, their rarities, and a stamp for every group. Every rarity starts unweighted,
    /// which is the state the whole catalog is in until somebody has been through this page.
    /// </summary>
    public RarityWeightFixture WithCatalog()
    {
        Context.Games.Add(new Game { Id = 1, Slug = "pokemon", Name = "Pokemon" });
        Context.Series.Add(new Series { Id = 1, Slug = "mega-evolution", Name = "Mega Evolution", GameId = 1 });

        Context.Sets.Add(new Set
        {
            Id = AscendedHeroesId,
            Code = "ME-AH",
            Name = "ME: Ascended Heroes",
            FullName = "Mega Evolution: Ascended Heroes",
            SeriesId = 1,
            ReleaseDateUnix = 1_760_000_000,
        });

        Context.Sets.Add(new Set
        {
            Id = AnniversaryId,
            Code = "SV-30",
            Name = "30th Anniversary",
            FullName = "Scarlet & Violet: 30th Anniversary",
            SeriesId = 1,
            ReleaseDateUnix = 1_750_000_000,
        });

        Context.Sets.Add(new Set
        {
            Id = EmptySetId,
            Code = "NEW",
            Name = "Not Loaded Yet",
            FullName = "Not Loaded Yet",
            SeriesId = 1,
            ReleaseDateUnix = 1_770_000_000,
        });

        AddRarity(1, AscendedHeroesId, "Common");
        AddRarity(2, AscendedHeroesId, "Special Illustration Rare");
        AddRarity(3, AscendedHeroesId, "Mega Hyper Rare");
        AddRarity(4, AnniversaryId, "Common");
        AddRarity(5, AnniversaryId, "Special Illustration Rare");

        // Cards, so the editor's per-rarity counts have something to count. Two of the Ascended
        // Heroes rarities have cards and the third deliberately has none.
        Context.Cards.Add(NewCard(1, AscendedHeroesId, "Common"));
        Context.Cards.Add(NewCard(2, AscendedHeroesId, "Common"));
        Context.Cards.Add(NewCard(3, AscendedHeroesId, "Special Illustration Rare"));
        Context.Cards.Add(NewCard(4, AnniversaryId, "Common"));

        foreach (var group in Enum.GetValues<FilterCacheGroup>())
        {
            Context.FilterCacheStamps.Add(new FilterCacheStamp
            {
                Group = group,
                Stamp = $"seeded-{group}",
                LastBumpedUnix = 1_700_000_000,
            });
        }

        Context.SaveChanges();

        return this;
    }

    /// <summary>Weights already in the database, as a set somebody has been through would have.</summary>
    public RarityWeightFixture WithWeights(params (int Id, int? PullRate, int? NameOrder)[] weights)
    {
        foreach (var (id, pullRate, nameOrder) in weights)
        {
            var row = Context.RarityBySetFilterOptions.First(rarity => rarity.Id == id);

            row.PullRateRarityOrder = pullRate;
            row.NameRarityOrder = nameOrder;
        }

        Context.SaveChanges();

        return this;
    }

    public string StampFor(FilterCacheGroup group) =>
        Context.FilterCacheStamps.AsNoTracking().First(stamp => stamp.Group == group).Stamp;

    public RarityBySetFilterOption Row(int id) =>
        Context.RarityBySetFilterOptions.AsNoTracking().First(rarity => rarity.Id == id);

    private void AddRarity(int id, int setId, string rarity) =>
        Context.RarityBySetFilterOptions.Add(new RarityBySetFilterOption
        {
            Id = id,
            SetId = setId,
            Rarity = rarity,
        });

    private static Card NewCard(int id, int setId, string rarity) =>
        new()
        {
            Id = id,
            TcgPlayerId = 5000 + id,
            SetId = setId,
            Name = $"Card {id}",
            Rarity = rarity,
            CardType = "Pokemon",
            CardSubtype = string.Empty,
        };

    public void Dispose() => Context.Dispose();
}
