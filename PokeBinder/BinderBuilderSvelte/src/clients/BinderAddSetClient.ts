// Client for the add-a-set endpoint. Same-origin requests, so the auth cookie rides along.

import type { SortCriterion } from '../features/binder/sort/reorder-criteria'
import type { ReorderedCard } from './BinderReorderClient'
import type { CardSearchResult } from './CardSearchClient'

/** Where the set goes relative to the cards already in the binder. Mirrors AddSetToBinder's ExistingCards. */
export type ExistingCards = 'after' | 'buffer' | 'end'

/** Mirrors AddSetToBinder.Request, less the binder id, which travels in the URL. */
export interface AddSetRequest {
  setId: number
  /** Rarities, by name, whose cards stay out. */
  leaveOut: string[]
  /** Rarity, card name and card number; never set. */
  criteria: SortCriterion[]
  emptyFirst: boolean
  existing: ExistingCards
  /** Pages may be added at the end when the set does not fit. */
  addPages: boolean
}

/** Mirrors AddSetToBinder.Response. */
export interface AddSetResponse {
  binderId: number
  /** The binder's page count now: more than before when pages were added. */
  pages: number
  /** The pocket the set's first card went in. */
  setStart: number
  /** Every placed card in its pocket, in pocket order. */
  cards: ReorderedCard[]
  /** The set's cards as the workspace draws them. Every other card is one it already holds. */
  added: CardSearchResult[]
}

export const BinderAddSetClient = {
  /**
   * Puts the set in the binder on the server and stores the result, answering with the new layout.
   *
   * Where it goes depends on what the server has, so the caller settles any pending save first --
   * see `save.afterSettling`. By the time this resolves the new layout is already stored.
   */
  async addSet(binderId: number, request: AddSetRequest, signal?: AbortSignal): Promise<AddSetResponse> {
    // The binder travels in the URL only; the server takes the route's id over the body's.
    const response = await fetch(`/api/binderCards/${binderId}/addSet`, {
      method: 'POST',
      credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify(request),
      signal,
    })

    if (!response.ok) {
      throw new Error(`Failed to add the set (${response.status} ${response.statusText}).`)
    }

    return (await response.json()) as AddSetResponse
  },
}
