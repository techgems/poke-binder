// Client for the card search endpoints. Same-origin requests, so the auth cookie rides along.

export interface GenerationsFilter {
  id: number
  name: string
}

export interface SetsFilter {
  id: number
  name: string
  generationId: number
}

export interface PokemonFilter {
  id: number
  pokedexNumber: number
  name: string
  generationId: number
  alternateName: string | null
}

export interface RarityBySetFilter {
  id: number
  setId: number
  rarity: string
}

export interface CardTypeFilter {
  id: number
  name: string
}

export interface StarterFilters {
  generations: GenerationsFilter[]
  sets: SetsFilter[]
  pokemon: PokemonFilter[]
  rarityBySet: RarityBySetFilter[]
  cardType: CardTypeFilter[]
}

export const CardSearchClient = {
  async getStarterFilters(signal?: AbortSignal): Promise<StarterFilters> {
    const response = await fetch('/api/cardFilters/starterFilters', {
      // The SPA is served by the same host as the API, so cookies are sent by default.
      credentials: 'same-origin',
      headers: { Accept: 'application/json' },
      signal,
    })

    if (!response.ok) {
      throw new Error(`Failed to load starter filters (${response.status} ${response.statusText}).`)
    }

    return (await response.json()) as StarterFilters
  },
}
