/**
 * Whether the user has asked the OS for reduced motion, tracked live.
 *
 * hover-tilt does not check this itself, so anything that tilts has to gate on it. Kept here rather
 * than inside a component so every tilt surface answers the question the same way.
 */

// Guarded for the case where this module is ever evaluated outside a browser; matchMedia only
// exists on window.
const query =
  typeof window === 'undefined' ? null : window.matchMedia('(prefers-reduced-motion: reduce)')

let reduced = $state(query?.matches ?? false)

// Deliberately never removed. The query outlives every component that asks about it, and one
// listener for the page is cheaper than adding and dropping one per card in a grid of hundreds.
query?.addEventListener('change', (event) => (reduced = event.matches))

/** Read inside a component or $derived to stay reactive to the setting changing mid-session. */
export function prefersReducedMotion(): boolean {
  return reduced
}
