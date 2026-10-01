import { BinderReorderClient } from '../../../clients/BinderReorderClient'
import { binderPage } from '../../../stores/binder-page.svelte'
import { save } from '../../../stores/save.svelte'
import type { SortCriterion } from './reorder-criteria'

/**
 * Reorders the binder on the server and shows the result.
 *
 * Three steps, in an order that matters. Whatever the debounced save is holding goes first, since
 * the server sorts what it has. The reorder then runs with the save queue held, so nothing lands
 * behind it. Only once the server has answered does the binder on screen change -- to the order the
 * server stored, recorded so one Undo lays the old arrangement back out.
 *
 * @throws When the pending save or the reorder itself fails. Nothing on screen has changed then,
 * and nothing on the server beyond whatever pending save did land.
 */
export async function reorderBinder(criteria: readonly SortCriterion[]): Promise<void> {
  const binderId = binderPage.binderId

  if (binderId === null) return

  const response = await save.afterSettling(() => BinderReorderClient.reorder(binderId, criteria))

  binderPage.adoptReorder(response.cards)
}
