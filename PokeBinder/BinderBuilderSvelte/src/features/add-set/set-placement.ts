import type { ExistingCards } from '../../clients/BinderAddSetClient'
import type { CardSearchResult } from '../../clients/CardSearchClient'

/**
 * Where a whole set would land in the binder, worked out in the browser so the tab can show it and
 * ask about it before anything is sent. The server does the placing -- AddSetToBinder.PlanPlacement,
 * which this mirrors -- and this only previews it.
 */

export type { ExistingCards }

/** What one page of the binder holds once the set is in, for the page map. */
export type PageKind = 'existing' | 'set' | 'buffer' | 'blank'

export interface SetPlacementOptions {
  /** Cards in the set, after any rarities have been left out. */
  setCards: number
  /** Empty the binder before the set goes in. */
  emptyFirst: boolean
  /** Ignored when the binder is emptied first, or is empty already. */
  existing: ExistingCards
}

export interface SetPlacement {
  /** Cards placed in the binder now. */
  placed: number
  /** Pages up to and including the last one holding a card; 0 for an empty binder. */
  usedPages: number
  /** The page the set's first card goes on, counted from one. */
  startPage: number
  /** The page its last card goes on. */
  endPage: number
  /** Pages the binder has to have for all of it to fit. */
  pagesNeeded: number
  /** Pages that would have to be added at the end, 0 when it fits as it is. */
  pagesToAdd: number
  /** What each page holds afterwards, page 1 first, across the binder as it would be. */
  pages: PageKind[]
}

export function setPlacement(
  slots: readonly (CardSearchResult | null)[],
  cardsPerPage: number,
  pageCount: number,
  options: SetPlacementOptions,
): SetPlacement {
  let placed = 0
  let lastIndex = -1

  slots.forEach((card, index) => {
    if (!card) return

    placed += 1
    lastIndex = index
  })

  const usedPages = lastIndex < 0 ? 0 : Math.floor(lastIndex / cardsPerPage) + 1

  const setPages = Math.max(1, Math.ceil(options.setCards / cardsPerPage))

  // What is left of the binder's own cards once the options have had their say.
  const keptPages = options.emptyFirst ? 0 : usedPages
  const existing = keptPages === 0 ? 'after' : options.existing

  const pages: PageKind[] = []

  const push = (kind: PageKind, count: number) => {
    for (let i = 0; i < count; i++) pages.push(kind)
  }

  // Sending the binder's cards to the end keeps them in their pages, moved back as whole pages, so
  // a card keeps its place on its page and its neighbours.
  if (existing === 'end') {
    push('set', setPages)
    push('existing', keptPages)
  } else {
    push('existing', keptPages)
    if (existing === 'buffer') push('buffer', 1)
    push('set', setPages)
  }

  const pagesNeeded = pages.length

  push('blank', Math.max(0, pageCount - pagesNeeded))

  const startPage = pages.indexOf('set') + 1

  return {
    placed,
    usedPages,
    startPage,
    endPage: startPage + setPages - 1,
    pagesNeeded,
    pagesToAdd: Math.max(0, pagesNeeded - pageCount),
    pages,
  }
}
