// Client for the binder reorder endpoint. Same-origin requests, so the auth cookie rides along.

import type { SortCriterion } from '../features/binder/sort/reorder-criteria'

/** One pocket after a reorder. Mirrors ReorderBinder's ReorderedCard. */
export interface ReorderedCard {
  /** The pocket, counted from zero across the whole binder. */
  indexInBinder: number
  cardId: number
  /** Travels with its card: a reserved pocket holds a real card and sorts like any other. */
  isMissing: boolean
}

/** Mirrors ReorderBinder.Response: every placed card, from pocket zero with no gaps. */
export interface BinderReorderResponse {
  binderId: number
  cards: ReorderedCard[]
}

export const BinderReorderClient = {
  /**
   * Sorts the binder on the server and stores the result, answering with the new order.
   *
   * What gets sorted is what the server has, so the caller settles any pending save first -- see
   * `save.afterSettling`. By the time this resolves the new order is already stored.
   */
  async reorder(
    binderId: number,
    criteria: readonly SortCriterion[],
    signal?: AbortSignal,
  ): Promise<BinderReorderResponse> {
    // The binder travels in the URL only; the server takes the route's id over the body's.
    const response = await fetch(`/api/binderCards/${binderId}/reorder`, {
      method: 'POST',
      credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify({ criteria }),
      signal,
    })

    if (!response.ok) {
      throw new Error(`Failed to reorder the binder (${response.status} ${response.statusText}).`)
    }

    return (await response.json()) as BinderReorderResponse
  },
}
