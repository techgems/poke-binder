import { defaultCriteria, type SortCriterion } from './reorder-criteria'

/**
 * What the reorder keeps between one opening of the modal and the next.
 *
 * **The last criteria applied, for this session only**: re-sorting after adding cards is then one
 * click, and a reload starts from the defaults again. Applied rather than edited -- criteria the
 * user fiddled with and cancelled were not a sort they asked for.
 */

let remembered: SortCriterion[] | null = null

// A plain copy: what the modal edits is a $state proxy, and holding on to one would let later edits
// reach back into what was remembered.
function copy(criteria: readonly SortCriterion[]): SortCriterion[] {
  return criteria.map((criterion) => ({ ...criterion }))
}

export const sortSession = {
  /** A copy of the last criteria applied, or the defaults when nothing has been yet. Safe to edit. */
  get criteria(): SortCriterion[] {
    return copy(remembered ?? defaultCriteria())
  },

  remember(criteria: readonly SortCriterion[]): void {
    remembered = copy(criteria)
  },
}
