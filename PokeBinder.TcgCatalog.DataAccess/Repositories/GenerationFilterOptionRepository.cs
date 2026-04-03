using Microsoft.EntityFrameworkCore;
using PokeBinder.TcgCatalog.DbContext;
using PokeBinder.TcgCatalog.DbContext.Entities;

namespace PokeBinder.TcgCatalog.DataAccess.Repositories;

public class GenerationFilterOptionRepository(TcgCatalogDbContext context)
{
    public async Task<List<GenerationFilterOption>> GetAllAsync(CancellationToken ct = default)
    {
        return await context.GenerationFilterOptions
            .OrderBy(g => g.Id)
            .ToListAsync(ct);
    }

    public async Task<GenerationFilterOption?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await context.GenerationFilterOptions
            .FirstOrDefaultAsync(g => g.Id == id, ct);
    }
}
