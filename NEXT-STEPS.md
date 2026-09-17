# Next steps — temporary

Working notes for the next few pieces of the binder workspace. **Delete this file once the list is
empty**; anything here that turns out to be a lasting rule belongs in `CLAUDE.md` instead.

Each item records what was asked for and where the code currently sits, so the work can start
without re-deriving it. Open questions are called out rather than answered.

---

## 1. Make the filter-cache code readable

The filter cache works, end to end, and `GetSearchStarterFilters` reads like something to be decoded
rather than something to be followed. This is the pass that fixes that, and nothing is blocking it:
the admin screen, the endpoint and the Svelte side are all in, so the request shape has stopped
moving.

**Nothing about the behaviour changes.** The stamp stays in the cache key, a group with no stamp row
stays uncached, every response still carries the full stamp map, and a group that is not being sent
is still null rather than empty. This is about the shape of the code, and the shape that wins here
is the one the remaining slices should follow.

`PokeBinder.Features.Tests` exists for this refactor: fifteen tests go through `Handler` with a
`Request` and assert on the `Response`, touching none of the slice's internals. Each one was
checked against a deliberately broken slice, so a green run afterwards means something. Start by
running them, and treat a test that has to change to accommodate the new shape as a behaviour change
that needs deciding on rather than editing away.

What is actually tangled, all of it in `PokeBinder.Features/CardSearch/GetSearchStarterFilters`:

- **The handler says the same thing five times.** Each group is a six-argument `ReadAsync` call that
  names its group three times over — once for `Stale`, once for the cache key, once in the request
  property it reads — and carries the stamps dictionary, the cache and a lambda for its own query.
  Five lines that differ in two words each are a list wearing a disguise.
- **`ReadAsync` does three jobs**: honours a decision already made (`bool wanted`), looks the group
  up under a stamped key, and runs the query. The `wanted` parameter is the tell — it exists because
  the decision could not be expressed where the reading happens.
- **The rule for what comes back is spread across three places**: `Request.IsCold`, the `Stale`
  local function, and the ternaries inside `new Response(...)`. Answering "when do super types come
  back?" means holding all three in your head at once.
- **The response is built positionally** out of seven nullable lists in an order that does not match
  the order the groups are read in. It is the same hazard the old seven-argument constructor had.
- **The request's property names have drifted apart**: `SuperTypesCacheByPass` against
  `CardTypesCacheBypass`. Whatever else happens, those two should agree.

Shapes worth weighing, none of them chosen: one descriptor per group — the group, the stamp the
caller sent, the query — walked once; splitting "which groups does this request entitle you to" from
"read one group, cached"; a named builder in place of the positional response. The five mapping
expressions at the foot of the file are fine as they are and predate this work.

The rest of the filter-cache code is small enough to leave alone: `BumpFilterCacheStamps`,
`GetFilterCacheStamps`, `CardFiltersController`, the admin page and the client's `filter-cache.ts`
are each one thing.

---

## 2. Organise the Svelte app

`src/lib` holds twenty files in one folder — sixteen components and four modules that are not
components at all — and nothing in the folder says which part of the app any of them belongs to.
`AddCardFilters.svelte` and `BinderPageNav.svelte` are neighbours despite belonging to different
tabs, and `tray.svelte.ts` sits between them despite belonging to both.

**Components split three ways by where they are used**: general, the Card Tray tab, the Binder tab.
**Stores get a directory of their own**, because crossing tabs is what they are for and filing them
under either one would be a lie. **Preloads get a directory of their own too** — a loader for data
the page embedded is not a client, and neither belongs next to components. There are three kinds of
module on that side and they are worth naming: a **client**, which knows how to call an endpoint; a
**preload**, which reads what the page already embedded; and a **dynamic preload**, which fills the
same role as a preload but has to go and get it.

**The scale of this step is deliberate: moves, and small extractions that follow from them.** It is
not a rewrite of the front end. Where a file is doing two jobs and one of them plainly belongs
somewhere else, that job moves with it — the `filter-cache.ts` split below is the size meant.
Anything larger gets its own step and its own brief rather than being smuggled in here, because a
reorganisation that also rewrote behaviour would be unreviewable: nobody could tell a move from a
change in the diff.

The map below is what the imports actually say today, not a guess at intent:

| Now | Belongs to | Because |
| --- | --- | --- |
| `AddCardsWorkspace`, `AddCardFilters`, `FilterCombobox`, `CardTypeFilterGroup`, `SearchModeSelector`, `SimpleSearchFilters`, `AddToTrayButton`, `CardTray`, `QuantityStepper` | Card Tray tab | every one of them is reached only through `AddCardsWorkspace` |
| `filter-option.ts` | Card Tray tab | imported by `AddCardFilters`, `CardTypeFilterGroup`, `FilterCombobox`, and nothing else |
| `BinderView`, `BinderPage`, `BinderPageNav`, `BinderTrayStrip` | Binder tab | reached only through `BinderView` |
| `WorkspacePanel`, `ActionSidebar`, `Modal` | general | `App.svelte` renders them around both tabs |
| `tilt/CardSpotlight`, `tilt/TiltCard`, `tilt/prefers-reduced-motion` | general | `CardSpotlight` is used by `AddCardsWorkspace` and `BinderView`, and the other two back it |
| `tray.svelte.ts`, `binder-page.svelte.ts`, `click-add.svelte.ts` | stores | `tray` alone is imported by seven files across both tabs and by `binder-page` |
| `clients/preload.ts` | preloads | it is not a client: nothing is requested, it reads what the Razor page already put on the document |
| `clients/filter-cache.ts` | splits in two — see below | it is doing storage and orchestration at once |
| `clients/CardSearchClient.ts` | stays the client | it is the one module here that talks to the API |

### The rules that settle it, and what is left open

- **Components file by where they are used, not by what they could be reused for.**
  `QuantityStepper` is a plain number control that anything might want, and it goes under the Card
  Tray tab because that is the only place using it. A component that grows a second caller in
  another tab moves to general on the day it does, which is a two-line change, and until then the
  folder tells the truth about the app rather than about an intention.
- **A client and a preload loader are different things, and they stop sharing a folder.**
  `CardSearchClient` requests something; `preload.ts` requests nothing at all -- it reads what the
  Razor page wrote onto the document before the app booted. Preloads get their own directory.
- **`filter-cache.ts` is doing two jobs and keeps one.** It should be the storage layer and nothing
  more: take what the preload carried, put it in `localStorage`, and read back what is there.
  Deciding whether anything is stale, calling the client and merging what comes back is the other
  job, and it moves to a **dynamic preload** — the file that executes the client, reading storage to
  find out whether it has to. That is the third classification, and it is why the preload directory
  is not just for the static one: both exist to hand the app data it can use, one off the document
  and one off the network.

  This is the one part of this step that is a rewrite rather than a move: `AddCardsWorkspace` calls
  `loadCachedSearchFilters` and `refreshSearchFilters` today, and both of those names belong to the
  job that is leaving.
- **Naming.** Something like `lib/common`, `lib/search`, `lib/binder`, plus `src/stores` and
  `src/preloads`, but the names are worth agreeing before twenty files move rather than after.
- **Barrels.** No `index.ts` re-exports anywhere in the app today. Adding them would shorten the
  imports this step is about to rewrite; leaving them out keeps every import pointing at a real file.

### Code worth moving, not just files

Found by reading the imports rather than by taste. Ordered by what they cost, and the first three
are the same size as the moves around them:

- **`CARD_BACK_URL` is declared five times** — `AddCardsWorkspace`, `BinderPage`, `BinderTrayStrip`,
  `BinderView` and `CardTray` each carry their own `const CARD_BACK_URL =
  '/images/TcgImages/card-back.png'`. It is a path the server serves; one module, five imports, and
  the next person to move that file changes one line instead of finding five.
- **Domain rules are being exported out of components.** `AddCardFilters.svelte` opens with seventy
  lines of `<script module>` before the component starts: `FilterSelection`, `emptySelection`,
  `pokemonFieldsApply`, `cardTypeFieldApplies`, `effectiveSelection` and the two super-type
  constants. `AddCardsWorkspace` imports four of them — which is to say the rule about *which
  filters still apply* is reachable only through a UI file, cannot be read without opening one, and
  cannot be tested without mounting one. The same shape, smaller, in four more components:
  `SimpleSearchFilters` (`SimpleSearchTerms`, `emptyTerms`), `SearchModeSelector` (`SearchMode`,
  `SEARCH_MODES`, `DEFAULT_SEARCH_MODE`), `WorkspacePanel` (`WorkspaceTab`,
  `DEFAULT_WORKSPACE_TAB`) and `CardTypeFilterGroup` (`CardTypeOption`). A component should be
  imported for what it renders.
- **`toRequest` and `toSimpleRequest`** sit privately inside `AddCardsWorkspace` and do one thing:
  turn the string-valued UI selection into the number-valued shape the API takes. Pure functions,
  no reactivity, and they belong with the selection model they translate — or beside the client
  whose contract they satisfy.

And one that is bigger than this step, listed so it is not rediscovered later:

- **The search runner inside `AddCardsWorkspace`.** Of that file's 460 lines, 278 are script, and
  most of the script is search policy rather than rendering: the debounced `$effect`, the arming
  rule that stops an untouched advanced filter fetching page one of the catalog, the key that
  dedupes repeat searches, the `AbortController` bookkeeping that keeps a slow response from landing
  on top of a fast one, plus `search()`, `clearResults()` and the paging state. That is a coherent
  thing — "run a card search, and keep the results honest" — living inside a component that also
  lays out three columns and renders a spotlight. Extracting it is the largest single-responsibility
  win in the app and the one most likely to change behaviour by accident, so it wants its own step.

Possibly, and only worth it if a third caller appears: both tabs keep their own `spotlit` state and
build a near-identical `<CardSpotlight>` — same `src`/`alt`/`label`/`tilt`/`onclose` wiring, same
fallback art, different payload type and different buttons underneath. The differences are real, so
this is a "twice is a coincidence" case rather than an extraction that pays for itself now.

### Doing it safely

Every import in the app is a relative path, so this rewrites import lines in every file that moves
and every file that referred to one. `npm run check` in `PokeBinder/BinderBuilderSvelte` is the
proof — `svelte-check` reports unresolved imports as errors, and it currently runs clean.

The stylesheet does not care: `app.css` names the two Razor trees and the Skeleton package in
`@source`, and the SPA's own files are found by Tailwind's scan of this project, so moving files
inside `src/` changes nothing about what is generated.

**This is why it comes before the undo step.** That step adds another store, and the first thing it
needs is somewhere to put it.

---

## 3. A history of what the user did, to drive undo and redo

The two buttons already exist and do nothing: `App.svelte` renders Undo and Redo with no `onclick`,
and `Components/Binder/BinderSideToolbar.cshtml` carries the same pair with `disabled="true"`. This
step is what puts something behind them.

**The actions worth tracking**, and where each one happens today:

| Action | The call | Reached from |
| --- | --- | --- |
| Add a card to the tray | `tray.add` | `AddToTrayButton`, and the spotlight in `AddCardsWorkspace` |
| Add a card to the binder | `binderPage.dropOnSlot` with a tray source, `binderPage.placeInFirstFreeSlot` | a drop on a pocket; the place buttons in `BinderTrayStrip` and `BinderView` |
| Remove a card from the tray | `tray.remove` | `BinderTrayStrip`, and the spotlight in `BinderView` |
| Move a card between pockets | `binderPage.dropOnSlot` with a slot source | a drop on another pocket |
| Take a card out of the binder | `binderPage.returnToTray`, `binderPage.dropOnTray` | the take-out button in `BinderPage`, the spotlight in `BinderView`, a drop on the tray strip |
| Change how many copies are in the tray | `tray.setQuantity` | the stepper in `AddToTrayButton` and `CardTray` |
| Empty the tray | `tray.clear` | the Clear button on the tray panel |

### None of these is a single change

Placing a card spends a copy out of the tray, and if the pocket was occupied it puts the displaced
card back in the tray — one action, three edits across two stores. Taking a card out is the same
transfer run backwards: the pocket is emptied and a copy appears in the tray. A pocket-to-pocket
drop is a *swap*, not a move, which is convenient (its own inverse) but is not what "move a card"
sounds like. An entry that records only the binder side will restore the pockets and leave the tray
wrong, which is worse than not having undo at all.

Placing and taking out are inverses, which matters for what undo *applies*, not for what gets
recorded: both are events a user performs and both push an entry. It is just that the code undo runs
for one is the code the other already contains, provided the entry knows which copy went where.

### The list is now every way the state changes, and it has to stay that way

Between them those seven cover every mutating call on `tray` and `binderPage` except `load`, which
sets the opening state from what the page preloaded and is the baseline the first undo stops at
rather than an action of its own. `startDrag`/`endDrag` and `goBack`/`goForward` are not on the list
because they change no cards — one is drag bookkeeping, the other is which spread is open.

**That completeness is the whole soundness argument, and it is fragile.** An edit that reaches the
tray or the slots without recording an entry leaves the stack describing a binder that no longer
exists, and undo then restores a state the user was never in. The failure is silent and shows up
long after the change that caused it, so a new mutating call is a new history entry in the same
commit — not a follow-up.

**Every change of quantity is its own entry, including a run of them on the same card.** Taking a
card from one copy to four records four times, and undo walks back a copy at a time. Folding a run
into one entry would read better and costs more to build — something has to decide what closes the
run and hold an entry open while it waits — and the simpler version is the one worth having here.

That also covers both ways a count moves, because they are the same call underneath: `tray.add` on a
card already in the tray does not append, it hands off to `setQuantity` with one more than the
current count. Record there and the Add button and the stepper are both accounted for.

### Where the entry gets pushed from

The seven actions above are reached from a dozen call sites across six components, so pushing the
entry at the call site means remembering it in twelve places, and the invariant above holds only as
long as nobody forgets. Pushing it inside the store methods instead means an action records itself
however it was triggered — a drag, a button, click-add mode — and the completeness of the history
can be checked by reading `tray.svelte.ts` and `binder-page.svelte.ts` rather than every component
that touches them.

**The catch is that those methods call each other.** `dropOnSlot` calls `tray.add` for the displaced
card and `tray.setQuantity` to spend the copy; `dropOnTray`, `returnToTray` and
`placeInFirstFreeSlot` all reach into the tray as well. Recording inside every mutating method would
push two or three entries for one thing the user did, and undo would then need two or three presses
to walk back a single drop. Whatever the mechanism — an outermost-call wrapper, a snapshot taken
before the first mutation of an action, a depth counter — one user action has to close as one entry.

### Two events that are not in the table yet

- **Resizing the binder**, from the settings tab in step 4. Changing the grid or the page count
  re-flows every placed card, or empties the binder into the tray — a larger change than anything
  listed above, and it arrives from a different tab. It either pushes an entry of its own or clears
  the history, and that is worth settling while step 4 is still being designed rather than after.
- **Deleting the binder**, the trash button in `App.svelte`'s sidebar, which is as inert today as
  Undo and Redo. When it is wired it acts on the binder itself rather than on its cards, so undo
  cannot reach back across it; it should clear the stack.

Nothing else can reach this state. `startDrag` is only ever called with a slot source from
`BinderPage` or a tray source from `BinderTrayStrip`, so a search result cannot be dragged into a
pocket without passing through the tray first, and there is no entry point hiding in the results
grid.

### Open questions

- **Inverses or snapshots.** Because every action spans two stores, a snapshot of the slots and the
  tray per step is the obviously-correct version; the cost is memory, and `slots` is the whole
  binder rather than the open spread. Worth measuring against a full binder before ruling it out,
  rather than reaching for commands because snapshots sound expensive. Emptying the tray is the case
  that argues hardest for them: its inverse is every entry that was in it, with its quantity, which
  is a snapshot whatever it gets called.
- **Nothing is saved yet.** The SPA does not call `BinderCardsController` or `BinderTrayController`
  at all — the endpoints exist and no client code posts to them. When that is wired, both are
  whole-binder PUTs, so undo needs no endpoint of its own: it changes the same client state and the
  same save follows. It does decide whether history should survive a reload, and today nothing does.
- **Where the store lives.** A rune store beside `tray.svelte.ts` and `binder-page.svelte.ts` is the
  established shape.
- **Depth**, and whether an undo that changes a page the user is not looking at should turn to that
  page first — a change off-screen reads as a button that did nothing.
- **Keyboard shortcuts**, and whether the Razor toolbar in `BinderSideToolbar.cshtml` gets wired or
  stays disabled, since it renders on pages that never boot the SPA.

---

## 4. Third tab: binder settings

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

---

## 5. LoadSet: loading a set from a CSV

`Pages/Admin/LoadSet.cshtml` is still the empty stub it has always been. It becomes the page that
loads a new set, taking that job off the ETL: a Razor page on `_AdminLayout`, built from Static
Components with HTMX doing the posting, and Alpine only where a control needs local state of its own.
Desktop only, like every admin page.

The form is a CSV file, a set name, a date published, and a series — **prefilled with the series
currently running**, since that is the answer almost every time.

**Uploading is two steps, not one.** The file is validated and what it would do is reported back
first; only then does a second action commit it. A CSV that is half wrong should cost the person a
glance, not an undo.

### What the CSV actually is

The files in `TCGCSV/` are what this takes, and `CardSetCsvLoader` plus
`ModernPokemonSetsCsvMapper` already read that shape: `productId`, `name`, `extNumber`, `extRarity`,
`extCardType`, `extHP`, `extStage`, `extCardText`.

**It is one row per printing, not per card, and that is the trap.** `SV01ScarletAndVioletBaseSet`
has 444 rows for 258 cards: 186 `productId`s appear two or three times, differing only in
`subTypeName` — Normal, Holofoil, Reverse Holofoil. `cards.tcgPlayerId` is `UNIQUE`, so loading the
file row by row collides. The ETL never had to say what it wanted here because it upserts
`.On(x => x.TcgPlayerId)` and the last row quietly wins; a page with a validation step should decide
this out loud instead.

`CardCsvUtilsService.MapCsvCardTypeToDbCardType` is the existing card-type derivation (Energy,
Trainer, Pokemon, else the `UNKNOWN` placeholder). `UNKNOWN` is the value the search filters exclude
from super types, so rows that land on it are rows nobody will be able to filter for.

### What the validation step should report

Enough for someone to say yes with their eyes open — the counts first, then anything that will be
wrong afterwards. Worth flagging, and the list is open:

- **Rows that collapse into one card**, and whether the duplicates agree on name, number and rarity.
  Two printings of Sprigatito are fine; two rows claiming different rarities for one `productId` are
  not.
- **Rows whose card type maps to `UNKNOWN`** — they import, and then they are invisible to the
  filters.
- **Missing `extNumber` or `extRarity`.** `cards.cardNumber` and `cards.rarity` are `NOT NULL`, so a
  blank is an insert failure, not a blank card.
- **`productId`s already in the catalog**, which means either a re-upload or a set overlapping one
  that is already loaded.
- **A set name or code that already exists.**
- **The summary itself**: rows, distinct cards, distinct rarities, and the rarities by name — eight
  in SV01, and they become the `rarityBySet` rows this set is filtered by.

### The slice

One vertical slice under `PokeBinder.Features/CardAdmin/` — `UpsertCardSet.cs` and
`AddCardSeries.cs` are empty stubs sitting there already. It writes the set, its cards and its
`rarityBySet` rows, and **bumps `Sets` and `RarityBySet` in the same transaction as the rows it
writes**. Split across two transactions, a crash in between leaves new cards in the database with
every browser still holding the old stamp — permanently stale, with nothing surfacing it.
`BumpFilterCacheStamps.Handler` takes both groups in one call and saves them together.

Validation is its own thing rather than a step inside the write: the page needs the report without
committing anything, so whatever produces it has to be callable on its own. `FluentValidation` is
already in `PokeBinder.Features` and the binder slices pair a `Handler` with a `Validator`, though a
per-row CSV report may not fit that shape — worth deciding rather than assuming.

The slice gets tests. See "Rules for tests" in `CLAUDE.md`.

### Open questions

- **Where do `code` and `fullName` come from?** The form gives a name, a date and a series, but
  `sets.code` is what the ETL upserts sets on (`ME03`, `SV01`), and `fullName` is the long form
  ("Mega Evolution: Perfect Order" against "ME: Perfect Order"). Either the form grows two fields or
  something derives them.
- **Which series is "currently running"?** `series.endDateUnix` holds **0** for the open one, not
  null, even though the entity types it `long?` — today that is `Mega Evolution`, and a
  `EndDateUnix == null` check would find nothing at all. Is the open series the one with 0, or simply
  the latest `startDateUnix`?
- **Card art.** The ETL downloads it per card from TCGplayer (`TcgPlayerImgDownloadService`) and
  fills `imageUrl`. Does an upload do that too, leave the cards imageless until a later pass, or
  queue it?
- **Card text.** `pkmnCardText` and `nonPkmnCardText` are written by the ETL from the same CSV
  columns. In scope here or not?
- **Re-uploading.** Does loading a set that already exists update it, or is that refused outright?
- **The file itself.** Nothing in this app uploads anything yet, so the multipart form, the size
  limit and the antiforgery token on it are all new ground — and it is worth deciding whether the
  CSV is kept anywhere after the import or simply read and dropped.
