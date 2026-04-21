using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext.Entities;

namespace PokeBinder.Binders.DbContext;

public class BinderDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public BinderDbContext(DbContextOptions<BinderDbContext> options) : base(options) { }

    public DbSet<Binder> Binders => Set<Binder>();
    public DbSet<BinderSize> BinderSizes => Set<BinderSize>();
    public DbSet<BinderCard> BinderCards => Set<BinderCard>();
    public DbSet<BinderTray> BinderTray => Set<BinderTray>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BinderSize>(entity =>
        {
            entity.ToTable("binderSizes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CardCount).HasColumnName("cardCount");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.X).HasColumnName("x");
            entity.Property(e => e.Y).HasColumnName("y");
            entity.Property(e => e.Pages)
                .HasColumnName("pages")
                .HasComputedColumnSql("[cardCount] / ([x] * [y])", stored: true);

            entity.HasMany(e => e.Binders)
                .WithOne(b => b.BinderSize)
                .HasForeignKey(b => b.BinderSizeId);
        });

        modelBuilder.Entity<Binder>(entity =>
        {
            entity.ToTable("binder");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.BinderSizeId).HasColumnName("binderSizeId");

            entity.HasMany(e => e.Cards)
                .WithOne(c => c.Binder)
                .HasForeignKey(c => c.BinderId);

            entity.HasMany(e => e.Tray)
                .WithOne(t => t.Binder)
                .HasForeignKey(t => t.BinderId);
        });

        modelBuilder.Entity<BinderCard>(entity =>
        {
            entity.ToTable("binderCards");
            entity.HasKey(e => new { e.BinderId, e.IndexInBinder });
            entity.Property(e => e.BinderId).HasColumnName("binderId");
            entity.Property(e => e.CardId).HasColumnName("cardId");
            entity.Property(e => e.IndexInBinder).HasColumnName("indexInBinder");
            entity.Property(e => e.IsMissing).HasColumnName("isMissing");
        });

        modelBuilder.Entity<BinderTray>(entity =>
        {
            entity.ToTable("binderTray", t =>
                t.HasCheckConstraint("CK_binderTray_quantity", "[quantity] > 0"));
            entity.HasKey(e => new { e.BinderId, e.CardId });
            entity.Property(e => e.BinderId).HasColumnName("binderId");
            entity.Property(e => e.CardId).HasColumnName("cardId");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
        });
    }
}
