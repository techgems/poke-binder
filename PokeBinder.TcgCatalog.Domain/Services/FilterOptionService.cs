using Microsoft.Extensions.Caching.Memory;
using PokeBinder.TcgCatalog.DataAccess.Repositories;
using PokeBinder.TcgCatalog.DomainModels.Responses;

namespace PokeBinder.TcgCatalog.Domain.Services;

public class FilterOptionService(
    GenerationFilterOptionRepository generationFilterOptionRepository,
    PokemonFilterOptionRepository pokemonFilterOptionRepository,
    RarityBySetFilterOptionRepository rarityBySetFilterOptionRepository,
    CardTypeFilterOptionRepository cardTypeFilterOptionRepository,
    IMemoryCache cache)
{
    private const string SearchFiltersCacheKey = "FilterOptions:All";

    public async Task<SearchFiltersResponse> GetSearchFiltersAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(SearchFiltersCacheKey, out SearchFiltersResponse? cached) && cached is not null)
        {
            return cached;
        }

        var generationsTask = generationFilterOptionRepository.GetAllAsync(ct);
        var pokemonTask = pokemonFilterOptionRepository.GetAllAsync(ct);
        var raritiesTask = rarityBySetFilterOptionRepository.GetAllAsync(ct);
        var cardTypesTask = cardTypeFilterOptionRepository.GetAllAsync(ct);

        await Task.WhenAll(generationsTask, pokemonTask, raritiesTask, cardTypesTask);

        var response = new SearchFiltersResponse
        {
            Generations = (await generationsTask).Select(g => new GenerationFilterOptionItem
            {
                Id = g.Id,
                Name = g.Name
            }).ToList(),

            Pokemon = (await pokemonTask).Select(p => new PokemonFilterOptionItem
            {
                Id = p.Id,
                PokedexNumber = p.PokedexNumber,
                Name = p.Name,
                GenerationId = p.GenerationId,
                AlternateName = p.AlternateName
            }).ToList(),

            RaritiesBySet = (await raritiesTask).Select(r => new RarityBySetFilterOptionItem
            {
                Id = r.Id,
                SetId = r.SetId,
                Rarity = r.Rarity
            }).ToList(),

            CardTypes = (await cardTypesTask).Select(c => new CardTypeFilterOptionItem
            {
                Id = c.Id,
                Name = c.Name
            }).ToList()
        };

        cache.Set(SearchFiltersCacheKey, response, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        });

        return response;
    }
}
