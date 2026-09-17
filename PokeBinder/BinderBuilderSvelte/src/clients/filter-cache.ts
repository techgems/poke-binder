// The browser's copy of the advanced-search filter options.
//
// The five big groups -- pokemon, generations, series, sets, rarities by set -- are 141 KB that
// change a few times a year, so they live in localStorage and are re-fetched only when the catalog
// says they moved. The page embeds a stamp per group; this module compares those against the stamps
// stored beside each cached copy and asks the server for the ones that disagree, which on a warm
// load is none of them and costs no request at all.
//
// Card types and super types are not here. They are a couple of kilobytes that ship with the page
// and are never stored, so they have no stamp and no slot.
//
// Every localStorage access is wrapped: private windows, cleared site data and a full quota all
// throw or answer empty, and none of them should do worse than make this browser fetch every time.

import {
  CACHED_FILTER_GROUPS,
  CardSearchClient,
  type CachedFilterGroup,
  type FilterStamps,
  type StarterFilters,
  type StarterFiltersResponse,
} from './CardSearchClient'
import { preloadedSearchFilters } from './preload'

/**
 * One namespace, two keys per group: the rows, and the stamp they were fetched under. Separate
 * keys rather than one object so a warm load can compare five short strings without parsing
 * 141 KB of JSON it may not need.
 *
 * This is catalog data, not user data. It is shared by every account that signs in on this device
 * and survives signing out, which is correct -- the card catalog is the same catalog for everyone
 * -- and is the reason there is no user id in these keys.
 */
const KEY_PREFIX = 'pokeBinder.searchFilters.'

const rowsKey = (group: CachedFilterGroup) => `${KEY_PREFIX}${group}`
const stampKey = (group: CachedFilterGroup) => `${KEY_PREFIX}${group}.stamp`

function readItem(key: string): string | null {
  try {
    return window.localStorage.getItem(key)
  } catch {
    // Blocked storage. Nothing is cached, everything is fetched, and the app is none the wiser.
    return null
  }
}

function removeItem(key: string): void {
  try {
    window.localStorage.removeItem(key)
  } catch {
    // Nothing to do: a copy that cannot be removed is a copy whose stamp will not match either.
  }
}

/** Stored rows for a group, or null when there are none to be had -- blocked, absent or unreadable. */
function readRows(group: CachedFilterGroup): unknown[] | null {
  const raw = readItem(rowsKey(group))

  if (raw === null) return null

  try {
    const parsed: unknown = JSON.parse(raw)

    // A half-written or hand-edited entry reads as junk rather than as an empty filter list.
    return Array.isArray(parsed) ? parsed : null
  } catch {
    removeItem(rowsKey(group))
    removeItem(stampKey(group))

    return null
  }
}

/**
 * Stores one group. The rows go in first and the stamp second, and the order is the whole point: a
 * quota failure between them leaves a stamp-less copy, which the next load treats as missing and
 * re-fetches. Written the other way round, the same failure would leave this browser holding a
 * stamp it believes is current with nothing behind it.
 */
function writeGroup(group: CachedFilterGroup, rows: unknown[], stamp: string): void {
  try {
    window.localStorage.setItem(rowsKey(group), JSON.stringify(rows))
    window.localStorage.setItem(stampKey(group), stamp)
  } catch {
    // Out of room, or storage is refusing writes. Drop both halves so nothing half-stored is left
    // to be believed, and let the next load fetch again.
    removeItem(rowsKey(group))
    removeItem(stampKey(group))
  }
}

/** Forgets a group, for the case where the catalog has no stamp for it and it cannot be cached. */
function forgetGroup(group: CachedFilterGroup): void {
  removeItem(rowsKey(group))
  removeItem(stampKey(group))
}

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

export interface CachedSearchFilters {
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
 * wait on, which is also why this is localStorage and not IndexedDB.
 */
export function loadCachedSearchFilters(): CachedSearchFilters {
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
    const rows = readRows(group)
    const stamp = rows ? readItem(stampKey(group)) : null

    // Only ever claim a stamp for rows that are actually here. A stamp sent without its rows would
    // have the server agree this browser is current and send nothing back, leaving the group empty
    // for good.
    if (rows && stamp) held[group] = stamp

    const current = page.stamps?.[group] ?? null

    // Current is null for a group the catalog has no stamp row for: uncacheable, so whatever is
    // stored is unprovable and the group is fetched again.
    if (rows && stamp && current !== null && stamp === current) {
      filters[group] = rows as never
    } else {
      stale.push(group)
    }
  }

  return { filters, stale, held, hasPageFilters: true }
}

/**
 * Fetches the stale groups, stores them, and returns the completed options.
 *
 * The response decides what is stored, not the request: the server compares the stamps again and
 * may send a group this browser thought was current, or leave one out that it thought was stale.
 * Either way, a group is stored only alongside the stamp it came back with.
 */
export async function refreshSearchFilters(
  cached: CachedSearchFilters,
  signal?: AbortSignal,
): Promise<StarterFilters> {
  const response: StarterFiltersResponse = await CardSearchClient.getStarterFilters(
    cached.held,
    signal,
  )

  const filters: StarterFilters = { ...cached.filters }

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
      writeGroup(group, rows, stamp)
    } else {
      // No stamp means nothing could ever mark this copy stale, so it is used for this session and
      // deliberately not kept. Any older copy goes with it.
      forgetGroup(group)
    }
  }

  return filters
}
