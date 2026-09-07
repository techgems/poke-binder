import type { CardSearchResult } from '../clients/CardSearchClient'

/**
 * The binder's card tray: what the user has picked out but not yet placed on a page.
 *
 * Held here rather than inside a component because two places need the same answer — the tray
 * panel lists it, and every result tile asks whether the card it shows is already in it. Nothing
 * is sent to the server yet; this is the client's copy, and the shape it keeps (a card and a
 * count, keyed by card) is the shape SaveBinderTray takes when it is wired up.
 */

export interface TrayEntry {
  card: CardSearchResult
  quantity: number
}

/**
 * Copies of one card. Mirrors SaveBinderTray.MaxQuantity so the UI cannot build a tray the server
 * would refuse; the practical ceiling is far lower than this.
 */
export const MAX_QUANTITY = 999

/** Distinct cards in a tray. Mirrors SaveBinderTray.MaxCards for the same reason. */
export const MAX_CARDS = 500

// Insertion-ordered, like a shopping cart: a card the user just picked belongs at the end of the
// list, not sorted into the middle of it where they have to hunt for it.
let entries = $state<TrayEntry[]>([])

function indexOf(cardId: number): number {
  return entries.findIndex((entry) => entry.card.id === cardId)
}

function clamp(quantity: number): number {
  return Math.min(Math.max(Math.round(quantity), 0), MAX_QUANTITY)
}

export const tray = {
  /** The tray in order. Read inside a component or $derived to stay reactive. */
  get entries(): readonly TrayEntry[] {
    return entries
  },

  /** Distinct cards. */
  get cardCount(): number {
    return entries.length
  },

  /** Copies in total — what the panel counts, since three of one card is three cards to place. */
  get totalQuantity(): number {
    return entries.reduce((total, entry) => total + entry.quantity, 0)
  },

  get isEmpty(): boolean {
    return entries.length === 0
  },

  /** No room for another distinct card. Adding more of one already in the tray is still fine. */
  get isFull(): boolean {
    return entries.length >= MAX_CARDS
  },

  /** Copies of this card in the tray, or zero when it is not in it. */
  quantityOf(cardId: number): number {
    return entries[indexOf(cardId)]?.quantity ?? 0
  },

  /**
   * Adds copies of a card, or raises the count if it is already in the tray. The card itself is
   * kept so the tray can render a row for it without going back to the search results, which the
   * user may well have replaced by then.
   */
  add(card: CardSearchResult, quantity = 1): void {
    const index = indexOf(card.id)

    if (index === -1) {
      if (this.isFull) return

      entries = [...entries, { card, quantity: clamp(quantity) || 1 }]

      return
    }

    this.setQuantity(card.id, entries[index].quantity + quantity)
  },

  /** Sets the count outright. Zero removes the card: a tray entry of nothing is not a thing. */
  setQuantity(cardId: number, quantity: number): void {
    const index = indexOf(cardId)

    if (index === -1) return

    const next = clamp(quantity)

    if (next === 0) {
      this.remove(cardId)

      return
    }

    entries = entries.map((entry, at) => (at === index ? { ...entry, quantity: next } : entry))
  },

  remove(cardId: number): void {
    entries = entries.filter((entry) => entry.card.id !== cardId)
  },

  clear(): void {
    entries = []
  },
}
