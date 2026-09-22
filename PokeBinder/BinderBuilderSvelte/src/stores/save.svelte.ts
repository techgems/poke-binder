import {
  BinderSaveClient,
  type BinderSaveRequest,
  type BinderSaveResponse,
  type SavedCard,
} from '../clients/BinderSaveClient'
import { binderPage } from './binder-page.svelte'
import { tray } from './tray.svelte'

/**
 * When the binder is saved, and what one save carries.
 *
 * Every edit arms a timer here; the timer sends one request covering the pages that were on screen
 * when the edits happened, plus the whole tray. `BinderSaveClient` is what puts it on the wire, and
 * the shapes it travels as are that client's.
 *
 * **It listens to the history store, not to the stores history watches.** An action that records
 * itself schedules its own save, which leaves one list of mutating calls to keep complete instead
 * of two: if `tray.svelte.ts` and `binder-page.svelte.ts` record everything they change, this is
 * armed by everything they change. It is three events rather than one, though -- a new action
 * pushes, and undo and redo push nothing because they move a pointer -- and all three arm it,
 * because all three change what is in the binder.
 *
 * Clearing the history is not one of them, and must not cancel a save that is already armed: the
 * edits before the clear still happened.
 */

export type SaveStatus = 'idle' | 'saving' | 'saved' | 'error'

/**
 * How long a run of edits defers the save.
 *
 * Five seconds, not the ten the plan allowed for. Everything lost when a tab closes is bounded by
 * this number, and the cost of it being short is one extra request during a pause the user took
 * anyway -- a cheap trade against the same pause losing five more seconds of work.
 */
export const SAVE_DEBOUNCE_MS = 5_000

/**
 * The longest a save waits, counted from the first change it is still holding, however many edits
 * arrive after it.
 *
 * Without this, nothing bounds a run of resets except the user: somebody leaning on a quantity
 * stepper defers the save for as long as they keep going, and a tab closed mid-run loses all of
 * it. Thirty seconds, so a long run costs a handful of requests rather than one.
 */
export const SAVE_MAX_WAIT_MS = 30_000

/** How long a failed save waits before its one retry. */
export const SAVE_RETRY_DELAY_MS = 1_000

/**
 * Pages one request may claim. Mirrors SaveBinderChanges.MaxPages, which refuses more: a binder
 * falls open two pages at a time, and a page flip flushes the pending save rather than widening it.
 */
const MAX_PAGES = 2

let dirty = $state(false)
let status = $state<SaveStatus>('idle')

/**
 * Saves that could not be stored this session -- one per save, not per attempt, so a save that
 * failed and failed again on its retry counts once. What the toast in `App.svelte` watches, and a
 * number worth keeping beyond that: how often saving fails is the question a workspace that saves
 * silently cannot otherwise answer.
 */
let failures = $state(0)

// The pages the pending save will claim. Captured as each edit arms the timer rather than read when
// it fires: built at fire time from the open spread, a save armed on page two and fired after a flip
// to page four would post page four's pockets, lose the edit on page two, and claim a scope it never
// touched -- which would clear page four as well.
let scope = new Set<number>()

let timer: ReturnType<typeof setTimeout> | null = null

// When the oldest change this save is still holding arrived, which is what SAVE_MAX_WAIT_MS counts
// from.
let oldestPendingAt = 0

// Bumped by every arm, so a save that comes back can tell whether anything changed while it was in
// flight.
let revision = 0

// The save currently in flight, or null. One at a time.
let inFlight: AbortController | null = null

function clearTimer(): void {
  if (timer === null) return

  clearTimeout(timer)
  timer = null
}

/**
 * The pages a set of pockets sits on, or the open spread when the action touched no pocket at all.
 *
 * A tray-only edit -- adding a card, changing a quantity -- has no pocket to name, and a request
 * has to claim something. The open spread is the honest answer: those pages are what the client is
 * looking at, the payload carries them as they stand, and the server's diff over them writes
 * nothing.
 */
function pagesFor(pockets: readonly number[]): number[] {
  const pages = new Set<number>()
  const perPage = binderPage.cardsPerPage

  for (const pocket of pockets) {
    if (pocket >= 0) pages.add(Math.floor(pocket / perPage) + 1)
  }

  if (pages.size === 0) {
    for (const page of binderPage.spreadPages) pages.add(page.pageNumber)
  }

  return [...pages]
}

/** The request for a set of pages, or null when there is no binder to address it to. */
function buildRequest(pages: number[]): BinderSaveRequest | null {
  const binderId = binderPage.binderId

  if (binderId === null) return null

  const cards: SavedCard[] = []

  for (const page of pages) {
    for (const slot of binderPage.pageAt(page).slots) {
      if (slot.card === null) continue

      cards.push({
        cardId: slot.card.id,
        indexInBinder: slot.index,
        // Round-tripped rather than decided here: nothing in the workspace sets this flag, so what
        // the server sent is what it gets back. Leaving it out would clear it on every pocket the
        // save covers.
        isMissing: binderPage.isMissing(slot.index),
      })
    }
  }

  return {
    binderId,
    pages: [...pages].sort((left, right) => left - right),
    cards,
    tray: tray.entries.map((entry) => ({
      cardId: entry.card.id,
      quantity: entry.quantity,
    })),
  }
}

function schedule(): void {
  clearTimer()

  // Whichever comes first: the quiet window, or the ceiling measured from the oldest change still
  // waiting. A run of edits keeps resetting the first and cannot touch the second.
  const untilCeiling = oldestPendingAt + SAVE_MAX_WAIT_MS - Date.now()
  const delay = Math.max(0, Math.min(SAVE_DEBOUNCE_MS, untilCeiling))

  timer = setTimeout(() => {
    timer = null
    send()
  }, delay)
}

/**
 * Sends the pending save, unless one is already in flight.
 *
 * <b>A new save does not abort the one in flight.</b> Aborting is safe only when the new payload
 * covers everything the old one carried, and after a flip it does not: the flush sends the spread
 * being left, and the save behind it may claim different pages. So this waits, and the save that
 * returns schedules the next one if anything is still pending.
 */
function send(): void {
  if (!dirty || scope.size === 0 || inFlight !== null) return

  clearTimer()

  const pages = [...scope]
  const sentRevision = revision
  const request = buildRequest(pages)

  if (request === null) return

  // Cleared only now that the request is built: an edit arriving while this is in flight captures
  // its own scope and arms its own timer, and the two do not have to be told apart.
  scope = new Set()

  const controller = new AbortController()

  inFlight = controller
  status = 'saving'

  void deliver(request, controller, pages, sentRevision)
}

async function deliver(
  request: BinderSaveRequest,
  controller: AbortController,
  pages: number[],
  sentRevision: number,
): Promise<void> {
  let failed = false

  try {
    await attempt(request, controller)

    status = 'saved'

    // The dirty flag is cleared when a save comes back, not when one is sent. An edit made while
    // this was in flight bumped the revision, and what it changed has not been saved by this.
    if (revision === sentRevision) dirty = false
  } catch {
    failed = true

    // The pages go back into the pending scope rather than being dropped: a save that failed is a
    // save that still has to happen.
    for (const page of pages) scope.add(page)

    status = 'error'

    // After the retry, so this is one count per save given up on rather than one per attempt.
    failures += 1
  } finally {
    if (inFlight === controller) inFlight = null

    // Something arrived while this was in flight, so it needs a save of its own.
    //
    // **A failed save does not re-arm the timer**, which is why this asks. Re-arming looks like
    // resilience and is a retry loop with no end: a server that is down gets one request every
    // window for as long as it stays down, and the user gets a notice each time -- which is
    // exactly what it did, until a broken endpoint stacked two dozen of them. The pages stay
    // dirty, so the next edit or page flip carries them, and `pagehide` carries them if the tab
    // closes first.
    if (dirty && !failed) schedule()
  }
}

/**
 * One send and, if it fails, one retry after a pause.
 *
 * A pause rather than an immediate second attempt: what makes a save fail is usually a connection
 * that has just dropped or a server that is restarting, and neither is fixed within a millisecond.
 */
async function attempt(
  request: BinderSaveRequest,
  controller: AbortController,
): Promise<BinderSaveResponse> {
  try {
    return await BinderSaveClient.save(request, { signal: controller.signal })
  } catch {
    await new Promise((resolve) => setTimeout(resolve, SAVE_RETRY_DELAY_MS))

    return await BinderSaveClient.save(request, { signal: controller.signal })
  }
}

export const save = {
  /** Unsaved changes are waiting. Read inside a component or a `$derived` to stay reactive. */
  get isDirty(): boolean {
    return dirty
  },

  get status(): SaveStatus {
    return status
  },

  /** Saves that could not be stored this session. One per save, retry included. */
  get failures(): number {
    return failures
  },

  /** The pages the pending save will claim, for a test or a console. Empty when nothing is pending. */
  get pendingPages(): readonly number[] {
    return [...scope].sort((left, right) => left - right)
  },

  /**
   * Arms the timer for an edit, and records which pages it has to cover.
   *
   * Called by the history store for all three of its events. The pockets are the ones the action
   * touched, taken from the entry rather than from what is on screen -- an undo can change a pocket
   * on a page nobody is looking at, and the pages it actually touched are the ones the request has
   * to claim.
   *
   * @param pockets Pockets this action changed, or empty for an action that changed only the tray.
   */
  arm(pockets: readonly number[] = []): void {
    // No binder, nothing to address a save to: /Binder without an id is a legitimate way to arrive.
    if (binderPage.binderId === null) return

    const pages = pagesFor(pockets)
    const widened = new Set([...scope, ...pages])

    if (widened.size > MAX_PAGES) {
      // More pages than one request may claim, which the flush-on-flip rule is supposed to make
      // impossible. Send what is pending under the scope it was captured with, and start a new one
      // rather than posting a claim the server would refuse.
      this.flush()

      scope = new Set(pages)
    } else {
      scope = widened
    }

    if (!dirty) {
      dirty = true
      oldestPendingAt = Date.now()
    }

    revision += 1

    schedule()
  },

  /**
   * Sends what is pending now instead of waiting out the window.
   *
   * What a page flip does: a flip ends the spread the pending save was captured against, so it is
   * sent rather than deferred again. **A flip with nothing pending sends nothing at all** -- which
   * is what the dirty flag is for, since a timer alone cannot tell "waiting to save" from "nothing
   * to save".
   */
  flush(): void {
    if (!dirty) return

    send()
  },

  /**
   * Starts listening for the tab going away, so a close costs less than the debounce window.
   *
   * `pagehide` rather than `unload`, which a page in the back/forward cache never gets, and
   * `visibilitychange` because iOS may never fire `pagehide` at all. Called once from `main.ts`
   * rather than on import, so importing this store has no side effect of its own.
   */
  listenForUnload(): void {
    const flushOnLeave = (): void => {
      if (!dirty) return

      // Fire and forget: nothing will be alive to read the answer, and `keepalive` is what lets
      // the request outlive the document. It goes around `send` because the in-flight rule is
      // about not racing two saves in a live tab, and this tab is not going to be live.
      const request = buildRequest([...scope])

      if (request !== null) {
        void BinderSaveClient.save(request, { keepalive: true }).catch(() => {})
      }
    }

    window.addEventListener('pagehide', flushOnLeave)
    window.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'hidden') flushOnLeave()
    })
  },
}
