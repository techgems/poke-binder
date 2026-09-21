using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;
using PokeBinder.Binders.DbContext.Entities;
using PokeBinder.TcgCatalog.DbContext;
using CatalogCard = PokeBinder.TcgCatalog.DbContext.Entities.Card;

namespace PokeBinder.Features.Tests.Binder;

/// <summary>
/// A binder database for one test, and the catalog its cards are looked up in.
///
/// <para>
/// The EF in-memory provider, never Sqlite. There is no connection string here and no file on disk
/// for one to point at, so no test can reach the real databases however it is edited later. What
/// that costs is query translation: the in-memory provider is not relational, so a query that would
/// only fail as SQL passes here and has to be caught by running the app.
/// </para>
///
/// <para>
/// Both databases, because the binder slices span both: the binder, its pockets and its tray are in
/// one, and the cards they point at are in the catalog, with no foreign key between the two.
/// Each fixture gets its own database names, so tests cannot leak rows into one another and can run
/// in any order.
/// </para>
/// </summary>
public sealed class BinderFixture : IDisposable
{
    /// <summary>The signed-in user every test acts as unless it says otherwise.</summary>
    public const int UserId = 7;

    /// <summary>Somebody else, for the tests about binders that are not the caller's.</summary>
    public const int OtherUserId = 8;

    /// <summary>The binder the helpers below build and the tests address.</summary>
    public const int BinderId = 1;

    public BinderFixture()
    {
        var binderOptions = new DbContextOptionsBuilder<BinderDbContext>()
            .UseInMemoryDatabase($"binders-{Guid.NewGuid()}")
            .Options;

        var catalogOptions = new DbContextOptionsBuilder<TcgCatalogDbContext>()
            .UseInMemoryDatabase($"catalog-{Guid.NewGuid()}")
            .Options;

        Binders = new BinderDbContext(binderOptions);
        Catalog = new TcgCatalogDbContext(catalogOptions);
    }

    public BinderDbContext Binders { get; }

    public TcgCatalogDbContext Catalog { get; }

    /// <summary>
    /// A binder of the given grid and length, owned by <paramref name="userId"/>.
    /// <para>
    /// The default is a 3x3 over four pages: 36 pockets, enough that a save claiming one page has
    /// three other pages to leave alone, which is what most of these tests are about.
    /// </para>
    /// </summary>
    public BinderFixture WithBinder(int x = 3, int y = 3, int pages = 4, int userId = UserId)
    {
        Binders.BinderSizes.Add(new BinderSize
        {
            Id = 1,
            Name = $"{x}x{y}",
            Description = $"{x * y} cards per page",
            X = x,
            Y = y,
            DefaultPages = pages,
        });

        Binders.Binders.Add(new PokeBinder.Binders.DbContext.Entities.Binder
        {
            Id = BinderId,
            Name = "Test binder",
            CreatedAt = 1_700_000_000,
            UserId = userId,
            BinderSizeId = 1,
            Pages = pages,
        });

        Binders.SaveChanges();

        return this;
    }

    /// <summary>Puts cards in pockets, as a binder that has been saved before would have.</summary>
    public BinderFixture WithPlacements(params (int Pocket, int CardId)[] placements)
    {
        foreach (var (pocket, cardId) in placements)
        {
            Binders.BinderCards.Add(new BinderCard
            {
                BinderId = BinderId,
                CardId = cardId,
                IndexInBinder = pocket,
                IsMissing = false,
            });
        }

        Binders.SaveChanges();

        return this;
    }

    /// <summary>Puts cards in the tray, as a binder that has been saved before would have.</summary>
    public BinderFixture WithTray(params (int CardId, int Quantity)[] entries)
    {
        foreach (var (cardId, quantity) in entries)
        {
            Binders.BinderTray.Add(new BinderTray
            {
                BinderId = BinderId,
                CardId = cardId,
                Quantity = quantity,
            });
        }

        Binders.SaveChanges();

        return this;
    }

    /// <summary>
    /// Cards in the catalog. Only their ids matter here -- the validator checks that a posted card
    /// exists and nothing in these slices reads anything else off it.
    /// </summary>
    public BinderFixture WithCatalogCards(params int[] cardIds)
    {
        foreach (var cardId in cardIds)
        {
            Catalog.Cards.Add(new CatalogCard
            {
                Id = cardId,
                TcgPlayerId = 1000 + cardId,
                SetId = 1,
                Name = $"Card {cardId}",
                CardType = "Pokemon",
                CardSubtype = string.Empty,
            });
        }

        Catalog.SaveChanges();

        return this;
    }

    /// <summary>
    /// What is stored in the binder's pockets now, as pocket to card id.
    /// <para>
    /// Read with the change tracker cleared, so what comes back is what a save committed rather
    /// than the entities the handler happened to be holding.
    /// </para>
    /// </summary>
    public async Task<Dictionary<int, int>> StoredPlacementsAsync()
    {
        Binders.ChangeTracker.Clear();

        return await Binders.BinderCards
            .Where(card => card.BinderId == BinderId)
            .ToDictionaryAsync(card => card.IndexInBinder, card => card.CardId);
    }

    /// <summary>What is stored in the binder's tray now, as card id to quantity.</summary>
    public async Task<Dictionary<int, int>> StoredTrayAsync()
    {
        Binders.ChangeTracker.Clear();

        return await Binders.BinderTray
            .Where(entry => entry.BinderId == BinderId)
            .ToDictionaryAsync(entry => entry.CardId, entry => entry.Quantity);
    }

    public void Dispose()
    {
        Binders.Dispose();
        Catalog.Dispose();
    }
}
