// What the simple search is narrowed by: two typed fields, and nothing else.

/** The current state of every simple search field, keyed by field. */
export interface SimpleSearchTerms {
  cardName: string
  cardIdentifier: string
}

export function emptyTerms(): SimpleSearchTerms {
  return { cardName: '', cardIdentifier: '' }
}
