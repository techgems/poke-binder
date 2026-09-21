import type { CardSearchResult } from '../clients/CardSearchClient'
import { history } from './history.svelte'

/**
 * The binder's card tray: what the user has picked out but not yet placed on a page.
 *
 * Held here rather than inside a component because two places need the same answer — the tray
 * panel lists it, and every result tile asks whether the card it shows is already in it. Nothing
 * is sent to the server yet; this is the client's copy, and the shape it keeps (a card and a
 * count, keyed by card) is the shape SaveBinderChanges takes when it is wired up — the tray
 * travels whole, with the pages of the spread being edited.
 */

export interface TrayEntry {
  card: CardSearchResult
  quantity: number
}

/**
 * Copies of one card, and distinct cards in a tray.
 *
 * **These are the only ceilings there are.** The server used to state its own and no longer does:
 * how big a tray is worth having is a question about the workspace, not about the table, and two
 * declarations of it were two things to keep in step. So nothing behind these enforces them — a
 * tray that gets past them is saved as it stands, and the numbers are here to keep the UI honest
 * rather than to keep the database safe. The practical ceiling is far lower than either.
 */
export const MAX_QUANTITY = 999

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

/**
 * Sets one row's count, inserting the row when it is coming back and dropping it when the count
 * reaches zero -- a tray entry of nothing is not a thing.
 *
 * The one write every change to the tray goes through, whether it is an action the user took or an
 * undo replaying one. History passes values the tray was already in, which is why this clamps
 * nothing: `clamp` and `MAX_CARDS` guard what the user can reach for, not what they already had.
 */
export function applyTrayQuantity(at: number, card: CardSearchResult, quantity: number): void {
  if (quantity === 0) {
    entries = entries.filter((_, index) => index !== at)

    return
  }

  if (entries[at]?.card.id === card.id) {
    entries = entries.map((entry, index) => (index === at ? { ...entry, quantity } : entry))

    return
  }

  entries = [...entries.slice(0, at), { card, quantity }, ...entries.slice(at)]
}

/** The same write, recorded. Nothing changed is nothing to record. */
function change(at: number, card: CardSearchResult, from: number, to: number): void {
  if (from === to) return

  history.record({ store: 'tray', card, at, from, to })
  applyTrayQuantity(at, card, to)
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

  /**
   * Replaces the tray with what the server sent. Called once at startup from the preloaded binder,
   * before anything is rendered -- not merged with what is already here, because at that point
   * there is nothing here and a merge would only invent a rule for a case that cannot happen.
   */
  load(loaded: TrayEntry[]): void {
    // Named `loaded` rather than `entries`: the parameter would shadow the state this is meant to
    // replace, and every assignment would land back on the argument.
    entries = loaded
      .slice(0, MAX_CARDS)
      .map((entry) => ({ card: entry.card, quantity: clamp(entry.quantity) }))
      .filter((entry) => entry.quantity > 0)
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
    history.act(() => {
      const index = indexOf(card.id)

      if (index === -1) {
        if (this.isFull) return

        change(entries.length, card, 0, clamp(quantity) || 1)

        return
      }

      this.setQuantity(card.id, entries[index].quantity + quantity)
    })
  },

  /** Sets the count outright. Zero removes the card: a tray entry of nothing is not a thing. */
  setQuantity(cardId: number, quantity: number): void {
    history.act(() => {
      const index = indexOf(cardId)

      if (index === -1) return

      change(index, entries[index].card, entries[index].quantity, clamp(quantity))
    })
  },

  remove(cardId: number): void {
    history.act(() => {
      const index = indexOf(cardId)

      if (index === -1) return

      change(index, entries[index].card, entries[index].quantity, 0)
    })
  },

  clear(): void {
    history.act(() => {
      // Backwards, so each row is still where its delta says it is when the deltas before it are
      // applied -- and so undo puts them back from the top down.
      for (let at = entries.length - 1; at >= 0; at -= 1) {
        change(at, entries[at].card, entries[at].quantity, 0)
      }
    })
  },
}
