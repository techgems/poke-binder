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

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserClaim> UserClaims => Set<UserClaim>();
    public DbSet<RoleClaim> RoleClaims => Set<RoleClaim>();
    public DbSet<UserLogin> UserLogins => Set<UserLogin>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();

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

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.EmailConfirmed).HasColumnName("emailConfirmed");
            entity.Property(e => e.PasswordHash).HasColumnName("passwordHash");
            entity.Property(e => e.SecurityStamp).HasColumnName("securityStamp");
            entity.Property(e => e.ConcurrencyStamp).HasColumnName("concurrencyStamp");
            entity.Property(e => e.PhoneNumber).HasColumnName("phoneNumber");
            entity.Property(e => e.PhoneNumberConfirmed).HasColumnName("phoneNumberConfirmed");
            entity.Property(e => e.TwoFactorEnabled).HasColumnName("twoFactorEnabled");
            entity.Property(e => e.LockoutEnd).HasColumnName("lockoutEnd");
            entity.Property(e => e.LockoutEnabled).HasColumnName("lockoutEnabled");
            entity.Property(e => e.AccessFailedCount).HasColumnName("accessFailedCount");

            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("IX_users_email");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.ConcurrencyStamp).HasColumnName("concurrencyStamp");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("userRoles");
            entity.HasKey(e => new { e.UserId, e.RoleId });
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.RoleId).HasColumnName("roleId");
        });

        modelBuilder.Entity<UserClaim>(entity =>
        {
            entity.ToTable("userClaims");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.ClaimType).HasColumnName("claimType");
            entity.Property(e => e.ClaimValue).HasColumnName("claimValue");

            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_userClaims_userId");
        });

        modelBuilder.Entity<RoleClaim>(entity =>
        {
            entity.ToTable("roleClaims");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoleId).HasColumnName("roleId");
            entity.Property(e => e.ClaimType).HasColumnName("claimType");
            entity.Property(e => e.ClaimValue).HasColumnName("claimValue");

            entity.HasIndex(e => e.RoleId).HasDatabaseName("IX_roleClaims_roleId");
        });

        modelBuilder.Entity<UserLogin>(entity =>
        {
            entity.ToTable("userLogins");
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });
            entity.Property(e => e.LoginProvider).HasColumnName("loginProvider");
            entity.Property(e => e.ProviderKey).HasColumnName("providerKey");
            entity.Property(e => e.ProviderDisplayName).HasColumnName("providerDisplayName");
            entity.Property(e => e.UserId).HasColumnName("userId");

            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_userLogins_userId");
        });

        modelBuilder.Entity<UserToken>(entity =>
        {
            entity.ToTable("userTokens");
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.LoginProvider).HasColumnName("loginProvider");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Value).HasColumnName("value");
        });
    }
}
