/**
 * The binder's labels and which pockets they are on.
 *
 * **A label belongs to the binder, not to a card.** Create one from the Label modal (or the
 * Settings Labels panel), apply it to as many cards as the collector wants, rename or recolor it
 * in Settings and every chip in the binder follows, delete it there and every assignment of it
 * goes with it. Up to three labels per card.
 *
 * **This is the substep-1 store, UI only.** Nothing here reaches the server or the save flow -- a
 * reload drops every label. The substeps that follow add the join table, the slice and the
 * client wiring; the slot delta will then carry the per-pocket label ids the way it already
 * carries the missing flag, and these explicit `clearPocket` / `swapPockets` calls move into
 * `changeSlot`.
 */

import { SvelteMap } from 'svelte/reactivity'

import type { CardSearchResult } from '../clients/CardSearchClient'
import { binderPage } from './binder-page.svelte'

/**
 * The eight colour names a label can take. Stored as names rather than hex so the swatches can be
 * restyled without rewriting any stored row. The components map each to its Tailwind classes.
 */
export type LabelColor =
  | 'red'
  | 'orange'
  | 'amber'
  | 'green'
  | 'teal'
  | 'blue'
  | 'purple'
  | 'pink'

export const LABEL_COLORS: readonly LabelColor[] = [
  'red',
  'orange',
  'amber',
  'green',
  'teal',
  'blue',
  'purple',
  'pink',
]

/** The longest tag that fits in the chip at small pocket sizes without wrapping or clipping. */
export const MAX_LABEL_LENGTH = 20

/** How many labels one pocket may carry at once. The schema's CHECK will enforce the same cap. */
export const MAX_LABELS_PER_POCKET = 3

export interface Label {
  /**
   * Local id for the session. Substep 3 moves to a server-assigned integer; the shape stays `id,
   * name, color` on both sides so the client code around it does not change.
   */
  id: number
  name: string
  color: LabelColor
}

export interface LabelDraft {
  name: string
  color: LabelColor
}

/**
 * Which pocket the editor modal is open on, or null when it is closed. The modal lives in
 * `App.svelte` and reads this; the Tag button in the toolbox calls `openEditor(index)`.
 */
let editingIndex = $state<number | null>(null)

/** Labels the binder owns, keyed by id. `SvelteMap` so reads of `.get(id)` subscribe to writes. */
const labels = new SvelteMap<number, Label>()

/**
 * Labels on each pocket, keyed by the pocket's absolute index and holding an ordered list of
 * label ids. Up to {@link MAX_LABELS_PER_POCKET} per pocket; the order is the order the collector
 * added them, which the chip strip reads top-left to right.
 */
const assignments = new SvelteMap<number, number[]>()

/**
 * Monotonic local id for the session, so a newly created label can be referenced from an
 * assignment without a round-trip. Substep 3 replaces this with the server's integer.
 */
let nextId = 1

function nameExists(name: string, exceptId?: number): boolean {
  const needle = name.trim().toLowerCase()

  for (const label of labels.values()) {
    if (label.id === exceptId) continue
    if (label.name.trim().toLowerCase() === needle) return true
  }

  return false
}

export const pocketLabels = {
  /** Every label the binder owns, in insertion order so new ones land at the bottom of the list. */
  get all(): readonly Label[] {
    return [...labels.values()]
  },

  /** One label by id, or undefined if it was deleted. */
  get(id: number): Label | undefined {
    return labels.get(id)
  },

  /**
   * The labels on a pocket, resolved to full `Label` objects in the order the collector added them.
   * Ids that no longer point at a live label (would only happen on a stale read mid-delete) are
   * skipped.
   */
  forPocket(index: number): Label[] {
    const ids = assignments.get(index)

    if (!ids) return []

    const out: Label[] = []

    for (const id of ids) {
      const label = labels.get(id)

      if (label) out.push(label)
    }

    return out
  },

  /** How many pockets carry this label. Settings shows it on each row. */
  usageCount(labelId: number): number {
    let count = 0

    for (const ids of assignments.values()) {
      if (ids.includes(labelId)) count += 1
    }

    return count
  },

  /** Whether a name is already taken by another label in this binder. Excludes `exceptId`. */
  nameTaken(name: string, exceptId?: number): boolean {
    return nameExists(name, exceptId)
  },

  /**
   * Creates a new label and returns its id. The name is trimmed and clamped; the caller must
   * check `nameTaken(name)` first and show its own validation (the UI disables Save on a
   * duplicate), since throwing from here would complicate the modal flows.
   */
  createLabel(draft: LabelDraft): number {
    const id = nextId++
    const name = draft.name.trim().slice(0, MAX_LABEL_LENGTH)

    labels.set(id, { id, name, color: draft.color })

    return id
  },

  /** Renames a label. Same trim-and-clamp as `createLabel`. No-op on a missing id. */
  renameLabel(id: number, name: string): void {
    const current = labels.get(id)

    if (!current) return

    const trimmed = name.trim().slice(0, MAX_LABEL_LENGTH)

    labels.set(id, { ...current, name: trimmed })
  },

  /** Changes a label's colour. No-op on a missing id. */
  recolorLabel(id: number, color: LabelColor): void {
    const current = labels.get(id)

    if (!current) return

    labels.set(id, { ...current, color })
  },

  /**
   * Removes a label from the binder and strips it from every pocket that carried it. One write
   * from the caller's point of view -- substep 4's `LabelDelta` will replay it in one undo entry.
   */
  deleteLabel(id: number): void {
    if (!labels.delete(id)) return

    for (const [pocket, ids] of assignments) {
      if (!ids.includes(id)) continue

      const next = ids.filter((other) => other !== id)

      if (next.length === 0) assignments.delete(pocket)
      else assignments.set(pocket, next)
    }
  },

  /**
   * Toggles a label on a pocket. Returns true when the pocket now carries the label, false when
   * it does not -- useful for the picker's UI feedback. Refuses to add past
   * {@link MAX_LABELS_PER_POCKET}; the caller should disable the row when the cap is reached.
   */
  toggleOnPocket(index: number, labelId: number): boolean {
    if (!labels.has(labelId)) return false

    const current = assignments.get(index) ?? []

    if (current.includes(labelId)) {
      const next = current.filter((id) => id !== labelId)

      if (next.length === 0) assignments.delete(index)
      else assignments.set(index, next)

      return false
    }

    if (current.length >= MAX_LABELS_PER_POCKET) return true

    assignments.set(index, [...current, labelId])

    return true
  },

  /**
   * Empties a pocket's label list. Called by the Clear button in the modal and by every binder
   * action that drops the slot's card -- delete, return to tray, paste's displaced-discard.
   */
  clearPocket(index: number): void {
    assignments.delete(index)
  },

  /**
   * Swaps the label lists between two pockets. The slot drag swaps the cards, so the labels have
   * to come along. Substep 4 moves this into the slot delta; for now the drag handler calls it.
   */
  swapPockets(a: number, b: number): void {
    if (a === b) return

    const aIds = assignments.get(a)
    const bIds = assignments.get(b)

    if (aIds) assignments.set(b, aIds)
    else assignments.delete(b)

    if (bIds) assignments.set(a, bIds)
    else assignments.delete(a)
  },

  /** The pocket whose labels are being edited, or null when the modal is closed. */
  get editingIndex(): number | null {
    return editingIndex
  },

  /** The card the editor is working on, or null when nothing is open or the pocket emptied. */
  get editingCard(): CardSearchResult | null {
    if (editingIndex === null) return null

    return binderPage.slots[editingIndex] ?? null
  },

  openEditor(index: number): void {
    editingIndex = index
  },

  closeEditor(): void {
    editingIndex = null
  },
}
