// The advanced search's filter options, assembled from everywhere they can come from.
//
// A dynamic preload: it fills the same role as preload.ts -- hand the app data it can use before it
// asks for any -- but where that one only reads what the Razor page wrote onto the document, this
// one also goes and gets what the document did not carry.
//
// Three sources, in this order. The page embeds card types and super types, plus a stamp per cached
// group. Storage (utils/filter-cache) holds the five big groups against the stamp they were
// fetched under. Anything whose stored stamp disagrees with the page's is fetched from the client
// and stored on the way through, which on a warm load is nothing at all and costs no request.

import {
  CACHED_FILTER_GROUPS,
  CardSearchClient,
  type CachedFilterGroup,
  type FilterStamps,
  type StarterFilters,
  type StarterFiltersResponse,
} from '../clients/CardSearchClient'
import { forgetGroup, readStoredGroup, storeGroup } from '../utils/filter-cache'
import { preloadedSearchFilters } from './preload'

/** An empty set of options: what the UI renders before anything has arrived. */
function emptyFilters(): StarterFilters {
  return {
    superTypes: [],
    generations: [],
    series: [],
    sets: [],
    pokemon: [],
    rarityBySet: [],
    cardType: [],
  }
}

export interface SearchFiltersPreload {
  /** Everything known without asking the server: the page's two groups plus whatever was stored. */
  filters: StarterFilters
  /** Groups with no usable copy, or one the catalog has since stamped differently. */
  stale: CachedFilterGroup[]
  /**
   * The stamps to send, covering only groups whose rows are actually in hand. Null means this
   * browser has nothing at all and should ask for everything.
   */
  held: Partial<FilterStamps> | null
  /** Whether the page embedded its half. False means the whole object has to be fetched. */
  hasPageFilters: boolean
}

/**
 * What this browser can show before it has asked the server anything. Synchronous on purpose: the
 * filters are in hand for the first paint instead of behind an async gate the panel would have to
 * wait on, which is also why the store behind it is localStorage and not IndexedDB.
 */
export function preloadSearchFilters(): SearchFiltersPreload {
  const page = preloadedSearchFilters()

  if (!page) {
    // No page payload, so no stamps to compare and no card types either. Nothing stored can be
    // trusted against a catalog this browser cannot see, so it starts from nothing.
    return { filters: emptyFilters(), stale: [...CACHED_FILTER_GROUPS], held: null, hasPageFilters: false }
  }

  const filters = emptyFilters()

  filters.superTypes = page.superTypes ?? []
  filters.cardType = page.cardType ?? []

  const stale: CachedFilterGroup[] = []
  const held: Partial<FilterStamps> = {}

  for (const group of CACHED_FILTER_GROUPS) {
    const stored = readStoredGroup(group)

    // Only ever claim a stamp for rows that are actually here. A stamp sent without its rows would
    // have the server agree this browser is current and send nothing back, leaving the group empty
    // for good.
    if (stored) held[group] = stored.stamp

    const current = page.stamps?.[group] ?? null

    // Current is null for a group the catalog has no stamp row for: uncacheable, so whatever is
    // stored is unprovable and the group is fetched again.
    if (stored && current !== null && stored.stamp === current) {
      filters[group] = stored.rows as never
    } else {
      stale.push(group)
    }
  }

  return { filters, stale, held, hasPageFilters: true }
}

/**
 * Fetches the groups the preload could not account for, stores them, and returns the completed
 * options.
 *
 * The response decides what is stored, not the request: the server compares the stamps again and
 * may send a group this browser thought was current, or leave one out that it thought was stale.
 * Either way, a group is stored only alongside the stamp it came back with.
 */
export async function fetchStaleSearchFilters(
  preloaded: SearchFiltersPreload,
  signal?: AbortSignal,
): Promise<StarterFilters> {
  const response: StarterFiltersResponse = await CardSearchClient.getStarterFilters(
    preloaded.held,
    signal,
  )

  const filters: StarterFilters = { ...preloaded.filters }

  // Only sent when this browser had none of them -- the page carries them on every other load.
  if (response.superTypes) filters.superTypes = response.superTypes
  if (response.cardType) filters.cardType = response.cardType

  for (const group of CACHED_FILTER_GROUPS) {
    const rows = response[group]

    // Null is "you already have this", so the copy in hand stands.
    if (!rows) continue

    filters[group] = rows as never

    const stamp = response.stamps?.[group] ?? null

    if (stamp) {
      storeGroup(group, rows, stamp)
    } else {
      // No stamp means nothing could ever mark this copy stale, so it is used for this session and
      // deliberately not kept. Any older copy goes with it.
      forgetGroup(group)
    }
  }

  return filters
}
