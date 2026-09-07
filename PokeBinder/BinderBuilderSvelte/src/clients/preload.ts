// Data the Razor page put on the document before the app booted.
//
// The page writes each payload into window.pokeBinder under a key (see Components/ServerPreload).
// Nothing type-checks that boundary — the server serialises a C# object and the browser hands back
// `any` — so the cast lives here, once, next to the types it claims to produce. Everywhere else
// reads a typed value and never touches window.
//
// These interfaces mirror the projection GetFullBinder returns, in the camelCase the serializer
// writes. They are the same shapes the API would return for the same slice, which is the point:
// preloaded and fetched data are interchangeable.

/** A card as the binder draws it. Same shape as CardSearchResult, deliberately. */
export interface PreloadedCard {
  id: number
  name: string | null
  rarity: string | null
  cardNumber: string | null
  tcgPlayerId: number
  imageUrl: string | null
  setName: string | null
}

export interface PreloadedPlacement {
  /** The pocket, counted from zero across the whole binder. */
  indexInBinder: number
  /** Which page it is on, counted from one. */
  page: number
  /** Its place on that page, counted from zero. */
  slotOnPage: number
  isMissing: boolean
  /** Null when the catalog no longer has the card the binder points at. */
  card: PreloadedCard | null
}

export interface PreloadedTrayCard {
  quantity: number
  card: PreloadedCard | null
}

export interface PreloadedBinderSummary {
  id: number
  name: string
  description: string | null
  /** Unix time in seconds, UTC. */
  createdAt: number
  pages: number
  binderSizeId: number
  sizeName: string
  sizeDescription: string
  /** Pockets across a page. */
  x: number
  /** Pockets down a page. */
  y: number
  cardsPerPage: number
  /** Pockets in the whole binder. */
  cardCount: number
}

/** The GetFullBinder response, as the page embedded it. */
export interface PreloadedBinder {
  found: boolean
  binder: PreloadedBinderSummary | null
  cards: PreloadedPlacement[]
  tray: PreloadedTrayCard[]
}

/** The namespace the page writes into. One object, so pages add keys rather than globals. */
interface PreloadNamespace {
  binder?: unknown
}

declare global {
  interface Window {
    pokeBinder?: PreloadNamespace
  }
}

/**
 * The binder this page was opened on, or null when it was opened without one — /Binder with no
 * binderId, which is a legitimate way to arrive.
 *
 * Read once at startup rather than watched: the page hands this over exactly once, and anything
 * that changes afterwards is the app's own state, not the server's.
 */
export function preloadedBinder(): PreloadedBinder | null {
  const payload = window.pokeBinder?.binder

  // Guarded rather than cast blind: a stale bundle against a newer page, or a payload that failed
  // to serialise, should leave the app empty rather than half-initialised from a shape it cannot
  // read.
  if (!payload || typeof payload !== 'object') {
    return null
  }

  const binder = payload as PreloadedBinder

  return binder.found && binder.binder ? binder : null
}
