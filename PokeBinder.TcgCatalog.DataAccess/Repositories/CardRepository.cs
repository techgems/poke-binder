using Microsoft.EntityFrameworkCore;
using PokeBinder.TcgCatalog.DataAccess.Entities;

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
}
