using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PokeBinder.Features.CardSearch.GetSearchStarterFilters.Models;
using PokeBinder.TcgCatalog.DbContext;
using PokeBinder.TcgCatalog.DbContext.Entities;
using System.Linq.Expressions;

namespace PokeBinder.Features.CardSearch.GetSearchStarterFilters;

public static class GetSearchStarterFilters
{
    private const string CacheKey = "CardSearch:StarterFilters";

    public record Request();

    public record Response(
        IReadOnlyList<GenerationsFilter> Generations,
        IReadOnlyList<SetsFilter> Sets,
        IReadOnlyList<PokemonFilter> Pokemon,
        IReadOnlyList<RarityBySetFilter> RarityBySet,
        IReadOnlyList<CardTypeFilter> CardType
    );

    public static async Task<Response> Handler(
        Request request,
        TcgCatalogDbContext context,
        IMemoryCache cache,
        CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out Response? cached) && cached is not null)
        {
            return cached;
        }

        var result = await QueryAsync(context, ct);

        cache.Set(CacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
            Priority = CacheItemPriority.NeverRemove
        });

        return result;
    }

    private static async Task<Response> QueryAsync(TcgCatalogDbContext context, CancellationToken ct)
    {
        var generations = await context.GenerationFilterOptions.Select(MapGenerations).ToListAsync(ct);
        var sets = await context.Sets.Select(MapSets).ToListAsync(ct);
        var pokemon = await context.PokemonFilterOptions.Select(MapPokemon).ToListAsync(ct);
        var rarities = await context.RarityBySetFilterOptions.Select(MapRarities).ToListAsync(ct);
        var cardTypes = await context.CardTypeFilterOptions.Select(MapCardTypes).ToListAsync(ct);

        return new Response(generations, sets, pokemon, rarities, cardTypes);
    }

    private static readonly Expression<Func<GenerationFilterOption, GenerationsFilter>> MapGenerations =
        series => new GenerationsFilter()
        {
            Id = series.Id,
            Name = series.Name
        };

    private static readonly Expression<Func<Set, SetsFilter>> MapSets =
        set => new SetsFilter()
        {
            Id = set.Id,
            Name = set.Name,
            GenerationId = set.SeriesId
        };

    private static readonly Expression<Func<PokemonFilterOption, PokemonFilter>> MapPokemon =
        pokemon => new PokemonFilter()
        {
            Id = pokemon.Id,
            PokedexNumber = pokemon.PokedexNumber,
            Name = pokemon.Name,
            GenerationId = pokemon.GenerationId,
            AlternateName = pokemon.AlternateName
        };

    private static readonly Expression<Func<RarityBySetFilterOption, RarityBySetFilter>> MapRarities =
        rarity => new RarityBySetFilter()
        {
            Id = rarity.Id,
            SetId = rarity.SetId,
            Rarity = rarity.Rarity
        };

    private static readonly Expression<Func<CardTypeFilterOption, CardTypeFilter>> MapCardTypes =
        cardType => new CardTypeFilter()
        {
            Id = cardType.Id,
            Name = cardType.Name
        };
}
