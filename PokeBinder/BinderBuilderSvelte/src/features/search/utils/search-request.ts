// The translation between what the UI holds and what the search endpoints take.
//
// The filter widgets are string-valued and the API is number-valued, and one field is called
// something different on each side. Pure functions with no reactivity in them, so they sit beside
// the selection model they translate rather than inside the component that happens to call them.

import type { CardSearchRequest, SimpleCardSearchRequest } from '../../../clients/CardSearchClient'
import type { FilterSelection } from './filter-selection'
import type { SimpleSearchTerms } from './simple-search-terms'

export function toRequest(selected: FilterSelection, pageNumber: number): CardSearchRequest {
  // The filter widgets are string-valued, so every id arrives as a string; the API takes numbers.
  return {
    // Left out when the box is empty, the same way the simple search leaves it out of its URL.
    cardName: selected.cardName || undefined,
    superTypes: [...selected.superTypes],
    generations: selected.generations.map(Number),
    series: selected.series.map(Number),
    sets: selected.sets.map(Number),
    pokemon: selected.pokemon.map(Number),
    // Rarities travel as names, not ids; the rest of the fields are ids.
    rarities: [...selected.rarities],
    cardTypes: selected.cardTypes.map(Number),
    pageNumber,
  }
}

/**
 * Takes the trimmed terms the search tab searches on rather than a {@link SimpleSearchTerms}
 * straight out of the fields: what is typed and what is searched for are not the same thing, and
 * the trimming happens where the search key is built.
 */
export function toSimpleRequest(
  typed: { cardName: string; cardNumber: string },
  pageNumber: number,
): SimpleCardSearchRequest {
  // The field is labelled Card Identifier in the UI, but what the API matches it against is the
  // card number printed on the card, so it travels under that name.
  return {
    cardName: typed.cardName,
    cardNumber: typed.cardNumber,
    pageNumber,
  }
}
