# Next steps — temporary

Working notes for the next few pieces of the binder workspace. **Delete this file once the list is
empty**; anything here that turns out to be a lasting rule belongs in `CLAUDE.md` instead.

Each item records what was asked for and where the code currently sits, so the work can start
without re-deriving it. Open questions are called out rather than answered.

---

## 1. Browser-side cache for the search filters

The advanced-search options are embedded in every binder page load by `<server-preload
key="searchFilters">` (see `Pages/Binder.cshtml` and `Components/ServerPreload.cshtml`). Measured at
**141 KB uncompressed**, of which the 1162-entry pokemon list is the bulk.

Cache them in the browser instead. **Response compression is explicitly not the answer here** — it
was measured at 141 KB → 21 KB and rejected.

The groups age at very different rates, and the cache should respect that rather than expiring
everything together:

| Group | Changes |
| --- | --- |
| Pokemon | On a new game — years apart |
| Generations | With a new generation — years apart |
| Series | Every 2-3 years |
| Sets, rarity-by-set | Every 2–3 months, with each release |
| Card types, super types | Effectively never |

**Open question to settle first.** A browser cache and "no API call at all" pull against each other:
if the client already holds the pokemon list, the page should not be embedding it again, and the
client needs some way to notice when its copy is stale. The likely shape is that the page embeds a
small *version stamp per group* and the client fetches only the groups whose stamp moved — which
deliberately reintroduces an endpoint for the stale groups, in exchange for sending almost nothing
on a normal load. Decide that trade before writing code, and decide where the stamps come from (ETL
run date, a row count, a table checksum).

Storage choice is also open: `localStorage` is simplest and synchronous but capped around 5 MB and
string-only; IndexedDB handles the size comfortably and is async.

---

## 2. Bug: the tray strip does not scroll when it is full

In the binder view, `BinderTrayStrip.svelte` should scroll horizontally once the tiles overflow, via
the left/right arrows. It does not.

What was verified earlier, and so is probably not the cause: the `<ul>` does overflow (scrollWidth
4489 against clientWidth 1047 with 30 tiles), a real mouse click on the right arrow did scroll it,
and the disabled states were correct at both ends.

Things worth suspecting:

- `[scrollbar-width:none]` hides the scrollbar deliberately, so a strip that *is* scrollable looks
  exactly like one that is not. Check whether the arrows are the only affordance and whether they
  are actually reachable/enabled in the failing case.
- `syncEnds()` runs on `scroll` and from an `$effect` via `tick()`. If it runs before layout settles
  the arrows can come back disabled and stay that way — which reads as "does not scroll".
- The tiles size from `basis-[calc((100%-3.36rem)/7)]` with `min-w-20 max-w-32`. Percentage bases
  inside a scroll container are worth re-measuring after the spread work changed the panel's height.

Reproduce first with enough cards to overflow, and check `scrollWidth`, `clientWidth`, and both
arrows' `disabled` before changing anything.

---

## 3. "Click-add mode"

Today a click always moves cards: a tray tile goes to the first free pocket
(`binderPage.placeInFirstFreeSlot`), and a placed card returns to the tray
(`binderPage.returnToTray`). Good behaviour, wrong default — it should be something the user turns
on deliberately.

- Add a **click-add mode** toggle to the floating action sidebar (`ActionSidebar.svelte`), which is
  currently inert and is the natural home for it.
- **Mode on:** today's behaviour, unchanged.
- **Mode off:** clicking a card opens the spotlight (`CardSpotlight`) instead of moving anything.
- **Mode off, tray cards:** a hover overlay on the tile offering two explicit actions — *add to the
  next open slot* and *remove from the tray*. So the click-move behaviour is still reachable without
  the mode, just never by accident.

Note that the click handlers currently double as the keyboard-reachable path for drag and drop
(`BinderPage.svelte`, `BinderTrayStrip.svelte`). Whatever replaces them must keep a non-drag route
to both actions, or the feature becomes mouse-only.

---

## 4. The tray heading should collapse the strip

In `BinderTrayStrip.svelte` only the Hide/Show button toggles the panel. Clicking anywhere in the
section heading should do the same. Keep the button — it is what announces the state to a screen
reader — and make the surrounding header a click target for the same action.

---

## 5. Third tab: binder settings

A third tab beside Card Tray and Binder (`WorkspacePanel.svelte`), for the binder's own properties:
name, dimensions (grid size), page count, and whatever else belongs to the binder rather than its
cards. `SaveBinder` already accepts all of these, and `GetFullBinder` already returns them.

**The hard part is resizing.** Changing the grid or the page count invalidates where every card
sits: pockets are stored as one absolute index across the binder, so a different page size moves
every card after the first page. The user has to choose, and the choice has to be explicit:

1. **Auto-rearrange** — keep the cards in their current order and re-flow them into the new shape.
2. **Empty the binder into the tray** — every placed card goes back to the tray and the binder is
   laid out again from scratch.

Neither should ever happen silently. Note that `SaveBinder`'s validator already refuses a resize
that would strand placed cards ("This binder holds N cards at that size, and M cards sit past
that"), so that path has to be reconciled with whichever option the user picks — the refusal is
correct for a bare resize and wrong once the user has consented to a rearrangement.

**Card reordering as a feature is deliberately undefined for now** and will be specified later; do
not invent an ordering model beyond what option 1 needs.
