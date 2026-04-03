using Microsoft.EntityFrameworkCore;
using PokeBinder.TcgCatalog.DbContext;
using PokeBinder.TcgCatalog.DbContext.Entities;

namespace PokeBinder.TcgCatalog.DataAccess.Repositories;

public class RarityBySetFilterOptionRepository(TcgCatalogDbContext context)
{
    public async Task<List<RarityBySetFilterOption>> GetAllAsync(CancellationToken ct = default)
    {
        return await context.RarityBySetFilterOptions
            .OrderBy(r => r.SetId)
            .ThenBy(r => r.Rarity)
            .ToListAsync(ct);
    }

    public async Task<List<RarityBySetFilterOption>> GetBySetIdAsync(int setId, CancellationToken ct = default)
    {
        return await context.RarityBySetFilterOptions
            .Where(r => r.SetId == setId)
            .OrderBy(r => r.Rarity)
            .ToListAsync(ct);
    }
}
