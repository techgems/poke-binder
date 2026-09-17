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
