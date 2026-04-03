using Microsoft.EntityFrameworkCore;
using PokeBinder.TcgCatalog.DbContext;
using PokeBinder.TcgCatalog.DbContext.Entities;

namespace PokeBinder.TcgCatalog.DataAccess.Repositories;

public class PokemonFilterOptionRepository(TcgCatalogDbContext context)
{
    public async Task<List<PokemonFilterOption>> GetAllAsync(CancellationToken ct = default)
    {
        return await context.PokemonFilterOptions
            .Include(p => p.Generation)
            .OrderBy(p => p.PokedexNumber)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);
    }

    public async Task<List<PokemonFilterOption>> GetByGenerationIdAsync(int generationId, CancellationToken ct = default)
    {
        return await context.PokemonFilterOptions
            .Where(p => p.GenerationId == generationId)
            .OrderBy(p => p.PokedexNumber)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);
    }

    public async Task<PokemonFilterOption?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await context.PokemonFilterOptions
            .Include(p => p.Generation)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }
}
