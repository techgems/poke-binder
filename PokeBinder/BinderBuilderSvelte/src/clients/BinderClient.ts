// Client for the binder itself -- BindersController -- as opposed to what is in it, which is
// BinderSaveClient's and BinderReorderClient's. Same-origin requests, so the auth cookie rides along.

import type { SortCriterion } from '../features/binder/sort/reorder-criteria'

/** What an edit does with the placed cards. Mirrors UpdateBinder's Rearrangement. */
export type Rearrangement = 'Keep' | 'Sort' | 'Tray' | 'Drop'

/** One edit to a binder. Mirrors UpdateBinder.Request, less the id, which travels in the URL. */
export interface BinderUpdateRequest {
  name: string
  description: string | null
  binderSizeId: number
  pages: number
  /** Null when the new size moves no card: every card stays in its pocket. */
  rearrange: Rearrangement | null
  /** Only with a sort, which requires them. */
  criteria: SortCriterion[] | null
}

export const BinderClient = {
  /**
   * Stores an edit to the binder's settings, and whatever it does to the placed cards, in one
   * transaction. The caller settles its pending page save first -- see `save.afterSettling` --
   * because a rearrangement works on what the server has.
   */
  async update(binderId: number, request: BinderUpdateRequest, signal?: AbortSignal): Promise<void> {
    // The binder travels in the URL only; the server takes the route's id over the body's.
    const response = await fetch(`/api/binders/${binderId}`, {
      method: 'PUT',
      credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify(request),
      signal,
    })

    if (!response.ok) {
      throw new Error(`Failed to save the binder (${response.status} ${response.statusText}).`)
    }
  },

  /**
   * Deletes the binder with its placed cards and its tray. Resolves once it is gone; there is
   * nothing to answer with.
   */
  async delete(binderId: number, signal?: AbortSignal): Promise<void> {
    const response = await fetch(`/api/binders/${binderId}`, {
      method: 'DELETE',
      credentials: 'same-origin',
      headers: { Accept: 'application/json' },
      signal,
    })

    if (!response.ok) {
      throw new Error(`Failed to delete the binder (${response.status} ${response.statusText}).`)
    }
  },
}
