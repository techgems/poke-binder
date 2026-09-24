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

## 1. One migration for the rarity seed, not eleven

`011` through `021` are eleven separate scripts that all do the same thing to the same table: write
`pullRateRarityOrder` onto `rarityBySetFilterOption`. They are eleven because they were written one
era at a time, as sources for each era turned up — measured data for the recent sets, community
counts for the old ones, a rescue job for Legendary Treasures. That is the history of the research,
not a fact about the catalog, and a database rebuilt from scratch should not have to replay it.

**Fold them into one seeding script.** `010` adds the two columns and `022` deletes the VSTAR Token;
both are different in kind and stay where they are. The eleven in between become one.

### What has to survive the fold

**The provenance, per block.** The rows are not all the same quality, and the whole value of those
eleven headers is that they say which is which. A single file listing 752 numbers with no sources is
worse than eleven that explain themselves. Four tiers, and they need to stay distinguishable after
the merge:

- **Measured** — the TCGplayer (formerly eBay) Authentication Center, which opens packs and
  publishes sample sizes and confidence intervals: the Scarlet & Violet and Mega Evolution sets,
  five Sword & Shield sets, and Fusion Strike, whose figures exist only as an infographic.
- **Community estimates** — ThePriceDex, self-described, no sample size, one source per set: Sun &
  Moon, XY, Black & White, EX, Diamond & Pearl, Platinum, HGSS, the other eleven Sword & Shield
  sets, and Shrouded Fable.
- **Hand-entered judgements**, which no source backs and which should be the easiest rows in the
  file to find: Black White Rare at 496, Holo Rare at 3 across the Sword & Shield sets, Common,
  Uncommon and Rare at 1 everywhere, and Astral Radiance's Radiant Rare taken from Lost Origin
  because its own article never measured it. The vintage WotC rows predate all of this and were
  entered by hand before any of it.
- **One rescaled set** — Legendary Treasures, whose source figures sum to 1.76 rare-slot cards per
  pack and are divided by that before use. The arithmetic has to travel with it, or the numbers
  look invented.

**The per-set arithmetic.** Most rows are a sum of sub-rarities folded into one catalog bucket:
`Ultra Rare` is V plus VMAX plus Full Art, and in Brilliant Stars the whole Trainer Gallery as well.
Without the addition written beside the value, a future reader cannot check a number against its
article — only trust it.

### The check that the fold worked

The table is the specification. Dump `(set code, rarity, pullRateRarityOrder, nameRarityOrder)` from
the live catalog first — 752 weighted rows of 789 — then rebuild a database from scratch through
the new script and diff the two dumps. They must be identical. Anything that differs is a
transcription error, which is the one real risk in copying eleven files into one.

### Open questions

- **Additive or authoritative.** All eleven use `COALESCE`, so a value already in place is never
  blanked, which is what made them safe to run in sequence. One consolidated seed has no such
  constraint and could set the column outright — including back to null. Additive is the safer
  default; authoritative is the honest one for a file that claims to be the seed.
- **Whether it carries `nameRarityOrder` as well.** Only the vintage sets and Ascended Heroes have
  one and the other hundred-odd sets are null, so the column is nearly empty either way. Leaving it
  out makes the file purely about pull rates, and means those hand-entered name orders live nowhere
  but the database.
- **What running it here does.** `011`-`021` are already journalled against this catalog, so deleting
  them leaves stale rows in `SchemaVersions` and the new script runs once and rewrites the same
  values. Harmless if it is idempotent, which it should be anyway — but worth confirming on a copy
  rather than assuming, the way every one of the eleven was.
- **What the seed does not cover, and should say so.** Thirty-seven rows are still null: the promo
  rows of nine promo and non-booster sets, which have no pull rate by definition, six EX-era Secret
  Rares that no source rates, and three odd singles. A seed silent about them reads as incomplete
  rather than as finished.

---

## 2. Reorder: sorting the whole binder

Triggered from the action sidebar (`App.svelte`, beside Undo and Redo), opening a modal —
`Modal.svelte` is already there, used at the moment only by the leftover "Search cards" placeholder.

The modal holds up to **three sorting criteria applied in sequence**: Set, Rarity, Card name. The
user **drags to set which one outranks which**, **drops any they do not want** (one, two or three is
valid), and gives each **its own direction**, ascending or descending — so "Set descending, then
Rarity ascending" is something the widget has to be able to say. Applying it **discards whatever
arrangement the binder has** and lays the placed cards out again in that order.

**There are four orderings but still three slots, because Rarity is one criterion with a choice of
key.** It sorts by *either* `pullRateRarityOrder` or `nameRarityOrder` — **never both**,
so the two are not separate criteria competing for a slot but one criterion whose widget asks which
ordering it is using. That choice is independent of the direction: pull rate ascending and pull rate
descending are both available, and so are the two for name ordering.

### Decided

- **The client sorts, and forces a whole-binder write.** Not the server. Both routes need a new
  whole-binder write regardless, because `SaveBinderChanges` refuses more than `MaxPages = 2` and a
  reorder rewrites every page; a server-side sort would *additionally* need a binder GET that does
  not exist — `GetFullBinder` reaches the SPA only as a Razor preload from `Binder.cshtml.cs` — and
  it would make the reorder **irreversible**, because reloading the binder runs `binderPage.load`,
  which calls `history.clear()`. Undo matters most for the one action whose whole purpose is
  throwing an arrangement away.
- **The ranks travel with the page, at initial load.** Not fetched when the modal opens. The binder
  page already embeds two payloads this way — the binder itself and the search starter filters — so
  this is a third beside them, the modal opens with everything it needs, and sorting costs no round
  trip at all.
- **Rarity order is already in the catalog**, as `pullRateRarityOrder` and `nameRarityOrder` on
  `rarityBySetFilterOption` — per set, because the hierarchy really does differ between sets: the
  three Mega Evolution sets each hold one or two **Mega Hyper Rare** gold cards sitting *above*
  Special Illustration Rare, while the fifteen other sets that have Special Illustration Rare have
  nothing above it. There is nothing for this step to rank, and nothing to wait for: the
  columns ship seeded, 752 rows of 789, and `/admin/setRarity` is where any of them is corrected.
- **The Rarity criterion carries which of the two it uses**, chosen in its own widget and exclusive:
  a payload saying both is not a thing the modal can produce. Two exclusive options with the chosen
  one visible at a glance is a segmented control, which Skeleton ships
  (`@skeletonlabs/skeleton-svelte`) — the same vocabulary the rest of the workspace is written in.
  It means a criterion is no longer just a name and a direction, so whatever shape the modal keeps
  its list in has to hold a key as well, and so does anything that remembers the last sort.
- **The two directions are labelled for what they do, not for which way the number goes.** Rarity
  reads **"rare first" / "common first"**. "Ascending" would have been the trap: the stored value is
  packs-to-open, so ascending is *easiest* first, and a collector asking for their chase cards at
  the front would have had to pick descending. The same principle gives the other two their words —
  Set is **"newest first" / "oldest first"** and Card name is **"A–Z" / "Z–A"** — so the labels
  differ per criterion instead of being one shared pair, which is the point. Internally each is
  still a direction on the underlying value.
- **Both rarity columns are numbered so that bigger means rarer**, which is what lets
  one label sit over both: "rare first" is the descending end whichever key the criterion is using.
  Numbered the other way round, `nameRarityOrder` would make the same label mean opposite things.
- **Set order is `releaseDateUnix` and nothing else.** It is *already* `NOT NULL` in the schema
  (`001_CreateTcgCatalogSchema.sql`); only the `Set` entity types it `long?`. **So there is nothing
  to migrate** — the change is the entity property, plus whatever reads it as nullable.
- **One history entry**, however many pockets move, so Ctrl+Z restores the old arrangement.
- **The new whole-binder write is shared with step 3.** Its rearrangement needs the same thing, so
  this step specifies it and step 3 reuses it: cards and tray in **one transaction**, because
  emptying a binder into the tray moves every card from one to the other.

### What the server side needs

1. **`Set.ReleaseDateUnix` as `long`**, matching the column that is already `NOT NULL`.
2. **`setId` on two projections, not one.** `BinderCardDetails` and `CardSearchResult` both carry
   `setName` and nothing else about the set. The binder's own cards need the id to find their rarity
   weights — and so do **cards added from search during the session**, or a card placed and then
   sorted without a reload would have nothing to sort by.
3. **The ordering data in the preload**: each set's release date, and the two rarity values keyed
   by set. A few hundred rows all told.
4. **The whole-binder write**: a slice, a validator and tests, per "Rules for tests" in `CLAUDE.md`.
   The test worth writing first is that a reorder of a fifty-page binder leaves the same set of
   cards in it, in the new order, with nothing dropped.

### What the client side needs

- The modal and the criteria widget: drag to reorder, a direction per criterion, and dropping a
  criterion without disturbing the order of the rest.
- The comparator, as one function with its tie rules written next to it.
- **Laying the cards out from pocket 0 with no gaps.** Assumed rather than asked: a sort that
  preserved holes would not look sorted. Worth confirming.
- **Settling with the debounced save before writing.** `stores/save.svelte.ts` holds a dirty flag
  and a pending scope of up to two pages; a whole-binder write that lands between an edit and that
  save's tick is overwritten by it. The reorder has to flush and wait, and the two writers must not
  be able to interleave.

### Open questions

- **Ties, and there are three kinds now.** Each needs a stated tie-break, or the same criteria
  produce a different arrangement every time they are applied. **Equal pull rates** are no longer an
  edge case: two rarities in one set may share a value on purpose, and the base rarities all share
  1, so every set has ties at the bottom by construction. **Unweighted rarities** are the 37 rows
  nothing rates -- and note that an empty value sorts *last* in a descending ordering, so the six
  EX-era Secret Rares and the promo rows currently sort below their own commons. **Sets sharing a
  release date** are the third.
- **Pockets flagged missing.** They hold a real card id, so they sort like any other card — unless a
  reserved pocket is meant to stay where it was put.
- **Whether the tray takes part.** Read as written, no: the reorder arranges the binder, and the
  tray is what has not been placed yet.
- **What undo costs here.** One entry can hold a delta per pocket — up to 4000 — and `MAX_ENTRIES`
  is 100. Worth deciding whether a reorder is capped, recorded more cheaply, or simply accepted.
- **Whether the criteria are remembered.** Per binder, so re-sorting after adding cards is one
  click, or per session, or not at all.
- **What this asks of step 4.** LoadSet has to supply a release date for every set it loads, and
  weights for the rarities it creates — `/admin/setRarity` edits rows but cannot add one, so a set loaded without them
  sorts as unweighted for every card in it.

---

## 3. Third tab: binder settings

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

**How a rearrangement writes the cards is undecided — but step 2 builds the write it needs.** The
debounced save (`SaveBinderChanges`, `PUT api/binderCards/{binderId}`) is the only thing that writes
a binder's contents today, and it refuses a payload claiming more than a spread, so a re-flow does
not fit it; the whole-binder writers that would have fitted (`SaveBinderCards`, `SaveBinderTray` and
their controllers) were deleted on 2026-09-21 rather than kept for it, because nothing had ever
called them. **The reorder needs exactly that write and is specified to add it, so build this step
after step 2 and reuse its endpoint** rather than adding a second one. Either way, whatever does it:

- **writes the cards and the tray in one transaction**, because "empty the binder into the tray"
  moves every placed card from one to the other, and half of that committing is a card both placed
  and waiting;
- **must not be able to run against the debounced save.** That save fires up to five seconds after
  an edit and again when the tab closes, so a resize that lands in between writes a layout the next
  tick overwrites with the old one. The workspace's save store (`stores/save.svelte.ts`) holds the
  dirty flag and the pending scope, so it is what a resize has to settle with before it starts.

**Card reordering as a feature is deliberately undefined for now** and will be specified later; do
not invent an ordering model beyond what option 1 needs.

---

## 4. LoadSet: loading a set from a CSV

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
