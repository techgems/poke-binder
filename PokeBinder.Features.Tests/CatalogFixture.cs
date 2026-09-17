using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PokeBinder.TcgCatalog.DbContext;
using PokeBinder.TcgCatalog.DbContext.Entities;

namespace PokeBinder.Features.Tests;

/// <summary>
/// A catalog database for one test, and the cache that goes with it.
///
/// <para>
/// The EF in-memory provider, never Sqlite. There is no connection string here and no file on disk
/// for one to point at, so no test can reach the real catalog database however it is edited later.
/// What that costs is query translation: the in-memory provider is not relational, so a query that
/// would only fail as SQL passes here and has to be caught by running the app.
/// </para>
///
/// <para>
/// Each fixture gets its own database name and its own <see cref="IMemoryCache"/>, so tests cannot
/// leak rows, stamps or cached entries into one another and can run in any order.
/// </para>
/// </summary>
public sealed class CatalogFixture : IDisposable
{
    public CatalogFixture()
    {
        var options = new DbContextOptionsBuilder<TcgCatalogDbContext>()
            .UseInMemoryDatabase($"catalog-{Guid.NewGuid()}")
            .Options;

        Context = new TcgCatalogDbContext(options);
    }

    public TcgCatalogDbContext Context { get; }

    public IMemoryCache Cache { get; } = new MemoryCache(new MemoryCacheOptions());

    /// <summary>
    /// A small but complete catalog: two of most things, so "the response carried this group" and
    /// "the response carried the right group" are different assertions.
    /// <para>
    /// Card types are seeded with the ETL's UNKNOWN placeholder among them, because leaving it out
    /// of the super types is behaviour the slice owns.
    /// </para>
    /// </summary>
    public CatalogFixture WithCatalog()
    {
        Context.Games.Add(new Game { Id = 1, Slug = "pokemon", Name = "Pokemon" });

        Context.Series.Add(new Series { Id = 1, Slug = "base", Name = "Base Series", GameId = 1 });
        Context.Series.Add(new Series { Id = 2, Slug = "neo", Name = "Neo Series", GameId = 1 });

        Context.Sets.Add(new Set { Id = 1, Code = "BS", Name = "Base Set", FullName = "Base Set", SeriesId = 1 });
        Context.Sets.Add(new Set { Id = 2, Code = "JU", Name = "Jungle", FullName = "Jungle", SeriesId = 1 });
        Context.Sets.Add(new Set { Id = 3, Code = "N1", Name = "Neo Genesis", FullName = "Neo Genesis", SeriesId = 2 });

        Context.GenerationFilterOptions.Add(new GenerationFilterOption { Id = 1, Name = "Generation I" });
        Context.GenerationFilterOptions.Add(new GenerationFilterOption { Id = 2, Name = "Generation II" });

        Context.PokemonFilterOptions.Add(new PokemonFilterOption { Id = 1, PokedexNumber = 25, Name = "Pikachu", GenerationId = 1 });
        Context.PokemonFilterOptions.Add(new PokemonFilterOption { Id = 2, PokedexNumber = 6, Name = "Charizard", GenerationId = 1 });
        Context.PokemonFilterOptions.Add(new PokemonFilterOption { Id = 3, PokedexNumber = 157, Name = "Typhlosion", GenerationId = 2 });

        Context.RarityBySetFilterOptions.Add(new RarityBySetFilterOption { Id = 1, SetId = 1, Rarity = "Rare Holo" });
        Context.RarityBySetFilterOptions.Add(new RarityBySetFilterOption { Id = 2, SetId = 1, Rarity = "Common" });
        Context.RarityBySetFilterOptions.Add(new RarityBySetFilterOption { Id = 3, SetId = 3, Rarity = "Rare" });

        Context.CardTypeFilterOptions.Add(new CardTypeFilterOption { Id = 1, Name = "Fire", ImageUrl = "/images/fire.png" });
        Context.CardTypeFilterOptions.Add(new CardTypeFilterOption { Id = 2, Name = "Water", ImageUrl = null });

        // Super types are read as the distinct card types in use, so they come from these rows and
        // not from a table of their own. UNKNOWN is what the ETL writes when the source had none.
        Context.Cards.Add(NewCard(1, "Charizard", "Pokemon"));
        Context.Cards.Add(NewCard(2, "Bill", "Trainer"));
        Context.Cards.Add(NewCard(3, "Fire Energy", "Energy"));
        Context.Cards.Add(NewCard(4, "Mystery", "UNKNOWN"));
        Context.Cards.Add(NewCard(5, "Pikachu", "Pokemon"));

        Context.SaveChanges();

        return this;
    }

    /// <summary>Gives every group a stamp, as the seed migration does on a real database.</summary>
    public CatalogFixture WithStamps(params FilterCacheGroup[] groups)
    {
        foreach (var group in groups.Length > 0 ? groups : Enum.GetValues<FilterCacheGroup>())
        {
            Context.FilterCacheStamps.Add(new FilterCacheStamp
            {
                Group = group,
                Stamp = StampFor(group),
                LastBumpedUnix = 1_700_000_000,
            });
        }

        Context.SaveChanges();

        return this;
    }

    /// <summary>The stamp a group is seeded with. Deterministic, so a test can assert against it.</summary>
    public static string StampFor(FilterCacheGroup group) => $"seeded-{group}";

    /// <summary>Moves a group's stamp, the way the admin screen's Bump button does.</summary>
    public async Task BumpAsync(FilterCacheGroup group, string stamp)
    {
        var row = await Context.FilterCacheStamps.FirstAsync(s => s.Group == group);

        row.Stamp = stamp;
        row.LastBumpedUnix = 1_700_000_001;

        await Context.SaveChangesAsync();
    }

    private static Card NewCard(int id, string name, string cardType) =>
        new()
        {
            Id = id,
            TcgPlayerId = 1000 + id,
            SetId = 1,
            Name = name,
            CardType = cardType,
            CardSubtype = string.Empty,
        };

    public void Dispose()
    {
        Context.Dispose();
        Cache.Dispose();
    }
}
