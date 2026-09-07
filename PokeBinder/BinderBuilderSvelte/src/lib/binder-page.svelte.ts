import type { CardSearchResult } from '../clients/CardSearchClient'
import { tray } from './tray.svelte'

/**
 * The cards placed on the binder page, and the drag in progress between them and the tray.
 *
 * The two belong together: every move is a transfer between the page and the tray, so the rules
 * about what a copy costs live in one place instead of being restated by whichever component
 * happens to be handling the drop. Nothing is sent to the server yet.
 */

/** Slots on one page. A 3x3 sheet, hardcoded until the real binder size arrives. */
export const SLOTS_PER_PAGE = 9

/** Where a dragged card came from, which is what decides what dropping it means. */
export type DragSource =
  | { kind: 'tray'; card: CardSearchResult }
  | { kind: 'slot'; card: CardSearchResult; index: number }

let slots = $state<(CardSearchResult | null)[]>(Array(SLOTS_PER_PAGE).fill(null))

// The drag lives here rather than in the DataTransfer because dragover has to decide whether a
// drop is allowed, and the payload cannot be read until the drop itself.
let dragged = $state<DragSource | null>(null)

export const binderPage = {
  /** Slot contents in order; null is an empty pocket. */
  get slots(): readonly (CardSearchResult | null)[] {
    return slots
  },

  /** The card being dragged, or null. Read to light up whatever could receive it. */
  get dragged(): DragSource | null {
    return dragged
  },

  get isDraggingFromSlot(): boolean {
    return dragged?.kind === 'slot'
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

    if (!source || index < 0 || index >= SLOTS_PER_PAGE) return

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
   * Puts a card in the first free pocket, again as a keyboard-reachable stand-in for dragging it
   * there. Does nothing when the page is full: silently dropping the request is better than
   * displacing a card the user never pointed at.
   */
  placeInFirstFreeSlot(card: CardSearchResult): boolean {
    const index = slots.indexOf(null)

    if (index === -1) return false

    slots[index] = card
    tray.setQuantity(card.id, tray.quantityOf(card.id) - 1)

    return true
  },
}
