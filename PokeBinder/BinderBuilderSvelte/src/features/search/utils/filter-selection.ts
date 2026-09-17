// What the advanced filter panel has picked, and which of those picks still describe the cards on
// offer.
//
// The rules live here rather than inside AddCardFilters.svelte because they are the answer to
// "what is this search actually narrowed by", which the search tab asks before every request. A rule
// reachable only by importing a component is one nobody can read, or test, without mounting it.

/** The one super type whose cards the Pokemon-only fields describe. */
export const POKEMON_SUPER_TYPE = 'Pokemon'

/** Along with Pokemon, the super types whose cards carry a card type. */
export const ENERGY_SUPER_TYPE = 'Energy'

/** The current state of every filter field, keyed by field. */
export interface FilterSelection {
  /** Matched anywhere in the card's name. The one typed field; everything else is a pick. */
  cardName: string
  superTypes: string[]
  generations: string[]
  series: string[]
  sets: string[]
  pokemon: string[]
  /** Rarity names rather than ids: one name is one rarity, whichever set it was printed in. */
  rarities: string[]
  cardTypes: string[]
}

export function emptySelection(): FilterSelection {
  return {
    cardName: '',
    superTypes: [],
    generations: [],
    series: [],
    sets: [],
    pokemon: [],
    rarities: [],
    cardTypes: [],
  }
}

/**
 * Whether the Pokemon-only fields (generations, Pokemon) still describe the cards on offer.
 * Selecting nothing means every super type, so the fields stay; picking anything but Pokemon
 * puts non-Pokemon cards in the results, which those fields cannot speak about.
 */
export function pokemonFieldsApply(superTypes: string[]): boolean {
  return superTypes.every((superType) => superType === POKEMON_SUPER_TYPE)
}

/**
 * Whether the Card Type field still describes the cards on offer. Only Pokemon and Energy cards
 * carry a card type, so selecting any other super type puts cards in the results that the field
 * cannot speak about.
 */
export function cardTypeFieldApplies(superTypes: string[]): boolean {
  return superTypes.every(
    (superType) => superType === POKEMON_SUPER_TYPE || superType === ENERGY_SUPER_TYPE,
  )
}

/**
 * The selection to actually search on. The user's picks stay in the UI selection so they survive
 * toggling a super type off and on, but every field the super type has hidden is dropped here —
 * the server must not narrow by filters the user can no longer see.
 */
export function effectiveSelection(selection: FilterSelection): FilterSelection {
  // Trimmed here rather than at the request, so that adding a trailing space to a name is not a
  // different search as far as the search tab is concerned.
  const applied = { ...selection, cardName: selection.cardName.trim() }

  if (!pokemonFieldsApply(selection.superTypes)) {
    applied.generations = []
    applied.pokemon = []
  }

  if (!cardTypeFieldApplies(selection.superTypes)) {
    applied.cardTypes = []
  }

  return applied
}
