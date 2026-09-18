# Next steps — temporary

Working notes for the next few pieces of the binder workspace. **Delete this file once the list is
empty**; anything here that turns out to be a lasting rule belongs in `CLAUDE.md` instead.

Each item records what was asked for and where the code currently sits, so the work can start
without re-deriving it. Open questions are called out rather than answered.

---

## 3. A history of what the user did, to drive undo and redo

The two buttons already exist and do nothing: `App.svelte` renders Undo and Redo with no `onclick`,
and `Components/Binder/BinderSideToolbar.cshtml` carries the same pair with `disabled="true"`. This
step is what puts something behind them.

**The actions worth tracking**, and where each one happens today:

| Action | The call | Reached from |
| --- | --- | --- |
| Add a card to the tray | `tray.add` | `AddToTrayButton`, and the spotlight in `SearchTab` |
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
to walk back a single drop. One user action has to close as one entry whatever the mechanism; the
next section picks one — a depth counter over a buffer the inner calls append to.

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

### What an entry holds: the deltas of one action

**Settled: an entry records what changed, not what the binder looked like.** A snapshot of the
slots and the tray per step is the whole binder per keystroke to describe a single pocket changing,
and the binder is the half that does not fit — `slots` is every page, so a run of quantity steps on
one tray row would each copy a 540-pocket array that none of them touched. An entry is instead the
list of edits the action made, each one carrying enough to run backwards.

There are two kinds of edit in the whole system, because there are two stores:

```ts
type Delta =
  | { store: 'tray'; card: CardSearchResult; at: number; from: number; to: number }
  | { store: 'slot'; index: number; from: CardSearchResult | null; to: CardSearchResult | null }

interface HistoryEntry {
  deltas: Delta[]
}
```

**Both are a before and an after of one thing, rather than a verb.** An addition is a delta whose
`from` is `0` or `null`; a removal is one whose `to` is. That is deliberate: undo applies every
`from` and redo applies every `to`, so one apply function serves both directions and no verb needs
an inverse written for it and kept in step with it. A vocabulary of `"addition"` and `"removal"` is
the same information with a second thing to get wrong, and it still would not describe the common
case — placing a card the tray holds four copies of is neither, it is `4 -> 3`.

**One action is a list of them, not one per store.** Dragging a card from the tray onto an occupied
pocket is three: the displaced card back into the tray, the pocket's contents changing, and the
placed card's tray count going down. A pocket-to-pocket drop is two slot deltas, which is the swap
`dropOnSlot` actually performs. Undo walks the list in reverse applying each `from`; redo walks it
forward applying each `to`.

Two details the shapes carry for a reason:

- **The tray delta holds the card and its row.** Undoing a removal has to put the entry back, and
  the tray is insertion-ordered — `tray.add` appends, so a restore without `at` moves a row the user
  never moved to the bottom of the list. The card itself has to travel because by then the search
  results that produced it are long gone; it is a reference to the object the tray already held, not
  a copy of it.
- **Pockets are addressed by absolute index**, the same number every mutating method and the server
  take. Page and grid coordinates are derived from `cardsPerPage`, and putting them in the entry
  only means converting back on undo against whatever the grid is by then.

**Emptying the tray is one delta per entry it held**, each `to: 0`. That is a long list, and it is
still exactly what changed rather than a picture of everything that did not — the case that looked
like it forced snapshots does not.

Two consequences worth stating, because both are load-bearing:

- **Applying a delta must not record one.** The apply path writes `slots` and the tray's entries
  directly rather than calling `add`/`setQuantity`/`remove`, which would both push entries of their
  own and re-clamp the values. Bypassing `clamp` and `MAX_CARDS` is correct here by construction:
  every `from` and `to` is a value the store was already in.
- **An action that changes nothing records nothing.** A drop on the pocket a card came from, an
  `add` into a full tray, a `setQuantity` on a card the tray does not hold — each returns without
  touching state, so it produces no deltas, and an entry with an empty list is not pushed. Undo
  never spends a press walking back something the user could not see happen. This also settles the
  nesting problem above: deltas accumulate into a buffer as the inner calls make them, and the
  outermost call commits the buffer as one entry.

**One thing this hands step 6 for free:** an entry knows which pockets it touched, because its slot
deltas carry their indexes. Step 6 has an off-screen undo either turning to the page it changed or
the save payload naming the pages it actually touched instead of the ones on screen; under deltas
the second is `entry.deltas` filtered to `store: 'slot'` and mapped through `cardsPerPage`, not a
new thing to track.

### The rest, settled

- **The store lives beside the other two** — a rune store next to `tray.svelte.ts` and
  `binder-page.svelte.ts`, which is the established shape.
- **The stack is capped at 100 entries**, oldest dropped. A delta is a handful of fields and a
  reference to a card the stores are holding anyway, so 100 costs almost nothing and is already far
  past what anyone walks back by hand. The cap exists so a long session has a ceiling at all, not
  because 100 is a number anybody will reach.
- **History does not survive a reload.** The stack starts empty and the baseline is the binder the
  server handed over. Persisting it would mean serialising the card in every delta and then
  answering what happens when the stack disagrees with what the save in step 6 actually committed —
  a reconciliation nobody has asked for, to undo past a reload nobody has asked for either.
- **Undo turns to the page it changes.** The entry carries the pocket indexes it touched, so the
  page is `Math.floor(index / cardsPerPage) + 1` and the spread follows from that; move there before
  applying, and only when the change is off-screen. A press that silently edits page eleven while
  the user is on page two is a press that reads as broken.
- **Undo and Redo are wired in the SPA only**, on `App.svelte`'s pair plus Ctrl/Cmd+Z and
  Ctrl/Cmd+Shift+Z. `BinderSideToolbar.cshtml` keeps `disabled="true"`: it renders on pages that
  never boot the SPA, where there is no history to act on and nothing its buttons could call.

The one thing still open is the resize above, which belongs to step 4 rather than here.

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

---

## 6. Saving the binder

**This follows step 3, not step 5** — the trigger for a save is an entry landing on the history
stack, so there is nothing to hook until undo and redo exist. It is numbered last because the file
is append-only, not because it is last in line.

Nothing in the workspace is saved today. The SPA holds the binder it was handed at startup and every
edit since, and a reload throws all of it away: `BinderCardsController` and `BinderTrayController`
exist and no client code has ever called them.

Three parts, in order:

1. **The debounce**, driven off the history stack, ending in a stubbed request that logs rather than
   fetches.
2. **The endpoint and the slice** that takes the contract below and writes it, cancellation
   included.
3. **The wiring**, which replaces the stub with a real call against the finished contract.

### The trigger is the history stack, not the stores

Step 3 argues that history entries have to be pushed from inside the store methods, so that the
completeness of the history can be checked by reading `tray.svelte.ts` and `binder-page.svelte.ts`
rather than every component that calls them. The save trigger inherits that argument for free:
**listen to the history store, not to the stores it watches.** An action that records itself
schedules its own save, and there is one list of mutating calls to keep complete instead of two.

It is three events rather than one, though. A new action pushes; undo and redo push nothing — they
move a pointer — and all three arm the timer, because all three change what is in the binder. The
events that *clear* the stack (deleting the binder, possibly a resize) are not save triggers, and
clearing must not swallow a timer that is already armed: the edits before the clear still happened.

**Successive actions reset the timer**, so a run of quantity steps costs one save and not four. A
page flip flushes it instead of resetting it, and **a flip with nothing pending sends nothing at
all** — which means the scheduler needs a dirty flag rather than just a timer, cleared when a save
comes back rather than when one is sent.

### What one save carries

| Half | What it is |
| --- | --- |
| The tray | every entry the tray currently holds — card and quantity, the whole list |
| The open pages | the pockets of the one or two pages in the open spread, and their contents |

The point is what it leaves out: **the binder's other pages are not in the payload**, so editing
page two of a fifty-page binder does not post fifty pages back. Both existing endpoints are the
exact opposite of this — whole-binder PUTs whose own documentation says "send the whole binder, not
the page being edited" — so this is a new slice beside them rather than a change to either.
`SaveBinderChanges.cs` is an empty `internal class` stub sitting in `PokeBinder.Features/Binder/`
already, which is presumably where it was always going.

**A partial payload has to say which pockets it covers, not only which cards it carries.** In
`SaveBinderCards.Handler` a stored pocket the body does not mention is a pocket the user emptied,
and it is deleted; give that handler one page and it empties the other forty-nine. Under this
contract an emptied pocket and an unsent pocket look identical unless the request states its scope,
so the page numbers travel with it and the index range is arithmetic over the grid — page n is
pockets `(n - 1) * cardsPerPage` through `n * cardsPerPage - 1`. The diff then runs inside that
range and nothing outside it is even read.

The tray goes whole because it has no comparable scope to claim, and it is small: `MAX_CARDS` is 500
entries, one row each.

**Which pages the payload names is decided when the timer is armed, not when it fires.** Built from
`binderPage.spreadPages` at fire time it would name whichever spread is open by then: arm a save on
page two, flip to page four, and the request posts page four's pockets and loses the edit on page
two — while also claiming a scope it never touched, which means the diff would clear page four. The
flush-on-flip rule is what keeps one pending save to one spread, so that rule is load-bearing rather
than a nicety, and the scope still wants capturing at arm time to say so in code.

Undo and redo are the harder version of the same thing: an undo can change a pocket on a page nobody
is looking at. Step 3 settles both halves of that. Undo turns to the page it changed before applying
it, so the spread on screen is again the spread being edited; and its entry carries the pocket
indexes it touched anyway, so a payload that would rather name the pages it actually touched than
the ones on screen can be built from the entry instead of from `spreadPages`.

### 6.1 — the debounce and the stub

A store beside the other two, holding the dirty flag, the timer, and the captured scope. The request
function has its final signature and its final return type from the start and logs the payload
instead of fetching it, so part 3 replaces a body and not a caller. The debounce window is 5–10
seconds; pick one number, name it, and put the reason next to it.

**Nothing bounds a run of resets except the user.** Somebody holding down a quantity stepper defers
the save for as long as they keep going, and a tab closed mid-run loses all of it. Whether there is a
ceiling — a maximum wait after the first pending change, regardless of what arrives after it — is
worth settling here rather than discovering later.

### 6.2 — the endpoint and the slice

One route taking both halves in one body, so one debounce tick is one request and one transaction.
Two calls, cards then tray, can half-fail: placing a card spends a copy out of the tray, so a
committed page write with a failed tray write leaves that copy both placed and still waiting.

The validator is the established pair — ownership first, answering for someone else's binder exactly
as it answers for a missing one, then the page numbers against the binder's page count, then the
pocket indexes against the pages the request claims, which is a stricter check than
`SaveBinderCardsValidator`'s capacity bound, then the tray quantities and the existence of every
card id in the catalog as `SaveBinderTray` already does.

**Cancellation is the part the debounce makes ordinary.** `ct` is already threaded through both
existing handlers, so the mechanism is nothing new; what changes is that an abandoned request stops
being an exception. A request aborted after `SaveChangesAsync` has returned is a save that happened
and a client that does not know it — harmless only while every payload is a complete snapshot of the
scope it claims, which is the standing reason to keep them that way.

The slice gets tests, on the in-memory provider, per "Rules for tests" in `CLAUDE.md`. **The test
this whole contract exists to make possible is that pockets outside the claimed pages survive the
save** — a partial payload that quietly empties the rest of the binder is the failure mode, and it is
the one nobody would notice until a binder came back short.

### 6.3 — wiring it up

A client beside `CardSearchClient.ts`, same shape as the others: `credentials: 'same-origin'`, an
`AbortSignal`, and a thrown error on a non-`ok` response. One save in flight at a time, with a new
one aborting the previous.

**The SPA does not know its own binder id.** `binderPage.load` reads `columns`, `rows` and `pages`
off the preloaded summary and drops `summary.id` on the floor; `binderId` appears nowhere in `src`
except a comment. The store has to keep it before anything can address an endpoint with it.

### Open questions

- **What "the list of what is currently in the binder" means.** Read as the tray it is the tray's
  whole contents, which is what the rest of this step assumes and what the stated goal implies — not
  sending every page. Read as the placed cards it is the whole binder again, which is the thing this
  contract is designed to avoid. Settle this before the contract is written down anywhere.
- **What a closing tab loses.** Up to the whole debounce window. A `pagehide` or `visibilitychange`
  flush is the usual answer, but `navigator.sendBeacon` only POSTs, so either the endpoint grows a
  POST alias or the flush is an unawaited `fetch` with `keepalive`. Leaving `/Binder` through the
  Razor sidebar is a full navigation and has the same problem.
- **Whether a resize fits this contract at all.** Step 4's rearrangement re-flows every page and
  changes the binder's own dimensions, which is `SaveBinder` plus a whole-binder card write — the
  existing endpoints, not this one. So the partial save is not the only writer, and the two must not
  be able to run against each other.
- **A maximum wait**, as above.
- **What the user is told when a save fails**, and whether it retries. The workspace has nowhere to
  say "not saved" today.
- **What a reload loses.** Step 3 settles that the stack does not survive one: the cards come back
  from the server and the history does not, so undo after a reload has nothing behind it. Worth
  confirming that reads acceptably once a save can be in flight as the tab goes.
