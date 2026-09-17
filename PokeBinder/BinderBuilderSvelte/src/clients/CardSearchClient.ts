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

/**
 * The filter options the advanced search is built from, complete. This is what the UI takes: every
 * group present, however it was assembled -- some of it embedded in the page, some read out of
 * localStorage, some just fetched.
 */
export interface StarterFilters {
  superTypes: SuperTypeFilter[]
  generations: GenerationsFilter[]
  series: SeriesFilter[]
  sets: SetsFilter[]
  pokemon: PokemonFilter[]
  rarityBySet: RarityBySetFilter[]
  cardType: CardTypeFilter[]
}

/** The groups this browser caches, in the order they are compared and stored. */
export const CACHED_FILTER_GROUPS = [
  'pokemon',
  'generations',
  'series',
  'sets',
  'rarityBySet',
] as const

export type CachedFilterGroup = (typeof CACHED_FILTER_GROUPS)[number]

/**
 * The catalog's current stamp for each cached group: an opaque value compared for equality and
 * nothing else. Null means the catalog has no stamp for that group, which makes it uncacheable --
 * nothing could ever tell this browser the copy had gone stale -- so it is used and not stored.
 */
export type FilterStamps = Record<CachedFilterGroup, string | null>

/**
 * What the filters endpoint returns, and -- deliberately -- the same shape the page embeds. A group
 * is null when it was not sent, which is not the same as an empty array: "you already have this"
 * against "there are none of these", and overwriting a good cached copy with the second would be a
 * bug that only shows up a release later.
 */
export interface StarterFiltersResponse {
  superTypes: SuperTypeFilter[] | null
  generations: GenerationsFilter[] | null
  series: SeriesFilter[] | null
  sets: SetsFilter[] | null
  pokemon: PokemonFilter[] | null
  rarityBySet: RarityBySetFilter[] | null
  cardType: CardTypeFilter[] | null
  /** Always complete, whatever groups the response carries. */
  stamps: FilterStamps
}

/** Filters for a card search. Ids go over the wire as numbers, unlike the string-valued UI state. */
export interface CardSearchRequest {
  /** Matched anywhere in the card's name, case-insensitively. Omit when the box is empty. */
  cardName?: string
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

/**
 * Terms for a simple card search. Both fields are optional and independent; blank means the user
 * did not narrow by it, and blank on both sides matches nothing rather than everything.
 */
export interface SimpleCardSearchRequest {
  /** Matched anywhere in the card's name, case-insensitively. */
  cardName?: string
  /** Matched against the whole card number as printed on the card ("4", "SV49"), never part of it. */
  cardNumber?: string
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

/** Query-string name for each group's stamp, matching GetSearchStarterFilters.Request. */
const STAMP_PARAMETERS: Record<CachedFilterGroup, string> = {
  pokemon: 'pokemonCacheStamp',
  generations: 'generationsCacheStamp',
  series: 'seriesCacheStamp',
  sets: 'setsCacheStamp',
  rarityBySet: 'rarityBySetCacheStamp',
}

export const CardSearchClient = {
  /**
   * Asks for the filter groups this browser's stamps no longer buy it.
   *
   * What travels is what the browser holds, not what it concluded: the server compares and decides,
   * so a stamp it still recognises costs nothing to send and a stamp it has retired brings the
   * group back. Send a stamp only for a group whose rows are actually in hand -- claiming a copy
   * that is not there is how a client ends up with an empty list it believes is current.
   *
   * @param held Stamps for the groups this browser has rows for, or null for "nothing at all",
   * which asks for everything including the two groups that are never cached.
   */
  async getStarterFilters(
    held: Partial<FilterStamps> | null,
    signal?: AbortSignal,
  ): Promise<StarterFiltersResponse> {
    const query = new URLSearchParams()

    if (held) {
      for (const group of CACHED_FILTER_GROUPS) {
        const stamp = held[group]

        if (stamp) query.set(STAMP_PARAMETERS[group], stamp)
      }
    }

    // An empty query is the first visit and is answered with the whole catalog of options, so this
    // is one request either way -- never a probe followed by a fetch.
    const response = await fetch(`/api/cardFilters/starterFilters?${query}`, {
      credentials: 'same-origin',
      headers: { Accept: 'application/json' },
      signal,
    })

    if (!response.ok) {
      throw new Error(`Failed to load card filters (${response.status} ${response.statusText}).`)
    }

    return (await response.json()) as StarterFiltersResponse
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

  async searchSimple(
    request: SimpleCardSearchRequest,
    signal?: AbortSignal,
  ): Promise<CardSearchPage> {
    // Two single-valued terms fit the query string, so this one is a GET. Empty fields are left
    // out entirely rather than sent blank: the server reads absent and blank the same way, and a
    // URL carrying only what was typed is the one that shows up readably in the network tab.
    const query = new URLSearchParams()

    if (request.cardName) query.set('cardName', request.cardName)
    if (request.cardNumber) query.set('cardNumber', request.cardNumber)
    if (request.pageNumber !== undefined) query.set('pageNumber', String(request.pageNumber))
    if (request.pageSize !== undefined) query.set('pageSize', String(request.pageSize))

    const response = await fetch(`/api/cardSearch/simpleSearch?${query}`, {
      credentials: 'same-origin',
      headers: { Accept: 'application/json' },
      signal,
    })

    if (!response.ok) {
      throw new Error(`Card search failed (${response.status} ${response.statusText}).`)
    }

    return (await response.json()) as CardSearchPage
  },
}
