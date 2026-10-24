/**
 * The binder's labels and the modal that edits them.
 *
 * **A label belongs to the binder, not to a card.** Create one from the Label modal or the
 * Settings Labels panel, apply it to as many cards as the collector wants, rename or recolor it
 * in Settings and every chip in the binder follows, delete it there and every assignment of it
 * goes with it. Up to three labels per card.
 *
 * **Per-pocket assignments live on the slot now**, not here. `binder-page.svelte.ts` keeps the
 * `ArrangedPocket.labelIds` next to the card and the missing flag, so a drag, a reorder, a save,
 * and an undo all carry the labels with their card the same way they carry the flag. This store
 * only holds the binder-level label list -- `labels` for the committed ids, `pending` for ids a
 * modal session has created but not yet sent through `CreateBinderCardLabel`.
 */

import { SvelteMap } from 'svelte/reactivity'

import {
  BinderCardLabelClient,
  type CreateLabelRequest,
} from '../clients/BinderCardLabelClient'
import type { CardSearchResult } from '../clients/CardSearchClient'
import type { PreloadedBinderLabel } from '../preloads/preload'
import { applySlot, binderPage } from './binder-page.svelte'
import { history } from './history.svelte'

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

/** How many labels one pocket may carry at once. The schema's CHECK enforces the same cap. */
export const MAX_LABELS_PER_POCKET = 3

export interface Label {
  /** Server-assigned once committed; a negative local id while a pending creation is in flight. */
  id: number
  name: string
  color: LabelColor
}

export interface LabelDraft {
  name: string
  color: LabelColor
}

/** Which pocket the editor modal is open on, or null when it is closed. */
let editingIndex = $state<number | null>(null)

/** Committed labels, keyed by server id. `SvelteMap` so reads of `.get(id)` subscribe to writes. */
const labels = new SvelteMap<number, Label>()

/**
 * Labels created in the current modal session that have not been sent to the server yet. Negative
 * ids so they do not collide with real ones; a modal close commits them through
 * `CreateBinderCardLabel`, swapping the negative id for the real one on every slot that referenced
 * it. A chip-X on a pending label here drops it from this map and from the slots that pointed at
 * it, so nothing is sent.
 */
const pending = new SvelteMap<number, Label>()

/** Monotonic negative id for pending labels. */
let nextLocalId = -1

function slotFor(index: number) {
  return binderPage.pocketAt(index)
}

function nameExists(name: string, exceptId?: number): boolean {
  const needle = name.trim().toLowerCase()

  for (const label of labels.values()) {
    if (label.id === exceptId) continue
    if (label.name.trim().toLowerCase() === needle) return true
  }

  for (const label of pending.values()) {
    if (label.id === exceptId) continue
    if (label.name.trim().toLowerCase() === needle) return true
  }

  return false
}

/**
 * Writes the pocket's labelIds back onto the slot. The caller holds the new array; this centralises
 * the `binderPage.setPocketLabels` call so the per-pocket writes still go through one place and the
 * history records them as any other slot change.
 */
function writePocketLabels(index: number, labelIds: readonly number[]): void {
  binderPage.setPocketLabels(index, [...labelIds])
}

export const pocketLabels = {
  /** Every label the binder owns, committed first then pending, in insertion order per bucket. */
  get all(): readonly Label[] {
    return [...labels.values(), ...pending.values()]
  },

  /** One label by id, or undefined if it was deleted. Committed and pending both. */
  get(id: number): Label | undefined {
    return labels.get(id) ?? pending.get(id)
  },

  /**
   * The labels on a pocket, resolved to full `Label` objects in chip-strip order. Ids that no
   * longer point at a live label (would only happen on a stale read mid-delete) are skipped.
   */
  forPocket(index: number): Label[] {
    const slot = slotFor(index)

    if (!slot) return []

    const out: Label[] = []

    for (const id of slot.labelIds) {
      const label = labels.get(id) ?? pending.get(id)

      if (label) out.push(label)
    }

    return out
  },

  /** How many pockets carry this label. Settings shows it on each row. */
  usageCount(labelId: number): number {
    let count = 0

    for (const slot of binderPage.slotEntities) {
      if (slot && slot.labelIds.includes(labelId)) count += 1
    }

    return count
  },

  /** Whether a name is already taken by another label in this binder. */
  nameTaken(name: string, exceptId?: number): boolean {
    return nameExists(name, exceptId)
  },

  /**
   * Loads the binder's committed labels from the preload. Called once at startup from
   * `binderPage.load` so the picker and the chips can read them.
   */
  loadFromPreload(preloaded: readonly PreloadedBinderLabel[]): void {
    labels.clear()
    pending.clear()
    editingIndex = null

    for (const label of preloaded) {
      labels.set(label.id, {
        id: label.id,
        name: label.name,
        color: label.color as LabelColor,
      })
    }
  },

  /**
   * Rewrites the committed labels map in-place. The Settings tab's Discard button is what calls
   * this -- the committed labels are mutated as the Labels panel is edited (so chips update in
   * real time), and Discard restores whatever the server last sent.
   */
  resetCommitted(preloaded: readonly PreloadedBinderLabel[]): void {
    loadInto(labels, preloaded)
  },

  /**
   * Adds a label to the local store without a server call: assigns a negative client id and
   * stages the row in `pending` until something commits it.
   *
   * Two commit paths:
   * <ul>
   *   <li>The Label modal: Apply calls <c>commitPending</c> with the draft list of ids, which
   *   runs <c>CreateBinderCardLabel</c> for each row in that list, returns the local-to-real id
   *   mapping, and silently drops any pending row that is not in the draft.</li>
   *   <li>The Settings tab: its own Save sends the labels sync with the pending row carrying its
   *   negative id; <c>UpdateBinder</c> treats a non-positive id as an insert and the page refreshes
   *   under the stored real ids.</li>
   * </ul>
   */
  createLabel(draft: LabelDraft): number {
    const id = nextLocalId
    nextLocalId -= 1
    const name = draft.name.trim().slice(0, MAX_LABEL_LENGTH)

    pending.set(id, { id, name, color: draft.color })

    return id
  },

  /** Renames a label in place. Same trim-and-clamp as `createLabel`. No-op on a missing id. */
  renameLabel(id: number, name: string): void {
    const map = labels.has(id) ? labels : pending.has(id) ? pending : null

    if (!map) return

    const trimmed = name.trim().slice(0, MAX_LABEL_LENGTH)
    const current = map.get(id)!

    map.set(id, { ...current, name: trimmed })
  },

  /** Changes a label's colour. No-op on a missing id. */
  recolorLabel(id: number, color: LabelColor): void {
    const map = labels.has(id) ? labels : pending.has(id) ? pending : null

    if (!map) return

    const current = map.get(id)!

    map.set(id, { ...current, color })
  },

  /**
   * Removes a label from the binder-local list and strips it from every slot that carried it. For
   * a pending label this is the chip-X-before-close path (never reaches the server); for a
   * committed one it is the Settings-panel Delete, which rides out in the next Settings Save.
   */
  deleteLabel(id: number): void {
    const removedCommitted = labels.delete(id)
    const removedPending = pending.delete(id)

    if (!removedCommitted && !removedPending) return

    stripFromAllSlots(id)
  },

  /**
   * Toggles a label on a pocket. Returns true when the pocket now carries the label. Refuses to
   * add past {@link MAX_LABELS_PER_POCKET}; the caller should disable the row when the cap is
   * reached. The write goes through the slot, so the slot delta picks up the change and the save
   * carries the new ids.
   */
  toggleOnPocket(index: number, labelId: number): boolean {
    if (!labels.has(labelId) && !pending.has(labelId)) return false

    const slot = slotFor(index)

    if (!slot) return false

    const current = slot.labelIds

    if (current.includes(labelId)) {
      writePocketLabels(index, current.filter((id) => id !== labelId))

      return false
    }

    if (current.length >= MAX_LABELS_PER_POCKET) return true

    writePocketLabels(index, [...current, labelId])

    return true
  },

  /** Empties a pocket's label list through the slot. */
  clearPocket(index: number): void {
    const slot = slotFor(index)

    if (!slot || slot.labelIds.length === 0) return

    writePocketLabels(index, [])
  },

  /**
   * Walks `pending` and, for each row, either calls `CreateBinderCardLabel` to commit it or drops
   * it without a server call. Returns the local-to-real id mapping so the caller can translate a
   * draft list of ids before writing it to a slot. On an error the caller handles the toast; this
   * method rethrows after rolling back the pending label from any slots that referenced it.
   *
   * Two modes, picked by whether `keep` is given:
   * <ul>
   *   <li>With `keep`: the Label modal's Apply calls this with the draft it is about to write.
   *   Every pending id in `keep` is committed and mapped; every other pending id is discarded.
   *   This runs BEFORE the slot write, so the save fires with real ids and never a negative one;
   *   `swapIdOnAllSlots` and `history.rewriteLabelId` here are no-ops in the common case because
   *   no slot references the id yet, and they are kept for the odd case where a pending id was
   *   already on a slot from somewhere else.</li>
   *   <li>Without `keep`: the Label modal's close effect calls this to tidy up after a Cancel,
   *   X, Escape or backdrop click. Pending labels with no slot pointing at them are discarded;
   *   any still in use (would only happen on an edit from another source, since Cancel never
   *   writes to a slot) are committed as a safety net.</li>
   * </ul>
   */
  async commitPending(
    binderId: number,
    keep?: readonly number[],
  ): Promise<Map<number, number>> {
    const mapping = new Map<number, number>()

    if (pending.size === 0) return mapping

    const kept = keep === undefined ? null : new Set(keep)

    // Snapshot the pending ids first, since the committing walk mutates `pending`.
    const localIds = [...pending.keys()]

    for (const localId of localIds) {
      const label = pending.get(localId)

      if (!label) continue

      const shouldCommit = kept === null ? anySlotHas(localId) : kept.has(localId)

      if (!shouldCommit) {
        // Discarded silently, no server call.
        pending.delete(localId)
        continue
      }

      try {
        const request: CreateLabelRequest = { name: label.name, color: label.color }
        const { id: realId } = await BinderCardLabelClient.create(binderId, request)

        labels.set(realId, { id: realId, name: label.name, color: label.color })
        pending.delete(localId)
        mapping.set(localId, realId)
        swapIdOnAllSlots(localId, realId)
        history.rewriteLabelId(localId, realId)
      } catch (error) {
        pending.delete(localId)
        stripFromAllSlots(localId)

        throw error
      }
    }

    return mapping
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

/** Whether any slot in the binder points at this label id. */
function anySlotHas(id: number): boolean {
  for (const slot of binderPage.slotEntities) {
    if (slot && slot.labelIds.includes(id)) return true
  }

  return false
}

/**
 * Rewrites every slot's labelIds, removing the given id. Goes through `applySlot` rather than
 * `setPocketLabels` so no history entry is recorded: the strip is a cascade of a Settings-side
 * label delete (and of a pending chip-X), not an edit the user made to a card.
 */
function stripFromAllSlots(id: number): void {
  const slots = binderPage.slotEntities

  for (let index = 0; index < slots.length; index += 1) {
    const slot = slots[index]

    if (slot && slot.labelIds.includes(id)) {
      applySlot(index, {
        card: slot.card,
        missing: slot.missing,
        labelIds: slot.labelIds.filter((other) => other !== id),
      })
    }
  }
}

/**
 * Rewrites every slot's labelIds, swapping one id for another. Same `applySlot`-not-`setPocketLabels`
 * rule: the swap is the modal's commit replacing a pending negative id with the server-assigned
 * real one, which is bookkeeping rather than something the user did -- so the undoable history
 * entry that put the label on the card stays as it was, and `history.rewriteLabelId` updates the
 * ids inside it to match.
 */
function swapIdOnAllSlots(from: number, to: number): void {
  const slots = binderPage.slotEntities

  for (let index = 0; index < slots.length; index += 1) {
    const slot = slots[index]

    if (slot && slot.labelIds.includes(from)) {
      applySlot(index, {
        card: slot.card,
        missing: slot.missing,
        labelIds: slot.labelIds.map((id) => (id === from ? to : id)),
      })
    }
  }
}

function loadInto(map: SvelteMap<number, Label>, preloaded: readonly PreloadedBinderLabel[]): void {
  map.clear()

  for (const label of preloaded) {
    map.set(label.id, { id: label.id, name: label.name, color: label.color as LabelColor })
  }
}
