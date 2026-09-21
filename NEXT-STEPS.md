# Next steps — temporary

Working notes for the next few pieces of the binder workspace. **Delete this file once the list is
empty**; anything here that turns out to be a lasting rule belongs in `CLAUDE.md` instead.

Each item records what was asked for and where the code currently sits, so the work can start
without re-deriving it. Open questions are called out rather than answered.

**When a step is finished, drop it and renumber what is left from 1** -- the list is what there is
still to do, and a step that is built belongs in the code and its comments rather than here. A list
whose first item is numbered 3 is already telling the reader it has not been kept. Renumbering means
the cross-references too: the steps cite each other by number, and a stale one sends the next reader
to the wrong section. **Ask before doing it**, in the same breath as reporting the step done:
"finished" is a judgement for the person who asked for it, and not every step is finished the
moment it runs.

---

## 1. Third tab: binder settings

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

## 2. LoadSet: loading a set from a CSV

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

## 3. Saving the binder

**What it hangs off already exists** — the trigger for a save is an entry landing on the history
stack, and `stores/history.svelte.ts` is where that happens. It is numbered after LoadSet because
the file is append-only, not because it is last in line.

Nothing in the workspace is saved today. The SPA holds the binder it was handed at startup and every
edit since, and a reload throws all of it away: part 1 below is built, and no client code calls it
yet.

The three whole-binder writers this step was designed to sit beside are gone, all on 2026-09-21,
none of them ever called by anything: `SaveBinderTray` and its controller, because the tray is not
separable from the pages that spend copies out of it, and `SaveBinderCards` with `BindersController`,
because nothing needs a whole-binder card write. **`SaveBinderChanges` is now the only thing that
writes a binder's contents** — which is worth knowing while reading the rest of this section, since
it was written when it was one writer of three.

Three parts, in order:

1. **The endpoint and the slice** that takes the contract below and writes it, cancellation
   included.
2. **The debounce**, driven off the history stack, ending in a stubbed request that logs rather than
   fetches.
3. **The wiring**, which replaces the stub with a real call against the finished contract.

### The trigger is the history stack, not the stores

The history stack pushes its entries from inside the store methods, so that the completeness of
the history can be checked by reading `tray.svelte.ts` and `binder-page.svelte.ts` rather than every
component that calls them. The save trigger inherits that argument for free:
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
page two of a fifty-page binder does not post fifty pages back. The two endpoints that existed when
this was written were the exact opposite of it — whole-binder PUTs whose own documentation said
"send the whole binder, not the page being edited" — which is why this began as a new slice beside
them rather than a change to either. Both have since been deleted, so it is not beside anything.

**A partial payload has to say which pockets it covers, not only which cards it carries.** To a
whole-binder handler, a stored pocket the body does not mention is a pocket the user emptied, and it
is deleted; give such a handler one page and it empties the other forty-nine. Under this
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
is looking at. The history store settles both halves of that already. Undo turns to the page it
changed before applying it, so the spread on screen is again the spread being edited; and its entry carries the pocket
indexes it touched anyway, so a payload that would rather name the pages it actually touched than
the ones on screen can be built from the entry instead of from `spreadPages`.

### 3.1 — the debounce and the stub

A store beside the other two, holding the dirty flag, the timer, and the captured scope. The request
function has its final signature and its final return type from the start and logs the payload
instead of fetching it, so part 3 replaces a body and not a caller. The debounce window is 5–10
seconds; pick one number, name it, and put the reason next to it.

**Nothing bounds a run of resets except the user.** Somebody holding down a quantity stepper defers
the save for as long as they keep going, and a tab closed mid-run loses all of it. Whether there is a
ceiling — a maximum wait after the first pending change, regardless of what arrives after it — is
worth settling here rather than discovering later.

### 3.2 — the endpoint and the slice

One route taking both halves in one body, so one debounce tick is one request and one transaction.
Two calls, cards then tray, can half-fail: placing a card spends a copy out of the tray, so a
committed page write with a failed tray write leaves that copy both placed and still waiting.

The validator is the established pair — ownership first, answering for someone else's binder exactly
as it answers for a missing one, then the page numbers against the binder's page count, then the
pocket indexes against the pages the request claims, which is stricter than a bound against the
binder's capacity, then the tray quantities and the existence of every card id in the catalog.

**Cancellation is the part the debounce makes ordinary.** `ct` was already threaded through the
handlers this was modelled on, so the mechanism is nothing new; what changes is that an abandoned
request stops
being an exception. A request aborted after `SaveChangesAsync` has returned is a save that happened
and a client that does not know it — harmless only while every payload is a complete snapshot of the
scope it claims, which is the standing reason to keep them that way.

The slice gets tests, on the in-memory provider, per "Rules for tests" in `CLAUDE.md`. **The test
this whole contract exists to make possible is that pockets outside the claimed pages survive the
save** — a partial payload that quietly empties the rest of the binder is the failure mode, and it is
the one nobody would notice until a binder came back short.

### 3.3 — wiring it up

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
- **How a resize writes its re-flow.** Settled that it is not this contract and not a whole-binder
  card write either: those endpoints were deleted rather than kept for it. Step 1's rearrangement
  re-flows every page and changes the binder's own dimensions, so it needs `SaveBinder` and
  something that moves cards, and what that something is has not been decided. Whatever it is must
  not be able to run against the debounced save.
- **A maximum wait**, as above.
- **What the user is told when a save fails**, and whether it retries. The workspace has nowhere to
  say "not saved" today.
- **What a reload loses.** The stack does not survive one: the cards come back from the server and
  the history does not, so undo after a reload has nothing behind it. Worth confirming that reads
  acceptably once a save can be in flight as the tab goes.
