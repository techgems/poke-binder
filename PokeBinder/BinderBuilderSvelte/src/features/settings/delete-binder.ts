import { BinderClient } from '../../clients/BinderClient'
import { binderPage } from '../../stores/binder-page.svelte'
import { save } from '../../stores/save.svelte'

/** Where the workspace goes once its binder is gone. */
const AFTER_DELETE_URL = '/my-binders'

/**
 * Deletes the open binder and leaves for My Binders.
 *
 * Through `save.afterSettling`, like the reorder: whatever the debounced save is holding goes
 * first, and the queue is held while the delete is out, so no page save can land after it and be
 * refused against a binder that no longer exists.
 *
 * @throws When the pending save or the delete fails. The binder is still there then, and so is the
 * page.
 */
export async function deleteBinder(): Promise<void> {
  const binderId = binderPage.binderId

  if (binderId === null) return

  await save.afterSettling(async () => {
    await BinderClient.delete(binderId)

    // Gone on the server, so nothing this page holds is worth sending -- not even the save a
    // closing tab fires, which would be refused against a binder that no longer exists.
    save.retire()
  })

  // replace rather than assign: the page being left is a binder that no longer exists, and Back
  // from My Binders should not land on its 404.
  window.location.replace(AFTER_DELETE_URL)
}
