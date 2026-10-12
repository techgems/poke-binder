import {
  BinderAddSetClient,
  type AddSetRequest,
  type AddSetResponse,
} from '../../clients/BinderAddSetClient'
import { binderPage } from '../../stores/binder-page.svelte'
import { save } from '../../stores/save.svelte'

/**
 * Adds a set to the binder on the server and shows the result -- the same three steps as a reorder,
 * for the same reasons (see `reorderBinder`): settle the pending save, run with the queue held, and
 * only then change what is on screen, recorded so one Undo takes the set out again.
 *
 * @throws When the pending save or the add itself fails. Nothing on screen has changed then.
 */
export async function addSetToBinder(request: AddSetRequest): Promise<AddSetResponse | null> {
  const binderId = binderPage.binderId

  if (binderId === null) return null

  const response = await save.afterSettling(() => BinderAddSetClient.addSet(binderId, request))

  binderPage.adoptAddedSet(response)

  return response
}
