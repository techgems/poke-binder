using Microsoft.EntityFrameworkCore;
using PokeBinder.TcgCatalog.DbContext;
using PokeBinder.TcgCatalog.DbContext.Entities;

namespace PokeBinder.TcgCatalog.DataAccess.Repositories;

public class SeriesRepository(TcgCatalogDbContext context)
{
    public async Task<Series?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await context.Generations
            .Include(g => g.Sets)
            .FirstOrDefaultAsync(g => g.Id == id, ct);
    }

    public async Task<List<Series>> GetAllAsync(CancellationToken ct = default)
    {
        return await context.Generations
            .OrderBy(g => g.StartDateUnix)
            .ToListAsync(ct);
    }
}
