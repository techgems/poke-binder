import type { CardSearchResult } from '../clients/CardSearchClient'
import {
  applyArrangement,
  applySlotCard,
  binderPage,
  type Arrangement,
} from './binder-page.svelte'
import { save } from './save.svelte'
import { applyTrayQuantity } from './tray.svelte'

/**
 * What the user did, so Undo and Redo have something to walk.
 *
 * An entry records what *changed*, not what the binder looked like: a snapshot per step would copy
 * every pocket in the binder to describe one of them changing. Every edit in the workspace is a
 * before and an after of one thing rather than a verb -- a tray row, a pocket, or for a reorder the
 * whole binder -- and undo applies every before while redo applies every after, so one replay
 * serves both directions and no verb needs an inverse written for it. The reorder is the one place
 * a snapshot is the right size, because it is the one edit that changes every pocket.
 *
 * Entries are pushed from inside the store methods rather than from the components that call them,
 * so an action records itself however it was triggered and the history is complete as long as
 * `tray.svelte.ts` and `binder-page.svelte.ts` are. **A new mutating call is a new recording in the
 * same commit**: an edit that reaches the tray or the slots without one leaves the stack describing
 * a binder that no longer exists, and undo then restores a state the user was never in.
 *
 * Saving inherits that argument for free, which is why it is armed from here rather than from the
 * stores: an action that records itself schedules its own save -- except a reorder, which the
 * server stored before it was recorded (see `recordStored`). All three events do it -- a commit,
 * an undo and a redo -- because all three change what is in the binder, and each hands over the
 * pockets its entry touched so the save claims the pages it actually changed rather than the ones
 * on screen. `clear` does not, and deliberately does not cancel a save already armed: the edits
 * before a clear still happened.
 */

/** A change to one tray row: the card, the row it sits on, and its count before and after. */
export interface TrayDelta {
  store: 'tray'
  card: CardSearchResult
  /**
   * Which row. The tray is insertion-ordered and `tray.add` appends, so undoing a removal without
   * this would put the card back at the bottom of a list it was never at the bottom of.
   */
  at: number
  from: number
  to: number
}

/** A change to one pocket, addressed by the absolute index the stores and the server both use. */
export interface SlotDelta {
  store: 'slot'
  index: number
  from: CardSearchResult | null
  to: CardSearchResult | null
}

/**
 * A reorder: the whole binder before it and after it, as two snapshots.
 *
 * The one entry that is not a per-pocket change. A reorder can move every pocket in the binder, and
 * the arrangement it replaced exists nowhere else once the server has stored the new one, so the
 * entry keeps both states outright -- undo lays `before` back out, redo lays `after` -- missing
 * flags included, since they travel with their cards.
 */
export interface ArrangementDelta {
  store: 'arrangement'
  before: Arrangement
  after: Arrangement
}

export type Delta = TrayDelta | SlotDelta | ArrangementDelta

/** One thing the user did, however many edits across the two stores it took. */
export interface HistoryEntry {
  deltas: Delta[]
}

/**
 * How far back Undo reaches. A delta is a handful of fields and a reference to a card the stores
 * are holding anyway, so this is a ceiling for a long session rather than a number anyone reaches.
 * A reorder's entry is two snapshots of the binder, a small object per pocket each, and that is
 * still nothing a tab notices.
 */
export const MAX_ENTRIES = 100

let entries = $state<HistoryEntry[]>([])

// How many of them are currently applied. Undo moves this down rather than dropping anything, so
// everything past it is the redo tail, thrown away by the next action.
let applied = $state(0)

// The deltas of the action in progress. Actions nest -- dropping a card on a pocket calls into the
// tray twice -- so edits accumulate here and the outermost call commits them as one entry.
let buffer: Delta[] = []
let depth = 0

// Set while undo or redo is writing, so the replay cannot record itself.
let replaying = false

function push(entry: HistoryEntry): void {
  // Anything undone and not redone is gone the moment the user does something else: the future it
  // described is not the one they are in any more.
  const kept = [...entries.slice(0, applied), entry]

  entries = kept.length > MAX_ENTRIES ? kept.slice(kept.length - MAX_ENTRIES) : kept
  applied = entries.length
}

function commit(): void {
  if (buffer.length === 0) return

  const entry: HistoryEntry = { deltas: buffer }

  buffer = []

  push(entry)

  save.arm(slotIndexes(entry.deltas))
}

/** Whether two snapshots disagree about a pocket: a different card, or the same one flagged differently. */
function pocketDiffers(arrangement: ArrangementDelta, index: number): boolean {
  const before = arrangement.before[index] ?? null
  const after = arrangement.after[index] ?? null

  return before?.card.id !== after?.card.id || (before?.missing ?? false) !== (after?.missing ?? false)
}

/**
 * Every pocket an entry changed. Empty for an entry that only touched the tray, which is a save
 * with no page of its own to name -- see `save.arm`. For a reorder, the pockets the two snapshots
 * disagree about: usually most of the binder, which is what makes its undo claim every page.
 */
function slotIndexes(deltas: readonly Delta[]): number[] {
  return deltas.flatMap((delta) => {
    if (delta.store === 'slot') return [delta.index]

    if (delta.store === 'arrangement') {
      const length = Math.max(delta.before.length, delta.after.length)

      return Array.from({ length }, (_, index) => index).filter((index) => pocketDiffers(delta, index))
    }

    return []
  })
}

/** The pocket an entry should be looking at, or -1 for an entry that touched no pocket. */
function firstSlotIndex(deltas: readonly Delta[]): number {
  for (const delta of deltas) {
    if (delta.store === 'slot') return delta.index

    // Its earliest changed pocket, which for a reorder is the front of the binder.
    if (delta.store === 'arrangement') return slotIndexes([delta])[0] ?? -1
  }

  return -1
}

function replay(entry: HistoryEntry, direction: 'undo' | 'redo'): void {
  const ordered = direction === 'undo' ? [...entry.deltas].reverse() : entry.deltas
  const pocket = firstSlotIndex(ordered)

  // Before the write, not after: a press that silently edits page eleven while the user is on page
  // two reads as a button that did nothing.
  if (pocket >= 0) binderPage.showIndex(pocket)

  replaying = true

  try {
    for (const delta of ordered) {
      if (delta.store === 'tray') {
        applyTrayQuantity(delta.at, delta.card, direction === 'undo' ? delta.from : delta.to)
      } else if (delta.store === 'arrangement') {
        applyArrangement(direction === 'undo' ? delta.before : delta.after)
      } else {
        applySlotCard(delta.index, direction === 'undo' ? delta.from : delta.to)
      }
    }
  } finally {
    replaying = false
  }
}

export const history = {
  /** Read inside a component or a `$derived` to stay reactive. */
  get canUndo(): boolean {
    return applied > 0
  },

  get canRedo(): boolean {
    return applied < entries.length
  },

  /** How many actions Undo can still walk back. */
  get depth(): number {
    return applied
  },

  /**
   * Runs one user action, committing everything it changed as a single entry.
   *
   * Every mutating method on the two stores wraps its body in this, including the ones that reach
   * into each other -- a drop that spends a copy out of the tray is one press of Undo, not three,
   * and only the outermost call closes the entry.
   */
  act<T>(run: () => T): T {
    depth += 1

    try {
      return run()
    } finally {
      depth -= 1

      if (depth === 0) commit()
    }
  },

  /**
   * Records one edit. Called by the stores as they make it, and ignored while undo or redo is
   * writing, since a replay is not something the user did.
   */
  record(delta: Delta): void {
    if (replaying) return

    buffer.push(delta)

    // A mutating method that forgot its `act` wrapper still records, as an entry of its own. The
    // history stays sound; only the grouping is wrong, which is the failure worth having.
    if (depth === 0) commit()
  },

  /**
   * Records an action the server has already stored -- a reorder, whose request both did it and
   * saved it. Its own entry, never merged into an action in progress, and it arms no save: there is
   * nothing to save until an undo or a redo moves the binder away from what is stored, and both of
   * those arm one like any other.
   */
  recordStored(delta: Delta): void {
    push({ deltas: [delta] })
  },

  undo(): void {
    if (!this.canUndo) return

    const entry = entries[applied - 1]

    replay(entry, 'undo')

    applied -= 1

    // Undo pushes nothing, so `commit` never runs for it and the save has to be armed here. The
    // replay turned to the page it changed before writing, so the spread on screen is the spread
    // being saved either way -- but the entry knows which pockets it touched, and that is what the
    // request should claim.
    save.arm(slotIndexes(entry.deltas))
  },

  redo(): void {
    if (!this.canRedo) return

    const entry = entries[applied]

    replay(entry, 'redo')

    applied += 1

    save.arm(slotIndexes(entry.deltas))
  },

  /**
   * Throws the history away. For the events undo cannot reach back across -- loading a binder, and
   * deleting one once that button is wired.
   */
  clear(): void {
    entries = []
    applied = 0
    buffer = []
  },
}
