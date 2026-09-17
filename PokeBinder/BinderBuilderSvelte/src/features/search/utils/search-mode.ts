// Which filtering system drives the Add Cards tab. The search tab picks a mode before it can decide
// what a search even means, so the type it picks from does not belong inside the selector that
// renders the switch.

/** The filtering systems the Add Cards tab can be driven by. Exactly one is ever active. */
export type SearchMode = 'simple' | 'advanced'

interface SearchModeOption {
  value: SearchMode
  label: string
}

export const SEARCH_MODES: SearchModeOption[] = [
  { value: 'simple', label: 'Simple Search' },
  { value: 'advanced', label: 'Advanced Filters' },
]

/**
 * Simple search is where the search tab opens: two typed fields ask less of someone who already
 * knows what they are looking for, and starting here also means the advanced filter options are
 * not fetched until somebody actually switches to them.
 */
export const DEFAULT_SEARCH_MODE: SearchMode = 'simple'
