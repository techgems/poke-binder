import type { AddSetResponse } from '../clients/BinderAddSetClient'
import type { ReorderedCard } from '../clients/BinderReorderClient'
import type { CardSearchResult } from '../clients/CardSearchClient'
import type { PreloadedBinder } from '../preloads/preload'
import { history } from './history.svelte'
import { pocketLabels } from './labels.svelte'
import { save } from './save.svelte'
import { tray } from './tray.svelte'

/**
 * The cards placed in the binder, and the drag in progress between them and the tray.
 *
 * The two belong together: every move is a transfer between the binder and the tray, so the rules
 * about what a copy costs live in one place instead of being restated by whichever component
 * happens to be handling the drop.
 *
 * Slots cover the *whole* binder, not the spread on screen, because the user pages through the
 * whole binder and every pocket has to be there to turn to. Saving does not need it: a save names
 * the pages it covers and carries only those, so the rest of this array is state the client holds
 * for its own sake and never posts.
 */

/** The grid used before a binder is loaded: a plain 3x3 sheet of one page. */
const DEFAULT_COLUMNS = 3
const DEFAULT_ROWS = 3

/** One pocket on screen: the card in it, the index the binder and the server know it by, and
 * whether the pocket is flagged as a card the collector does not own yet. */
export interface BinderSlotView {
  index: number
  card: CardSearchResult | null
  missing: boolean
}

/** One page on screen. */
export interface BinderPageView {
  pageNumber: number
  slots: BinderSlotView[]
}

/** A full pocket in a snapshot of the binder: the card, and whether the pocket is flagged missing. */
export interface ArrangedPocket {
  card: CardSearchResult
  missing: boolean
}

/**
 * Every pocket of the binder at one moment, empty ones as null. What a reorder is recorded as: the
 * arrangement it threw away and the one it put in its place.
 */
export type Arrangement = readonly (ArrangedPocket | null)[]

/** Where a dragged card came from, which is what decides what dropping it means. */
export type DragSource =
  | { kind: 'tray'; card: CardSearchResult }
  | { kind: 'slot'; card: CardSearchResult; index: number }

// Which binder this is. Null until one is loaded -- /Binder without an id is a legitimate way to
// arrive -- and needed before anything can address a save to it.
let binderId = $state<number | null>(null)

let columns = $state(DEFAULT_COLUMNS)
let rows = $state(DEFAULT_ROWS)
let pages = $state(1)
let slots = $state<(ArrangedPocket | null)[]>(Array(DEFAULT_COLUMNS * DEFAULT_ROWS).fill(null))

// Which spread is open, counted from zero. A binder opens on one page and then falls open in
// pairs, so spread 0 is page 1 alone and spread n is pages 2n and 2n+1 -- the same way the sheets
// are actually bound.
let spread = $state(0)

// The drag lives here rather than in the DataTransfer because dragover has to decide whether a
// drop is allowed, and the payload cannot be read until the drop itself.
let dragged = $state<DragSource | null>(null)

/**
 * Puts a slot in a pocket, or empties it.
 *
 * The one write every change to the slots goes through, whether it is an action the user took or
 * an undo replaying one. The slot carries both the card and the missing flag, so the two always
 * move together and undo restores whichever state the user left behind.
 */
export function applySlot(index: number, slot: ArrangedPocket | null): void {
  slots[index] = slot
}

/**
 * Lays the whole binder out as a snapshot says, missing flags included. The write a reorder and its
 * undo and redo go through -- a snapshot is the right size when every pocket could have moved.
 */
export function applyArrangement(arrangement: Arrangement): void {
  slots = [...arrangement]
}

/**
 * Turns to a spread, flushing whatever save was waiting for the one being left.
 *
 * The flush is the rule that keeps one pending save to one spread: a save names the pages it
 * covers, so a timer armed on page two and left to fire after a flip would post the wrong pages
 * entirely. Sending on the way out is what makes the captured scope safe to trust. A flip with
 * nothing pending sends nothing.
 */
function openSpread(next: number): void {
  if (next === spread) return

  save.flush()

  spread = next
}

/** The same write, recorded. Nothing changed is nothing to record. */
function changeSlot(index: number, to: ArrangedPocket | null): void {
  const from = slots[index] ?? null

  if (from === to) return

  history.record({ store: 'slot', index, from, to })
  applySlot(index, to)
}

/**
 * Lays out a layout the server has already stored and records it for undo: the arrangement it
 * replaced and the one it put in its place. Nothing is saved -- there is nothing to save until an
 * undo or a redo moves the binder away from what is stored.
 *
 * The server answers with ids. Each is looked up among the cards already in the binder, then in
 * `added` for any it has just put there. One found in neither -- a card the catalog dropped, which
 * `load` never drew either -- leaves its pocket empty on screen, as it always was.
 */
function adoptLayout(layout: readonly ReorderedCard[], added: readonly CardSearchResult[]): void {
  const before = binderPage.arrangement

  const cards = new Map<number, CardSearchResult>()

  for (const slot of slots) {
    if (slot) cards.set(slot.card.id, slot.card)
  }

  for (const card of added) cards.set(card.id, card)

  const after: (ArrangedPocket | null)[] = Array(slots.length).fill(null)

  for (const placed of layout) {
    const card = cards.get(placed.cardId)

    if (card && placed.indexInBinder < after.length) {
      after[placed.indexInBinder] = { card, missing: placed.isMissing }
    }
  }

  history.recordStored({ store: 'arrangement', before, after })
  applyArrangement(after)
}

export const binderPage = {
  /** Which binder is loaded, or null when the page was opened without one. */
  get binderId(): number | null {
    return binderId
  },

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

  /** Every pocket in the binder, in order, each as the card it holds -- the missing flag lives on
   * the slot itself, read through `isMissing` or straight off a `BinderSlotView`. */
  get slots(): readonly (CardSearchResult | null)[] {
    return slots.map((slot) => slot?.card ?? null)
  },

  /** Which spread is open, counted from zero. */
  get spread(): number {
    return spread
  },

  /**
   * Whether a pocket is flagged as reserved for a card the collector does not own yet. Read when a
   * save is built, so the flag survives the save; `BinderSlotView.missing` is the same answer for a
   * pocket that is already on screen.
   */
  isMissing(index: number): boolean {
    return slots[index]?.missing ?? false
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
      slots: Array.from({ length: this.cardsPerPage }, (_, offset) => {
        const slot = slots[start + offset] ?? null

        return {
          index: start + offset,
          card: slot?.card ?? null,
          missing: slot?.missing ?? false,
        }
      }),
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
    if (this.canGoBack) openSpread(spread - 1)
  },

  goForward(): void {
    if (this.canGoForward) openSpread(spread + 1)
  },

  /**
   * Opens the spread holding a pocket. What undo uses to bring a change it is about to make onto
   * the screen, since a pocket changing on a page nobody is looking at reads as a press that did
   * nothing.
   */
  showIndex(index: number): void {
    if (index < 0) return

    const page = Math.floor(index / this.cardsPerPage) + 1

    openSpread(page <= 1 ? 0 : Math.floor(page / 2))
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

    binderId = summary.id
    columns = summary.x
    rows = summary.y
    pages = summary.pages
    spread = 0

    const placed: (ArrangedPocket | null)[] = Array(summary.cardCount).fill(null)

    for (const placement of preloaded.cards) {
      // A card the catalog no longer has, or a pocket outside the binder's current size — the
      // binder may have been shrunk since. Either way there is nothing to draw, and dropping it
      // here is better than rendering a hole halfway down the page.
      if (!placement.card || placement.indexInBinder >= placed.length) continue

      placed[placement.indexInBinder] = { card: placement.card, missing: placement.isMissing }
    }

    slots = placed

    tray.load(
      preloaded.tray
        .filter((entry) => entry.card !== null)
        .map((entry) => ({ card: entry.card as CardSearchResult, quantity: entry.quantity })),
    )

    // The baseline the first undo stops at, and the one event undo cannot reach back across: what
    // the server sent is where this session starts.
    history.clear()
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

    history.act(() => {
      if (source.kind === 'slot') {
        if (source.index === index) return

        // A straight exchange: whatever was in the target lands where the dragged card came from,
        // which for an empty target is the same as moving it. The whole slot moves rather than just
        // the card, so the missing flag travels with it.
        const sourceSlot = slots[source.index] ?? null
        const targetSlot = slots[index] ?? null

        changeSlot(index, sourceSlot)
        changeSlot(source.index, targetSlot)

        // Labels follow their card. Substep 4 moves this into the slot delta so it ends up
        // undoable like the rest; for now the client-only store is swapped alongside the slot.
        pocketLabels.swapPockets(source.index, index)

        return
      }

      const displaced = slots[index]

      if (displaced) {
        tray.add(displaced.card)
      }

      // A card placed from the tray is not missing: the collector is putting one in. If the pocket
      // was flagged missing for an older card, that flag went with the displaced slot above.
      changeSlot(index, { card: source.card, missing: false })

      // Displaced goes to the tray; labels do not travel to the tray, so clear whatever was on
      // this pocket. The new card starts with no labels.
      pocketLabels.clearPocket(index)

      // Placing spends one of the copies waiting in the tray; setQuantity drops the entry when the
      // last one leaves.
      tray.setQuantity(source.card.id, tray.quantityOf(source.card.id) - 1)
    })
  },

  /** Drops the card being dragged back into the tray. Only a card from a pocket has anywhere to go.
   * The missing flag is left behind -- a tray entry is one card with a count, with nowhere to keep
   * it -- and an undo brings it back with its slot. Labels follow the same rule. */
  dropOnTray(): void {
    const source = dragged

    dragged = null

    if (source?.kind !== 'slot') return

    const { index, card } = source

    history.act(() => {
      changeSlot(index, null)

      tray.add(card)

      pocketLabels.clearPocket(index)
    })
  },

  /**
   * Takes a card out of a pocket without a drag — the same transfer the drop above performs, for
   * anyone not using a mouse. Same rule for the missing flag and the labels: dropped on the tray.
   */
  returnToTray(index: number): void {
    const slot = slots[index]

    if (!slot) return

    history.act(() => {
      changeSlot(index, null)

      tray.add(slot.card)

      pocketLabels.clearPocket(index)
    })
  },

  /**
   * Empties the pocket. The card goes nowhere -- not to the tray -- which is what sets this apart
   * from `returnToTray`. Undo brings the slot back as it was, missing flag included, like any other
   * slot change, so there is no confirmation to carry. Labels on the pocket go with the card.
   */
  deleteFromBinder(index: number): void {
    if (!slots[index]) return

    history.act(() => {
      changeSlot(index, null)

      pocketLabels.clearPocket(index)
    })
  },

  /**
   * Places a card into a pocket from the clipboard. Assumes the caller has already decided what
   * happens to any displaced card -- `clipboard.paste` is the orchestrator that makes that choice,
   * directly for an empty pocket or through the conflict dialog for an occupied one.
   *
   * A paste onto the same card id is a no-op, so a stray press on the source pocket neither
   * mutates nor records. A paste onto an empty pocket arrives with `missing: false`: the collector
   * is placing a card they consider present, like a tray drop.
   *
   * @param displacedTo What to do with the card already in the pocket. `tray` adds it as a copy
   *   there, matching the drag path; `discard` drops it from the binder altogether. Ignored for an
   *   empty pocket.
   */
  pasteCard(index: number, card: CardSearchResult, displacedTo: 'tray' | 'discard'): void {
    const existing = slots[index] ?? null

    if (existing && existing.card.id === card.id) return

    history.act(() => {
      if (existing && displacedTo === 'tray') tray.add(existing.card)

      changeSlot(index, { card, missing: false })

      // Displaced card -- whether sent to tray or discarded -- loses its labels; the pasted card
      // starts with none, same as a tray drop.
      pocketLabels.clearPocket(index)
    })
  },

  /**
   * Flips the missing flag on a pocket. A missing pocket is one the collector has marked as held
   * for a card they do not own yet; unmarking it says they have the card after all. The slot's
   * card stays put: only the flag moves, and undo puts it back.
   */
  toggleMissing(index: number): void {
    const current = slots[index]

    if (!current) return

    history.act(() => {
      changeSlot(index, { card: current.card, missing: !current.missing })
    })
  },

  /** The binder as it stands, every pocket, as a snapshot that later edits do not reach into. */
  get arrangement(): Arrangement {
    return slots.map((slot) => (slot ? { card: slot.card, missing: slot.missing } : null))
  },

  /**
   * Takes the order a reorder came back with, already stored -- see `adoptLayout`. Undo lays the
   * old order back out and Redo the new, and each of those is an edit the server has not seen,
   * which is what saves. Every card is one the binder already held, so none comes with it.
   */
  adoptReorder(reordered: readonly ReorderedCard[]): void {
    adoptLayout(reordered, [])

    // The start of the new order, which is the part of it worth seeing first.
    openSpread(0)
  },

  /**
   * Takes the layout adding a set came back with, already stored -- recorded the way a reorder is,
   * so Undo takes the set out again and puts back whatever it moved or removed.
   *
   * The set's cards come with the response, since the workspace has never seen them. Pages added
   * to fit it stay through an undo: the server has already grown the binder, and undo puts the
   * pockets back, not the binder's length -- so the snapshot taken before is padded out to the new
   * length, every added pocket empty.
   */
  adoptAddedSet(response: AddSetResponse): void {
    if (response.pages > pages) {
      pages = response.pages
      slots = [...slots, ...Array(pages * columns * rows - slots.length).fill(null)]
    }

    adoptLayout(response.cards, response.added)

    // The set's first page, which is what was just asked for.
    this.showIndex(response.setStart)
  },

  /**
   * Puts a card in the first free pocket on screen, again as a keyboard-reachable stand-in for
   * dragging it there. Scoped to the open spread rather than the whole binder: a card that vanished
   * onto page eleven would be worse than one that went nowhere.
   */
  placeInFirstFreeSlot(card: CardSearchResult): boolean {
    const free = this.visibleSlots.find((slot) => slot.card === null)

    if (!free) return false

    history.act(() => {
      changeSlot(free.index, { card, missing: false })
      tray.setQuantity(card.id, tray.quantityOf(card.id) - 1)
    })

    return true
  },
}
