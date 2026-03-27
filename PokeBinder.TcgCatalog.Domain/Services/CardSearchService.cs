using Microsoft.EntityFrameworkCore;
using PokeBinder.TcgCatalog.DataAccess;
using PokeBinder.TcgCatalog.DomainModels.Queries;
using PokeBinder.TcgCatalog.DomainModels.Responses;

namespace PokeBinder.TcgCatalog.Domain.Services;

public class CardSearchService(TcgCatalogDbContext context)
{
    public async Task<CardSearchResponse> SearchAsync(CardSearchQuery query, CancellationToken ct = default)
    {
        var cards = context.Cards
            .Include(c => c.Set)
                .ThenInclude(s => s!.Generation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Name))
            cards = cards.Where(c => c.Name != null && EF.Functions.Like(c.Name, $"%{query.Name}%"));

        if (!string.IsNullOrWhiteSpace(query.Rarity))
            cards = cards.Where(c => c.Rarity == query.Rarity);

        if (!string.IsNullOrWhiteSpace(query.CardNumber))
            cards = cards.Where(c => c.CardNumber == query.CardNumber);

        if (query.TcgPlayerId.HasValue)
            cards = cards.Where(c => c.TcgPlayerId == query.TcgPlayerId.Value);

        if (query.SetId.HasValue)
            cards = cards.Where(c => c.SetId == query.SetId.Value);

        if (query.GenerationId.HasValue)
            cards = cards.Where(c => c.Set != null && c.Set.GenerationId == query.GenerationId.Value);

        var results = await cards.Select(c => new CardSearchResult
        {
            Id = c.Id,
            Name = c.Name,
            Rarity = c.Rarity,
            CardNumber = c.CardNumber,
            TcgPlayerId = c.TcgPlayerId,
            ImageUrl = c.ImageUrl,
            SetName = c.Set != null ? c.Set.Name : null,
            GenerationName = c.Set != null && c.Set.Generation != null ? c.Set.Generation.Name : null
        }).ToListAsync(ct);

        return new CardSearchResponse { Results = results };
    }
}
