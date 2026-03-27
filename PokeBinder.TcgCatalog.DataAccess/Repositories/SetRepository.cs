using Microsoft.EntityFrameworkCore;
using PokeBinder.TcgCatalog.DataAccess.Entities;

namespace PokeBinder.TcgCatalog.DataAccess.Repositories;

public class SetRepository(TcgCatalogDbContext context)
{
    public async Task<Set?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await context.Sets
            .Include(s => s.Generation)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<List<Set>> GetByGenerationIdAsync(int generationId, CancellationToken ct = default)
    {
        return await context.Sets
            .Where(s => s.GenerationId == generationId)
            .ToListAsync(ct);
    }

    public async Task<List<Set>> GetAllAsync(CancellationToken ct = default)
    {
        return await context.Sets
            .Include(s => s.Generation)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);
    }
}
