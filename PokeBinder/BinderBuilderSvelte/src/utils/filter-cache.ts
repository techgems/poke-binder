// The browser's store of the advanced-search filter options: keys, reads and writes, and nothing
// else.
//
// The five big groups -- pokemon, generations, series, sets, rarities by set -- are 141 KB that
// change a few times a year, so they live in localStorage against the stamp they were fetched
// under. What that stamp is worth, and who to ask when it no longer holds, is the dynamic preload's
// business (preloads/search-filters); this file answers only "what is stored for this group" and
// "keep this".
//
// Card types and super types are not here. They are a couple of kilobytes that ship with the page
// and are never stored, so they have no stamp and no slot.
//
// Every localStorage access is wrapped: private windows, cleared site data and a full quota all
// throw or answer empty, and none of them should do worse than make this browser fetch every time.

import type { CachedFilterGroup } from '../clients/CardSearchClient'

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

/**
 * A group as it was stored: the rows and the stamp they came back with, never one without the
 * other. Half a copy is no copy -- a stamp with no rows would have this browser claim to be
 * current on a group it does not have -- so a group missing either half reads as nothing stored.
 */
export interface StoredFilterGroup {
  rows: unknown[]
  stamp: string
}

/** What is stored for a group, or null when there is nothing usable -- blocked, absent or junk. */
export function readStoredGroup(group: CachedFilterGroup): StoredFilterGroup | null {
  const raw = readItem(rowsKey(group))

  if (raw === null) return null

  let rows: unknown[]

  try {
    const parsed: unknown = JSON.parse(raw)

    // A half-written or hand-edited entry reads as junk rather than as an empty filter list.
    if (!Array.isArray(parsed)) return null

    rows = parsed
  } catch {
    forgetGroup(group)

    return null
  }

  const stamp = readItem(stampKey(group))

  return stamp === null ? null : { rows, stamp }
}

/**
 * Stores one group. The rows go in first and the stamp second, and the order is the whole point: a
 * quota failure between them leaves a stamp-less copy, which the next load treats as missing and
 * re-fetches. Written the other way round, the same failure would leave this browser holding a
 * stamp it believes is current with nothing behind it.
 */
export function storeGroup(group: CachedFilterGroup, rows: unknown[], stamp: string): void {
  try {
    window.localStorage.setItem(rowsKey(group), JSON.stringify(rows))
    window.localStorage.setItem(stampKey(group), stamp)
  } catch {
    // Out of room, or storage is refusing writes. Drop both halves so nothing half-stored is left
    // to be believed, and let the next load fetch again.
    forgetGroup(group)
  }
}

/** Forgets a group, for the case where the catalog has no stamp for it and it cannot be cached. */
export function forgetGroup(group: CachedFilterGroup): void {
  removeItem(rowsKey(group))
  removeItem(stampKey(group))
}
