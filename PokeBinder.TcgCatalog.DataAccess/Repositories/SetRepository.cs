using Microsoft.EntityFrameworkCore;
using PokeBinder.TcgCatalog.DbContext;
using PokeBinder.TcgCatalog.DbContext.Entities;

namespace PokeBinder.TcgCatalog.DataAccess.Repositories;

public class SetRepository(TcgCatalogDbContext context)
{
    public async Task<Set?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await context.Sets
            .Include(s => s.Series)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<List<Set>> GetBySeriesId(int seriesId, CancellationToken ct = default)
    {
        return await context.Sets
            .Where(s => s.SeriesId == seriesId)
            .ToListAsync(ct);
    }

    public async Task<List<Set>> GetAllAsync(CancellationToken ct = default)
    {
        return await context.Sets
            .Include(s => s.Series)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);
    }
}
