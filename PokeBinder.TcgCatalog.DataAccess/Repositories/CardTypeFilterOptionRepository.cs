using Microsoft.EntityFrameworkCore;
using PokeBinder.TcgCatalog.DbContext;
using PokeBinder.TcgCatalog.DbContext.Entities;

namespace PokeBinder.TcgCatalog.DataAccess.Repositories;

public class CardTypeFilterOptionRepository(TcgCatalogDbContext context)
{
    public async Task<List<CardTypeFilterOption>> GetAllAsync(CancellationToken ct = default)
    {
        return await context.CardTypeFilterOptions
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }
}
