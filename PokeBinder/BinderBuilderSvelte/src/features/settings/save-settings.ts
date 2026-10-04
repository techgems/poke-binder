import { BinderClient, type BinderUpdateRequest } from '../../clients/BinderClient'
import { binderPage } from '../../stores/binder-page.svelte'
import { save } from '../../stores/save.svelte'

/** How long the success toast is on screen before the page refreshes under it. */
export const REFRESH_AFTER_SAVE_MS = 2_500

/**
 * Stores the binder's new settings, then refreshes the page a moment later.
 *
 * Through `save.afterSettling`, like the reorder: whatever the debounced save is holding goes
 * first, because a rearrangement lays out what the server has. Once the edit is stored the save
 * store is retired before the queue is let go -- the cards on screen are laid out for the old size,
 * and any save of them, the one the refresh itself would fire included, would put that layout back
 * over the new one. The refresh is what brings the page up to date, the binder's name in the app
 * bar and the title with it, which is why nothing in this tab is ever undone.
 *
 * @param onSaved Called once the edit is stored, before the refresh -- where the toast goes.
 * @throws When the pending save or the edit fails. Nothing has changed on the server then beyond
 * whatever pending save did land, and the page stays as it is.
 */
export async function saveBinderSettings(
  request: BinderUpdateRequest,
  onSaved: () => void,
): Promise<void> {
  const binderId = binderPage.binderId

  if (binderId === null) return

  await save.afterSettling(async () => {
    await BinderClient.update(binderId, request)

    save.retire()
  })

  onSaved()

  setTimeout(() => window.location.reload(), REFRESH_AFTER_SAVE_MS)
}
