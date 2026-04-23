using Microsoft.EntityFrameworkCore;
using PokeBinder.TcgCatalog.DbContext;
using PokeBinder.TcgCatalog.DbContext.Entities;

namespace PokeBinder.TcgCatalog.DataAccess.Repositories;

public class CardRepository(TcgCatalogDbContext context)
{
    public async Task<Card?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await context.Cards
            .Include(c => c.Set)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<List<Card>> GetBySetIdAsync(int setId, CancellationToken ct = default)
    {
        return await context.Cards
            .Where(c => c.SetId == setId)
            .ToListAsync(ct);
    }

    public async Task<List<Card>> SearchByNameAsync(string name, CancellationToken ct = default)
    {
        return await context.Cards
            .Where(c => c.Name != null && EF.Functions.Like(c.Name, $"%{name}%"))
            .Include(c => c.Set)
            .ToListAsync(ct);
    }

    public async Task<List<Card>> SearchAsync(string name, string rarity, string cardNumber, int? tcgPlayerId, int? setId, int? generationId, CancellationToken ct = default)
    {
        var cards = context.Cards
            .Include(c => c.Set)
                .ThenInclude(s => s!.Series)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
            cards = cards.Where(c => c.Name != null && EF.Functions.Like(c.Name, $"%{name}%"));

        if (!string.IsNullOrWhiteSpace(rarity))
            cards = cards.Where(c => c.Rarity == rarity);

        if (!string.IsNullOrWhiteSpace(cardNumber))
            cards = cards.Where(c => c.CardNumber == cardNumber);

        if (tcgPlayerId.HasValue)
            cards = cards.Where(c => c.TcgPlayerId == tcgPlayerId.Value);

        if (setId.HasValue)
            cards = cards.Where(c => c.SetId == setId.Value);

        if (generationId.HasValue)
            cards = cards.Where(c => c.Set != null && c.Set.SeriesId == generationId.Value);

        var results = await cards.ToListAsync(ct);

        return results;
    }
}
