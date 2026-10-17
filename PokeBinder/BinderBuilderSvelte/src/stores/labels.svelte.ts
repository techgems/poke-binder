/**
 * Per-pocket labels: a short tag ("Trade", "PSA 10") in one of eight colours, pinned to the
 * card's bottom-left corner.
 *
 * **This is the substep-1 store, UI only.** Nothing here reaches the server or the save flow -- a
 * reload drops every label. Substep 2 moves this into the slot, through `SlotDelta`,
 * `ArrangedPocket`, `PlacedCard`, `BinderCard` and a new migration, with tests. The shape here is
 * what that step will carry.
 */

import { SvelteMap } from 'svelte/reactivity'

import type { CardSearchResult } from '../clients/CardSearchClient'
import { binderPage } from './binder-page.svelte'

/**
 * The eight colour names a label can take. Stored as names rather than hex so the swatches can be
 * restyled without rewriting any stored row. The component maps each to its Tailwind classes.
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

export interface PocketLabel {
  text: string
  color: LabelColor
}

/**
 * Which pocket the editor modal is open on, or null when it is closed. The modal lives in
 * `App.svelte` and reads this; a toolbox press on a pocket calls `openEditor(index)`.
 */
let editingIndex = $state<number | null>(null)

/**
 * The labels, keyed by pocket index. `SvelteMap` so component reads of `.get(index)` subscribe to
 * writes -- a plain Map wrapped in `$state` proxies property access, not the Map's own methods,
 * so reads would not re-render on `.set` / `.delete`.
 *
 * Local to the session: substep 1 stops here, substep 2 moves the storage into the slot itself so
 * it travels with its card like the missing flag already does.
 */
const labels = new SvelteMap<number, PocketLabel>()

export const pocketLabels = {
  /** Read inside a component or `$derived` to stay reactive. */
  get(index: number): PocketLabel | undefined {
    return labels.get(index)
  },

  /**
   * Writes a label onto a pocket, replacing whatever was there. The text is trimmed and clamped to
   * `MAX_LABEL_LENGTH` so stored rows (in substep 2) never need the validator to repair them.
   */
  set(index: number, text: string, color: LabelColor): void {
    const trimmed = text.trim().slice(0, MAX_LABEL_LENGTH)

    if (!trimmed) {
      labels.delete(index)

      return
    }

    labels.set(index, { text: trimmed, color })
  },

  /** Removes the label from a pocket. The modal's Remove button is what calls this. */
  clear(index: number): void {
    labels.delete(index)
  },

  /** The pocket whose label is being edited, or null when the modal is closed. */
  get editingIndex(): number | null {
    return editingIndex
  },

  /**
   * The card the editor is working on. Null when the editor is closed, or when the pocket has
   * been emptied under the modal (a race the modal should close itself out of).
   */
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
