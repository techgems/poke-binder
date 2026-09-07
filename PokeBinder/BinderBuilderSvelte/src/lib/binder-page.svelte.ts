import type { CardSearchResult } from '../clients/CardSearchClient'
import type { PreloadedBinder } from '../clients/preload'
import { tray } from './tray.svelte'

/**
 * The cards placed in the binder, and the drag in progress between them and the tray.
 *
 * The two belong together: every move is a transfer between the binder and the tray, so the rules
 * about what a copy costs live in one place instead of being restated by whichever component
 * happens to be handling the drop.
 *
 * Slots cover the *whole* binder, not the spread on screen. SaveBinderCards takes a snapshot of the
 * whole binder — a payload holding one page tells it every other page is empty — so the client has
 * to hold the whole thing to be able to save at all.
 */

/** The grid used before a binder is loaded: a plain 3x3 sheet of one page. */
const DEFAULT_COLUMNS = 3
const DEFAULT_ROWS = 3

/** One pocket on screen: the card in it, and the index the binder and the server know it by. */
export interface BinderSlotView {
  index: number
  card: CardSearchResult | null
}

/** One page on screen. */
export interface BinderPageView {
  pageNumber: number
  slots: BinderSlotView[]
}

/** Where a dragged card came from, which is what decides what dropping it means. */
export type DragSource =
  | { kind: 'tray'; card: CardSearchResult }
  | { kind: 'slot'; card: CardSearchResult; index: number }

let columns = $state(DEFAULT_COLUMNS)
let rows = $state(DEFAULT_ROWS)
let pages = $state(1)
let slots = $state<(CardSearchResult | null)[]>(Array(DEFAULT_COLUMNS * DEFAULT_ROWS).fill(null))

// Which spread is open, counted from zero. A binder opens on one page and then falls open in
// pairs, so spread 0 is page 1 alone and spread n is pages 2n and 2n+1 -- the same way the sheets
// are actually bound.
let spread = $state(0)

// The drag lives here rather than in the DataTransfer because dragover has to decide whether a
// drop is allowed, and the payload cannot be read until the drop itself.
let dragged = $state<DragSource | null>(null)

export const binderPage = {
  /** Pockets across a page. */
  get columns(): number {
    return columns
  },

  /** Pockets down a page. */
  get rows(): number {
    return rows
  },

  get cardsPerPage(): number {
    return columns * rows
  },

  get pages(): number {
    return pages
  },

  /** Every pocket in the binder, in order. */
  get slots(): readonly (CardSearchResult | null)[] {
    return slots
  },

  /** Which spread is open, counted from zero. */
  get spread(): number {
    return spread
  },

  /**
   * How many spreads the binder has. Page one is a spread of its own, and the rest pair up, so an
   * even page count leaves the last page facing nothing -- which is what a real binder does too.
   */
  get spreadCount(): number {
    return pages <= 1 ? 1 : 1 + Math.ceil((pages - 1) / 2)
  },

  /**
   * The one or two pages on screen, each with the absolute pocket indexes the rest of this module
   * and the server both address them by.
   */
  get spreadPages(): readonly BinderPageView[] {
    if (spread <= 0) {
      return [this.pageAt(1)]
    }

    const left = spread * 2

    return left >= pages ? [this.pageAt(left)] : [this.pageAt(left), this.pageAt(left + 1)]
  },

  /** One page's pockets, by page number counted from one. */
  pageAt(pageNumber: number): BinderPageView {
    const start = (pageNumber - 1) * this.cardsPerPage

    return {
      pageNumber,
      slots: Array.from({ length: this.cardsPerPage }, (_, offset) => ({
        index: start + offset,
        card: slots[start + offset] ?? null,
      })),
    }
  },

  /** Every pocket on screen, both pages of a spread together. */
  get visibleSlots(): readonly BinderSlotView[] {
    return this.spreadPages.flatMap((page) => page.slots)
  },

  /** "Page 1 of 52", or "Pages 2-3 of 52" once the binder falls open in pairs. */
  get spreadLabel(): string {
    const shown = this.spreadPages

    return shown.length === 1
      ? `Page ${shown[0].pageNumber} of ${pages}`
      : `Pages ${shown[0].pageNumber}-${shown[1].pageNumber} of ${pages}`
  },

  get canGoBack(): boolean {
    return spread > 0
  },

  get canGoForward(): boolean {
    return spread < this.spreadCount - 1
  },

  goBack(): void {
    if (this.canGoBack) spread -= 1
  },

  goForward(): void {
    if (this.canGoForward) spread += 1
  },

  get dragged(): DragSource | null {
    return dragged
  },

  get isDraggingFromSlot(): boolean {
    return dragged?.kind === 'slot'
  },

  /**
   * Takes the binder the Razor page handed over: its shape, its cards, and the tray that came with
   * it. Called once at startup, before anything is rendered, so the first paint is the real binder
   * rather than an empty sheet that fills in afterwards.
   */
  load(preloaded: PreloadedBinder): void {
    const summary = preloaded.binder

    if (!summary) return

    columns = summary.x
    rows = summary.y
    pages = summary.pages
    spread = 0

    const placed: (CardSearchResult | null)[] = Array(summary.cardCount).fill(null)

    for (const placement of preloaded.cards) {
      // A card the catalog no longer has, or a pocket outside the binder's current size — the
      // binder may have been shrunk since. Either way there is nothing to draw, and dropping it
      // here is better than rendering a hole halfway down the page.
      if (!placement.card || placement.indexInBinder >= placed.length) continue

      placed[placement.indexInBinder] = placement.card
    }

    slots = placed

    tray.load(
      preloaded.tray
        .filter((entry) => entry.card !== null)
        .map((entry) => ({ card: entry.card as CardSearchResult, quantity: entry.quantity })),
    )
  },

  startDrag(source: DragSource): void {
    dragged = source
  },

  endDrag(): void {
    dragged = null
  },

  /**
   * Drops the card being dragged into a pocket.
   *
   * From the tray it costs a copy; from another pocket the two swap, which is what makes
   * rearranging a page possible without emptying it first. Either way a card already in the target
   * pocket goes back to the tray rather than disappearing.
   */
  dropOnSlot(index: number): void {
    const source = dragged

    dragged = null

    if (!source || index < 0 || index >= slots.length) return

    if (source.kind === 'slot') {
      if (source.index === index) return

      // A straight exchange: whatever was in the target lands where the dragged card came from,
      // which for an empty target is the same as moving it.
      const displaced = slots[index]
      slots[index] = source.card
      slots[source.index] = displaced

      return
    }

    const displaced = slots[index]

    if (displaced) {
      tray.add(displaced)
    }

    slots[index] = source.card

    // Placing spends one of the copies waiting in the tray; setQuantity drops the entry when the
    // last one leaves.
    tray.setQuantity(source.card.id, tray.quantityOf(source.card.id) - 1)
  },

  /** Drops the card being dragged back into the tray. Only a card from a pocket has anywhere to go. */
  dropOnTray(): void {
    const source = dragged

    dragged = null

    if (source?.kind !== 'slot') return

    slots[source.index] = null

    tray.add(source.card)
  },

  /**
   * Takes a card out of a pocket without a drag — the same transfer the drop above performs, for
   * anyone not using a mouse.
   */
  returnToTray(index: number): void {
    const card = slots[index]

    if (!card) return

    slots[index] = null

    tray.add(card)
  },

  /**
   * Puts a card in the first free pocket on screen, again as a keyboard-reachable stand-in for
   * dragging it there. Scoped to the open spread rather than the whole binder: a card that vanished
   * onto page eleven would be worse than one that went nowhere.
   */
  placeInFirstFreeSlot(card: CardSearchResult): boolean {
    const free = this.visibleSlots.find((slot) => slot.card === null)

    if (!free) return false

    slots[free.index] = card
    tray.setQuantity(card.id, tray.quantityOf(card.id) - 1)

    return true
  },
}
