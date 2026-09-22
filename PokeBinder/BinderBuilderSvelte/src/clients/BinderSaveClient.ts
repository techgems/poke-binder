// Client for the binder save endpoint. Same-origin requests, so the auth cookie rides along.

/** One card in one pocket, as the request carries it. Mirrors SaveBinderChanges' PlacedCard. */
export interface SavedCard {
  cardId: number
  /** The pocket, counted from zero across the whole binder. */
  indexInBinder: number
  isMissing: boolean
}

/** One tray row, as the request carries it. Mirrors SaveBinderChanges' TrayCard. */
export interface SavedTrayCard {
  cardId: number
  quantity: number
}

/**
 * One save.
 *
 * The pages are its scope and are not decoration: within them the cards are a snapshot, so a pocket
 * left out is one the user emptied, and outside them the server touches nothing. The tray travels
 * whole because it has no comparable scope to claim.
 */
export interface BinderSaveRequest {
  /** Travels in the URL rather than the body — see `save` below. */
  binderId: number
  /** Counted from one. At most two — a spread. The server refuses more. */
  pages: number[]
  cards: SavedCard[]
  tray: SavedTrayCard[]
}

/** What the endpoint answers with: what it actually wrote. Mirrors SaveBinderChanges.Response. */
export interface BinderSaveResponse {
  binderId: number
  placed: number
  changed: number
  removed: number
  trayAdded: number
  trayUpdated: number
  trayRemoved: number
}

export interface BinderSaveOptions {
  signal?: AbortSignal
  /**
   * Lets the request outlive the document, for the flush a closing tab fires. Chrome caps a
   * keepalive body at 64 KB, which a spread of a very large grid plus a full tray could reach —
   * nothing near the 3x3 case, but the reason this is not the default.
   */
  keepalive?: boolean
}

export const BinderSaveClient = {
  /**
   * Stores one edit: the pages named in the request, and the whole tray.
   *
   * PUT, because the request replaces what is at the pages it names — so sending it again writes
   * nothing, and a retry needs no ledger of what went before it.
   */
  async save(
    request: BinderSaveRequest,
    options: BinderSaveOptions = {},
  ): Promise<BinderSaveResponse> {
    // The binder is in the URL and stays out of the body. The server takes the id from the route
    // and overwrites whatever the body claimed, so a `binderId` in there is a field that cannot be
    // read and might be believed.
    const { binderId, ...edit } = request

    const response = await fetch(`/api/binderCards/${binderId}`, {
      method: 'PUT',
      credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify(edit),
      signal: options.signal,
      keepalive: options.keepalive,
    })

    if (!response.ok) {
      throw new Error(`Failed to save the binder (${response.status} ${response.statusText}).`)
    }

    return (await response.json()) as BinderSaveResponse
  },
}
