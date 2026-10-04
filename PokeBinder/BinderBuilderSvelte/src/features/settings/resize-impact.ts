import type { CardSearchResult } from '../../clients/CardSearchClient'
import { MAX_CARDS, MAX_QUANTITY, type TrayEntry } from '../../stores/tray.svelte'

/**
 * What a new grid or page count would do to the cards already placed, worked out before anything is
 * saved so the tab can ask the user only when there is something to ask.
 */

/** What the user chose to do with the placed cards when a resize moves them. */
export type ResizeChoice = 'keep' | 'sort' | 'tray' | 'drop'

export interface ResizeImpact {
  /** Cards placed in the binder now. */
  placed: number
  /** Pockets in the binder at the new size. */
  capacity: number
  /** Cards sitting in a pocket the new size no longer has. */
  stranded: number
  /** The grid changes, which moves placed cards to other pages and other places on a page. */
  gridChanged: boolean
  /** Whether any card would move at all. Nothing is asked when none would. */
  affected: boolean
  /**
   * Every placed card has a pocket at the new size, so keeping the order or sorting can lay them
   * all out.
   */
  cardsFit: boolean
  /** Every placed card fits in the tray beside what it already holds. */
  trayFits: boolean
  /** Distinct cards the tray would hold if the binder were emptied into it. */
  trayDistinctAfter: number
}

/**
 * Pages added at the end, or empty pages taken off it, move nothing. A different page size moves
 * every card that is placed, because pockets are one absolute index across the binder and a page
 * boundary is all that changes.
 */
export function resizeImpact(
  slots: readonly (CardSearchResult | null)[],
  tray: readonly TrayEntry[],
  current: { columns: number; rows: number },
  next: { columns: number; rows: number; pages: number },
): ResizeImpact {
  const capacity = next.columns * next.rows * next.pages

  const gridChanged = current.columns !== next.columns || current.rows !== next.rows

  let placed = 0
  let stranded = 0

  const quantities = new Map(tray.map((entry) => [entry.card.id, entry.quantity]))

  slots.forEach((card, index) => {
    if (!card) return

    placed += 1

    if (index >= capacity) stranded += 1

    quantities.set(card.id, (quantities.get(card.id) ?? 0) + 1)
  })

  const trayDistinctAfter = quantities.size

  return {
    placed,
    capacity,
    stranded,
    gridChanged,
    affected: placed > 0 && (gridChanged || stranded > 0),
    cardsFit: placed <= capacity,
    trayFits:
      trayDistinctAfter <= MAX_CARDS && [...quantities.values()].every((q) => q <= MAX_QUANTITY),
    trayDistinctAfter,
  }
}
