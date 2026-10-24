// Client for the one dedicated label endpoint, POST /api/binders/{binderId}/labels. Same-origin
// requests, so the auth cookie rides along. Rename, recolor and delete all round-trip through
// `BinderClient.update` as part of the Settings save -- they are not CRUD, they are a sync.

/** The new label as the Create-and-apply flow carries it. */
export interface CreateLabelRequest {
  /** Trimmed and clamped to 20 characters by the server. */
  name: string
  /** One of the eight allowed colour names. The validator refuses anything else. */
  color: string
}

/** The server's answer: the id the row was assigned. */
export interface CreateLabelResponse {
  id: number
}

export const BinderCardLabelClient = {
  /**
   * Creates one label on a binder and returns its id. Called by Create-and-apply in the Label
   * modal, and by Settings' "New label" press -- the label needs a real id before anything else
   * (an assignment on a card, a rename, a recolor) can reference it.
   */
  async create(
    binderId: number,
    request: CreateLabelRequest,
    signal?: AbortSignal,
  ): Promise<CreateLabelResponse> {
    const response = await fetch(`/api/binders/${binderId}/labels`, {
      method: 'POST',
      credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify(request),
      signal,
    })

    if (!response.ok) {
      throw new Error(`Failed to create the label (${response.status} ${response.statusText}).`)
    }

    return (await response.json()) as CreateLabelResponse
  },
}
