import { createToaster } from '@skeletonlabs/skeleton-svelte'

/**
 * The workspace's toasts. One store for the whole app, rendered once by `App.svelte`.
 *
 * `bottom-end`, which is the corner furthest from everything the workspace puts under the cursor —
 * the action rail is top-left, the tray strip runs along the bottom of the binder, and the tab
 * strip is at the top. `overlap` so a run of them stacks into one card instead of climbing the
 * side of the screen; a save that fails tends to fail again, and three identical notices in a
 * column say no more than one.
 *
 * It is a plain module rather than something a store owns, because a toast is UI: the save
 * scheduler counts its failures and says nothing about how they are shown.
 */
export const toaster = createToaster({
  placement: 'bottom-end',
  overlap: true,
  // Zag's default is 24, and the cap is a queue rather than a limit: what arrives past it waits
  // its turn instead of being dropped. Three, because a run of failed saves says nothing more at
  // twenty-four than at three, and anything queued behind them is stale news by the time it shows.
  //
  // Dismissal is on a timer zag drives with requestAnimationFrame, so it only runs while the page
  // is painting -- a tab in the background keeps its toasts until it is looked at again, which is
  // the behaviour you want and also why a cap is worth having.
  max: 3,
})
