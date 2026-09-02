// Client for the card search endpoints. Same-origin requests, so the auth cookie rides along.

/** A card's super type (Pokemon, Trainer, Energy). Has no table, so the name is the identifier. */
export interface SuperTypeFilter {
  name: string
}

export interface GenerationsFilter {
  id: number
  name: string
}

export interface SeriesFilter {
  id: number
  name: string
}

export interface SetsFilter {
  id: number
  name: string
  seriesId: number
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
  // Null when the type has no energy symbol art.
  imageUrl: string | null
}

export interface StarterFilters {
  superTypes: SuperTypeFilter[]
  generations: GenerationsFilter[]
  series: SeriesFilter[]
  sets: SetsFilter[]
  pokemon: PokemonFilter[]
  rarityBySet: RarityBySetFilter[]
  cardType: CardTypeFilter[]
}

/** Filters for a card search. Ids go over the wire as numbers, unlike the string-valued UI state. */
export interface CardSearchRequest {
  superTypes: string[]
  generations: number[]
  series: number[]
  sets: number[]
  pokemon: number[]
  /** Rarity names, not ids: the same name is the same rarity in every set. */
  rarities: string[]
  cardTypes: number[]
  /** 1-based. Omit to get the first page. */
  pageNumber?: number
  /** Omit to take the server's default; the server caps it regardless. */
  pageSize?: number
}

export interface CardSearchResult {
  id: number
  name: string | null
  rarity: string | null
  cardNumber: string | null
  tcgPlayerId: number
  /**
   * Loadable URL for the card art, already resolved server-side from the ETL's local file path.
   * Null when the card has no art or its path sits outside the configured image root.
   */
  imageUrl: string | null
  setName: string | null
}

/** One page of search results. There is no total count, only whether another page exists. */
export interface CardSearchPage {
  results: CardSearchResult[]
  pageNumber: number
  pageSize: number
  hasMore: boolean
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

  async searchByFilter(
    request: CardSearchRequest,
    signal?: AbortSignal,
  ): Promise<CardSearchPage> {
    // The filters go in the body rather than the query string: there are seven of them and every
    // one is multi-valued.
    const response = await fetch('/api/cardSearch/byFilter', {
      method: 'POST',
      credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify(request),
      signal,
    })

    if (!response.ok) {
      throw new Error(`Card search failed (${response.status} ${response.statusText}).`)
    }

    return (await response.json()) as CardSearchPage
  },
}
