// Art the app falls back to, served by the host rather than bundled.
//
// At src root because both tabs draw cards: the binder and the card tray each showed their own copy
// of this path, and the next person to move the file would have had five lines to find.

/** Stand-in art for cards the catalog has no image for. */
export const CARD_BACK_URL = '/images/TcgImages/card-back.png'
