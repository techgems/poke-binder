using Microsoft.EntityFrameworkCore;
using PokeBinder.TcgCatalog.DbContext;
using PokeBinder.TcgCatalog.DbContext.Entities;

namespace PokeBinder.TcgCatalog.DataAccess.Repositories;

public class GenerationRepository(TcgCatalogDbContext context)
{
    public async Task<Generation?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await context.Generations
            .Include(g => g.Sets)
            .FirstOrDefaultAsync(g => g.Id == id, ct);
    }

    public async Task<List<Generation>> GetAllAsync(CancellationToken ct = default)
    {
        return await context.Generations
            .OrderBy(g => g.StartDateUnix)
            .ToListAsync(ct);
    }
}
