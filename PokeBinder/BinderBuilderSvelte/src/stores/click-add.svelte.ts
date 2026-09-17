/**
 * Whether a plain click moves cards.
 *
 * With it on, clicking a tray tile sends the card to the first free pocket and clicking a placed
 * card sends it back to the tray — quick, and the fastest way to fill a page. With it off a click
 * only lifts the card for a look, and moving one takes a deliberate action: a drag, or one of the
 * buttons on the tile.
 *
 * Off is the default because the fast behaviour is also the easy one to trigger by accident, and a
 * card that moved on a stray click is a card the user then has to find again.
 *
 * A module rather than a prop: the switch lives in the action rail and is read down in the pocket
 * grid and the tray strip, which share nothing closer than the root component.
 */

let enabled = $state(false)

export const clickAdd = {
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
