/**
 * Whether a click on a placed card lifts it into the spotlight rather than acting on it.
 *
 * On, clicking a placed card opens the spotlight -- larger and tilting -- and the hover toolbox
 * is gone, so a click is never ambiguous. Everything else stays available: dragging between
 * pockets and from the tray, click-add, undo and redo, reorder and add-set. A tray tile is never
 * lifted, in either mode; the spotlight is for placed cards only.
 *
 * Off is the default: a click on a placed card follows click-add, and the toolbox is how a card
 * is acted on with click-add off.
 *
 * A module rather than a prop, like click-add: the switch lives in the action rail and is read by
 * the pockets, the spotlight and the toolbox, which share nothing closer than the root component.
 */

let enabled = $state(false)

export const spotlight = {
  /** Read inside a component or a `$derived` to stay reactive. */
  get enabled(): boolean {
    return enabled
  },

  set enabled(value: boolean) {
    enabled = value
  },

  toggle(): void {
    enabled = !enabled
  },
}
