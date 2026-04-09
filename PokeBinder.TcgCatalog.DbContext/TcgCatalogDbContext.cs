using Microsoft.EntityFrameworkCore;
using PokeBinder.TcgCatalog.DbContext.Entities;

namespace PokeBinder.TcgCatalog.DbContext;

public class TcgCatalogDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public TcgCatalogDbContext(DbContextOptions<TcgCatalogDbContext> options) : base(options) { }

    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Set> Sets => Set<Set>();
    public DbSet<Generation> Generations => Set<Generation>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GenerationFilterOption> GenerationFilterOptions => Set<GenerationFilterOption>();
    public DbSet<PokemonFilterOption> PokemonFilterOptions => Set<PokemonFilterOption>();
    public DbSet<RarityBySetFilterOption> RarityBySetFilterOptions => Set<RarityBySetFilterOption>();
    public DbSet<CardTypeFilterOption> CardTypeFilterOptions => Set<CardTypeFilterOption>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<CardTag> CardTags => Set<CardTag>();
    public DbSet<PokemonCardText> PokemonCardTexts => Set<PokemonCardText>();
    public DbSet<NonPokemonCardText> NonPokemonCardTexts => Set<NonPokemonCardText>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>(entity =>
        {
            entity.ToTable("games");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Slug).HasColumnName("slug");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.HasMany(e => e.Generations)
                .WithOne(g => g.Game)
                .HasForeignKey(g => g.GameId);
        });

        modelBuilder.Entity<Generation>(entity =>
        {
            entity.ToTable("generations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Slug).HasColumnName("slug");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.StartDateUnix).HasColumnName("startDateUnix");
            entity.Property(e => e.EndDateUnix).HasColumnName("endDateUnix");
            entity.Property(e => e.GameId).HasColumnName("gameId");

            entity.HasMany(e => e.Sets)
                .WithOne(s => s.Generation)
                .HasForeignKey(s => s.GenerationId);
        });

        modelBuilder.Entity<Set>(entity =>
        {
            entity.ToTable("sets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.FullName).HasColumnName("fullName");
            entity.Property(e => e.ReleaseDateUnix).HasColumnName("releaseDateUnix");
            entity.Property(e => e.ImageUrl).HasColumnName("imageUrl");
            entity.Property(e => e.GenerationId).HasColumnName("generationId");
            entity.Property(e => e.PriorityOrder).HasColumnName("priorityOrder");
            entity.Property(e => e.DateLoadedUnix).HasColumnName("dateLoadedUnix");

            entity.HasMany(e => e.Cards)
                .WithOne(c => c.Set)
                .HasForeignKey(c => c.SetId);
        });

        modelBuilder.Entity<GenerationFilterOption>(entity =>
        {
            entity.ToTable("generationFilterOptions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(63);

            entity.HasMany(e => e.PokemonFilterOptions)
                .WithOne(p => p.Generation)
                .HasForeignKey(p => p.GenerationId);
        });

        modelBuilder.Entity<PokemonFilterOption>(entity =>
        {
            entity.ToTable("pokemonFilterOptions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.PokedexNumber).HasColumnName("pokedexNumber");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255);
            entity.Property(e => e.GenerationId).HasColumnName("generationId");
            entity.Property(e => e.AlternateName).HasColumnName("alternateName").HasMaxLength(255);
        });

        modelBuilder.Entity<RarityBySetFilterOption>(entity =>
        {
            entity.ToTable("rarityBySetFilterOption");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SetId).HasColumnName("setId");
            entity.Property(e => e.Rarity).HasColumnName("rarity").HasMaxLength(127);

            entity.HasOne(e => e.Set)
                .WithMany()
                .HasForeignKey(e => e.SetId);
        });

        modelBuilder.Entity<CardTypeFilterOption>(entity =>
        {
            entity.ToTable("cardTypeFilterOption");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(63);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("tags");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.ExtendedName).HasColumnName("extendedName");
        });

        modelBuilder.Entity<CardTag>(entity =>
        {
            entity.ToTable("cardTags");
            entity.HasKey(e => new { e.CardId, e.TagId });
            entity.Property(e => e.CardId).HasColumnName("cardId");
            entity.Property(e => e.TagId).HasColumnName("tagId");

            entity.HasOne(e => e.Card)
                .WithMany()
                .HasForeignKey(e => e.CardId);

            entity.HasOne(e => e.Tag)
                .WithMany(t => t.CardTags)
                .HasForeignKey(e => e.TagId);
        });

        modelBuilder.Entity<PokemonCardText>(entity =>
        {
            entity.ToTable("pkmnCardText");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.HP).HasColumnName("hp");
            entity.Property(e => e.Resistance).HasColumnName("resistance");
            entity.Property(e => e.Weaknesses).HasColumnName("weaknesses");
            entity.Property(e => e.FlavorText).HasColumnName("flavorText");
            entity.Property(e => e.Retreat).HasColumnName("retreat");
            entity.Property(e => e.AttackList).HasColumnName("attackList")
                .HasColumnType("TEXT")
                .HasConversion(
                    v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<List<Attack>>(v, (System.Text.Json.JsonSerializerOptions?)null)
                );
            entity.Property(e => e.Ability).HasColumnName("ability")
                .HasColumnType("TEXT")
                .HasConversion(
                    v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<Ability>(v, (System.Text.Json.JsonSerializerOptions?)null)
                );
            entity.Property(e => e.EvolvesFrom).HasColumnName("evolvesFrom");
            entity.Property(e => e.DexNumber).HasColumnName("dexNumber");
            entity.Property(e => e.Stage).HasColumnName("stage");
            entity.HasOne(e => e.Card)
                .WithOne(c => c.PokemonCardText)
                .HasForeignKey<PokemonCardText>(e => e.CardId);
        });

        modelBuilder.Entity<NonPokemonCardText>(entity =>
        {
            entity.ToTable("nonPkmnCardText");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Text).HasColumnName("text");

            entity.HasOne(e => e.Card)
                .WithOne(c => c.NonPokemonCardText)
                .HasForeignKey<NonPokemonCardText>(e => e.Id);
        });

        modelBuilder.Entity<Card>(entity =>
        {
            entity.ToTable("cards");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TcgPlayerId).HasColumnName("tcgPlayerId");
            entity.Property(e => e.SetId).HasColumnName("setId");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Rarity).HasColumnName("rarity");
            entity.Property(e => e.CardNumber).HasColumnName("cardNumber");
            entity.Property(e => e.CardType).HasColumnName("cardType");
            entity.Property(e => e.CardSubtype).HasColumnName("cardSubtype");
            entity.Property(e => e.ImageUrl).HasColumnName("imageUrl");
            entity.Property(e => e.Artist).HasColumnName("artist");
            entity.Property(e => e.MaskImageOneUrl).HasColumnName("maskImageOneUrl");
            entity.Property(e => e.MaskImageTwoUrl).HasColumnName("maskImageTwoUrl");
            entity.Property(e => e.HasImageDownloadAttempt).HasColumnName("hasImageDownloadAttempt")
                .HasDefaultValue(false);
            entity.Property(e => e.PokemonCardTextId).HasColumnName("pkmnCardTextId");
            entity.Property(e => e.NonPokemonCardTextId).HasColumnName("nonPkmnCardTextId");

            entity.HasOne(e => e.PokemonCardText)
                .WithMany()
                .HasForeignKey(e => e.PokemonCardTextId);

            entity.HasOne(e => e.NonPokemonCardText)
                .WithMany()
                .HasForeignKey(e => e.NonPokemonCardTextId);
        });
    }
}
