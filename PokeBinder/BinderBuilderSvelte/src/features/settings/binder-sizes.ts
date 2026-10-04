import { preloadedBinderSizes, type PreloadedBinderSize } from '../../preloads/preload'

/**
 * The grids a binder can be set to: the seeded `binderSizes` rows, smallest first, as the page
 * handed them over. The same list the create form on My Binders offers.
 */

export type BinderSize = PreloadedBinderSize

export const BINDER_SIZES: readonly BinderSize[] = preloadedBinderSizes()

/** Mirrors CreateBinder.MaxPages, which UpdateBinderValidator holds an edit to as well. */
export const MAX_PAGES = 200
