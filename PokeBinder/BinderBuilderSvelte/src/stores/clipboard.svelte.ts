/**
 * One card held for pasting, and the dialog that opens when a paste would displace a different one.
 *
 * The clipboard is per-page memory: a copy survives until another is copied or the tab reloads, and
 * nothing else saves or persists it. A paste over an empty pocket -- or a pocket already holding
 * the same card -- is a quiet mutation; a paste that would displace a different card opens the
 * conflict dialog, since the drag path never forces the choice between sending the displaced to the
 * tray and dropping it.
 *
 * This module decides which of the three happens and sets the state the dialog reads. The actual
 * write goes through `binderPage.pasteCard`, so undo and the save flow handle paste like any other
 * slot change.
 */

import type { CardSearchResult } from '../clients/CardSearchClient'
import { binderPage } from './binder-page.svelte'

/** A paste that is waiting on the user to decide what happens to the displaced card. */
export interface PasteConflict {
  /** Pocket the paste is landing in. */
  index: number
  /** The card currently in that pocket, which the paste would replace. */
  displaced: CardSearchResult
}

/**
 * A copy that should flash on the pocket it came from. The nonce bumps on every copy so repeating
 * the same pocket re-triggers the animation -- a straight equality check on `index` would miss it.
 */
export interface CopyFlash {
  index: number
  nonce: number
}

let copied = $state<CardSearchResult | null>(null)
let conflict = $state<PasteConflict | null>(null)
let flash = $state<CopyFlash | null>(null)

export const clipboard = {
  /** The card waiting to be pasted, or null if nothing is copied. */
  get copied(): CardSearchResult | null {
    return copied
  },

  /** True once a card has been copied this session. */
  get hasCopy(): boolean {
    return copied !== null
  },

  /** A paste waiting on the dialog, or null when nothing is pending. */
  get conflict(): PasteConflict | null {
    return conflict
  },

  /**
   * Which pocket to flash to confirm the copy. The pocket component reads it; a plain CSS
   * animation on a matching overlay is the visual feedback.
   *
   * A flash was picked over a toast because the card is where the eye already is -- the pointer
   * just clicked it. A toast in the top-right travels the same message a longer distance, and the
   * confirmation is small enough that the gentler option fits the action. The alternative is
   * worth remembering: a flash doesn't fire when the copy comes from a page the user cannot see
   * (future scope), and a toast does, so a settings toggle may offer it later. See the toast
   * group in `App.svelte` and `components/toaster.ts` for the ready-made path.
   */
  get flash(): CopyFlash | null {
    return flash
  },

  /**
   * Captures a card into the clipboard, replacing whatever was there, and flashes the pocket it
   * came from when `fromIndex` is given. Called from the toolbox button (which knows the pocket)
   * and from the Ctrl/Cmd+C handler (which looks it up under the pointer).
   *
   * The flash state is cleared on a timeout matching the animation. Without it, flipping to a
   * different spread and back would re-mount the pocket's `{#if clipboard.flash?.index === …}`
   * block long after the fact, and the glint would play all over again -- stale state the
   * component cannot tell apart from a fresh copy.
   */
  copy(card: CardSearchResult, fromIndex?: number): void {
    copied = card

    if (fromIndex === undefined) return

    const next: CopyFlash = { index: fromIndex, nonce: (flash?.nonce ?? 0) + 1 }

    flash = next

    // A touch longer than the CSS animation (see `.copy-flash-band` in BinderPage.svelte) so the
    // sweep finishes rather than being unmounted mid-flight. Only clear when we are still the
    // current flash: a later copy may have replaced us, and its own timeout will handle it.
    setTimeout(() => {
      if (flash?.nonce === next.nonce) flash = null
    }, 400)
  },

  /**
   * Pastes the copied card into a pocket. Empty pockets and same-card pockets write (or no-op)
   * straight away; a different card opens the conflict dialog, which `resolveConflict` closes.
   *
   * Does nothing if there is nothing copied, or if a conflict is already open.
   */
  paste(index: number): void {
    if (!copied || conflict) return

    const existing = binderPage.slots[index] ?? null

    if (existing && existing.id === copied.id) return

    if (existing) {
      conflict = { index, displaced: existing }

      return
    }

    binderPage.pasteCard(index, copied, 'discard')
  },

  /**
   * Closes the conflict dialog by committing the paste with the user's choice for the displaced
   * card -- to the tray, or discarded.
   */
  resolveConflict(displacedTo: 'tray' | 'discard'): void {
    if (!conflict || !copied) return

    binderPage.pasteCard(conflict.index, copied, displacedTo)
    conflict = null
  },

  /** Closes the dialog without pasting -- the Cancel path. */
  cancelConflict(): void {
    conflict = null
  },
}
