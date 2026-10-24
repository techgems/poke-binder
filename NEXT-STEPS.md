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

## 1. Card labels

Short tags you give the cards in a binder -- up to **three per card**, in the pocket's bottom-left
corner -- picked from a list of labels **the binder owns**. A label is a short name and one of
**eight colours**; the collector creates them, applies them to as many cards as they like, and
manages them from Settings. Rename a label and every chip of it in the binder follows; delete it
and every assignment of it goes with it.

The per-pocket mockup (first attempt, in the code since commit 82079ba) and the binder-scoped UI
reshape (substep 1 below, in the code since the local commit after it) are both done. **Substeps 2–3
are the server work; substep 4 is the client wiring that replaces the session-only state with the
real round trip.**

### What it does

- **A chip per label**, each a short tag (around 20 characters) in a bright background, in the
  pocket's bottom-left corner. Three chips fit side-by-side at the standard pocket size and wrap
  onto two lines when a tag is long. Missing's chip stays bottom-right.
- **Up to three labels per card.** The schema's `CHECK` and the validator both say so; the picker
  disables the fourth row while three are checked.
- **The picker lives in the Label modal**, opened from the Tag button in the toolbox. Two tabs:
  **Assign** (default) shows the labels on this card at the top and the labels available in the
  binder below; drag from Available onto the card, or click a row to add. Each chip on the card
  has an X that returns it to Available; Clear all empties the card side in one press. **Create
  label** is a tab for the inline create form; Save there creates the label on the binder (one
  dedicated slice call -- see below) and, as long as the card has room, applies it to this
  pocket, then switches back to Assign.
- **Pending-until-close for freshly created labels.** A label created in the modal is pending
  while the modal is open: dropping it off the card with its chip-X before the modal closes
  discards it entirely rather than returning it to Available, which is the typo / wrong-colour /
  accidental-creation escape. Once the modal closes, pending labels are committed and that path
  is gone.
- **Settings owns rename, recolor and delete.** A Labels section in `BinderSettingsTab.svelte`
  lists every label with its name, swatch and usage count, with per-row Edit (inline rename and
  recolor) and Delete -- Delete asks first with the count ("Delete 'Trade'? This removes it from
  12 cards."). Settings' Save button is what sends the labels sync -- edits do not fire per
  press.
- **Label names are unique per binder**, enforced in the validator and by a `UNIQUE(binderId,
  name)` index.
- **No cap on how many labels a binder has.** Collector prunes from Settings.
- **A label stays once created**, even when no card uses it; orphans are not auto-cleaned.
- **Labels travel with their card** on a drag, a reorder and `returnToTray` -- same rule the
  missing flag already follows. **Deleting the card drops its labels** (the delete and the
  paste-conflict Remove both set the slot to null, so the assignments go with it). **Copy does
  not carry them**, matching the copy-no-label rule for everything that is not on the catalog
  card.
- **The spotlight does not show the chips.** Spotlight is for looking at the art.
- **The eight colours stay as the Tailwind-named set** -- red, orange, amber, green, teal, blue,
  purple, pink. The user does not pick their own, so the eight are what they are and new ones can
  be added without rewriting any stored row.
- **Spotlight / Click-add mutex** stays in effect; label edits happen through the toolbox and the
  modal, not through plain card clicks.

### Where it plugs in

- **The chip and the picker** live in `BinderPage.svelte` (chips in the pocket) and
  `LabelModal.svelte` (the Assign + Create tabs). Both exist today in the substep-1 shape.
- **The Settings section** is in `BinderSettingsTab.svelte` as its own panel. Also already in the
  substep-1 shape; substep 4 swaps its per-edit writes for the Save-button sync.
- **The join table carries the three-cap.** A per-pocket array on the client, a join row per
  assignment on the server, so a cascade on the card or on the label cleans itself up.

### Substeps

1. ✅ **(DONE) Binder-scoped labels, UI only.** `stores/labels.svelte.ts` holds two SvelteMaps:
   the binder's labels (keyed by a local id, carrying name and color) and the pocket assignments
   (keyed by pocket index, with an ordered list of up to three label ids). `LabelModal.svelte` has
   the Assign / Create tabs with drag-and-drop between the two lists, pending-until-close for
   freshly created labels, and chip-X-discards-pending. `BinderSettingsTab.svelte` has the Labels
   panel. `binder-page.svelte.ts` clears or swaps pocket assignments on delete, return-to-tray,
   drops and paste; substep 4 moves those into `changeSlot`. Per-session only; nothing posted.
2. ✅ **(DONE) Schema and entity.** Migration under `PokeBinder.Migrations/Scripts/PokeBinder/`
   (`005_CreateBinderCardLabels.sql`) created `binderCardLabels` and
   `binderCardLabelAssignments` with the composite FK on `(binderId, indexInBinder)`,
   `UNIQUE(binderId, name)`, cascades on both sides and the `CHECK` on `orderIndex`.
   `BinderCardLabel` and `BinderCardLabelAssignment` entities and their mappings in
   `BinderDbContext.cs`; `BinderCard.LabelAssignments` and `Binder.Labels` navigations added.
3. ✅ **(DONE) Slices, validators and tests.** `CreateBinderCardLabel` with validator and
   `POST /api/binders/{id}/labels`. `SaveBinderChanges.SavedCard` carries `LabelIds`; the handler's
   `SaveLabelAssignmentsAsync` diffs per claimed pocket, the validator rejects dangling ids,
   duplicates per pocket, and lists past the three-cap. `UpdateBinder.Request` carries a
   `labels: { id, name, color }[]` sync; `SyncLabelsAsync` renames, recolors and deletes by
   omission; validator rejects dangling ids, duplicate names in the leftover state, blank names
   and unknown colours. `ReorderedCard` carries `LabelIds` with value-equal `Equals`;
   `BinderArrangement` loads label assignments, carries them through sort and rewrites them in
   `LayOut`; `ReorderBinder`, `AddSetToBinder` and `UpdateBinder` all `Include` the assignments.
   `GetFullBinder` returns the binder's labels and each `BinderPlacement.LabelIds`. 32 new tests
   cover every rule above; all 197 tests pass.
4. ✅ **(DONE) Client wiring.** Three write paths; only one of them calls the dedicated label slice.
   - **Pending-labels staging stays client-only.** `pocketLabels.pending: SvelteMap<number,
     Label>` with local negative ids, separate from the committed `labels`. The picker merges
     them. Chip-X on a pending label drops it from both maps; nothing is sent.
   - **Create-and-apply (modal) → `CreateBinderCardLabel` right away.** On success, swap the
     negative id for the real id in `pocketAssignments`, move the entry from `pending` into
     `labels`, record a `LabelCreateDelta`. On failure, error toast ("Could not create '{name}'.
     Try again."), pending entry dropped, no card change.
   - **Modal close** records the assignment changes in history and arms the debounced
     `SaveBinderChanges`, which now carries each affected pocket's `labelIds` alongside its card
     and missing flag. One save, same existing debounce window.
   - **Settings panel's labels list** round-trips through `SaveBinderSettings` on its Save button:
     the full `labels` field with every id, carrying any renames and recolors; omission is a
     delete. Settings' "New label" still calls `CreateBinderCardLabel` first so the id exists
     before Save sends the sync.
   - **`SlotDelta` gains `labelIds: readonly number[]`** on `ArrangedPocket`, next to the missing
     flag. `pocketDiffers` compares the arrays. The explicit `pocketLabels.clearPocket` /
     `swapPockets` calls in `binder-page.svelte.ts` go away -- `changeSlot` carries the whole
     slot (card + missing + labelIds).
   - **Preload**: `PreloadedBinder` lands the binder's labels and each pocket's `labelIds` at
     load, replacing the substep-1 local-only state.
   - **History deltas**:
     - Assignment changes: existing `SlotDelta` with the new `labelIds` field. Fully undoable via
       the existing save flow.
     - `LabelCreateDelta`: one entry per `CreateBinderCardLabel` success.
     - Settings edits (rename, recolor, delete) are not Ctrl+Z undoable in this round; Settings
       has its own Save and Discard controls, and the delete has the count-aware confirmation.

---

## 2. One code per data source on `sets`

Before LoadSet (step 3): a catalog migration that **renames `sets.code` to `tcgPlayerCode`** and
**adds `tcgDexCode`**, and the changes to carry both through the app. Today's one code is
TCGplayer's (`ME02`, `ME-AH`); TCGdex has ids of its own (`me02`), and more data sources may follow,
so each source gets its own column rather than a single code that means whichever source wrote it.

### Where it plugs in

- **The migration** goes in `PokeBinder.Migrations/Scripts/TcgCatalog/`, after
  `010_AddRarityOrdersToRarityBySetFilterOption.sql`. `code` is defined in
  `001_CreateTcgCatalogSchema.sql` as `TEXT UNIQUE NOT NULL`, so the rename keeps that constraint.
- **Filling `tcgDexCode` for the sets already loaded is catalog data, not schema**, so per "Rules
  for catalog data" in `CLAUDE.md` it goes in a re-runnable script in `Scripts/`, not in the
  migration. The column has to allow null until that script has run, and for any set TCGdex does
  not have.
- **The entity and mapping.** `Set.Code` in `PokeBinder.TcgCatalog.DbContext/Entities/Set.cs`, and
  `HasColumnName("code")` in `TcgCatalogDbContext.cs`.
- **What reads `Code` today:**
  - the ETL, which upserts sets on it (`PokeBinderETL/Db/Repositories/CardUpsertService.cs`,
    `.On(x => x.Code)`, then looks sets back up by it);
  - `GetRarityBySetForAdminEdit` and its `RaritySetChoice` model, and `RaritySetEditor.cshtml.cs`,
    which shows it beside the set name on `/admin/setRarity`;
  - the test fixtures that build sets (`CatalogFixture.cs`, `BinderFixture.cs`,
    `GetSearchStarterFiltersTests.cs`).
- **The scripts in `Scripts/` join on `[sets].[code]`** -- `SeedRarityWeights.sql` resolves every
  set by it, and so does `RemoveVstarTokenFromBrilliantStars.sql`. They have to move to the new
  column name, or the next run of either fails.

### Open questions

- **Is `tcgDexCode` unique?** Unique where it is set, like `tcgPlayerCode`, or not constrained.

---

## 3. LoadSet: loading a set from the TCGdex API

`Pages/Admin/LoadSet.cshtml` is still the empty stub it has always been. It becomes the page that
loads a new set, taking that job off the ETL: a Razor page on `_AdminLayout`, built from Static
Components with HTMX doing the posting, and Alpine only where a control needs local state of its own.
Desktop only, like every admin page.

**The source is the TCGdex API (`https://api.tcgdex.net/v2/en/...`), not a CSV file.** The form
names a TCGdex set rather than taking an upload, alongside a set name, a date published, and a
series — **prefilled with the series currently running**, since that is the answer almost every
time. The CSVs in `TCGCSV/` and the ETL's CSV readers (`CardSetCsvLoader`,
`ModernPokemonSetsCsvMapper`) are not part of this.

**Loading is two steps, not one.** The set is fetched and what it would do is reported back first;
only then does a second action commit it. A set that comes back half wrong should cost the person a
glance, not an undo.

### What TCGdex gives

Checked against the live API on 2026-09-30; worth re-checking when the work starts.

- **A set** is `GET /v2/en/sets/{id}`: `id` (`me02`), `name`, `releaseDate` (`2025-11-14`),
  `serie` (`{ id, name }`), `abbreviation.official` (`PFL`), `cardCount`, and `cards`. The `id` is
  what goes in `sets.tcgDexCode` (step 2). The TCGplayer code is usually the same id upper-cased
  (`ME02`, `SV01`), but not always: Ascended Heroes is `ME-AH` in the catalog.
- **The set's `cards` are briefs** -- `id`, `localId`, `name`, `image` -- with no rarity or category,
  so the full card is a second request each: `GET /v2/en/cards/{id}`, one per card, 130 for ME02.
- **A card is one card, not one row per printing.** Printings are listed inside it
  (`variants_detailed`), so the CSV's "one productId on three rows" collision does not arise.
- **The TCGplayer id is `thirdParty.tcgplayer`** (`487828` for `sv01-001`), which is what
  `cards.tcgPlayerId` (`UNIQUE`, `NOT NULL`) needs. It is also repeated per variant under
  `pricing.tcgplayer`.
- **`category` is the super type directly** -- `Pokemon`, `Trainer`, `Energy` -- in place of the
  CSV's derivation through `CardCsvUtilsService.MapCsvCardTypeToDbCardType`.
- **Rarity names are not spelled like ours.** TCGdex says "Special illustration rare" where the
  catalog's TCGplayer-sourced rows say "Special Illustration Rare", and the `rarityBySet` weights and
  the rarity filters are both keyed by that name.
- **Card numbers are `localId` alone** (`129`), where the catalog holds `129/094`.
- **Art** is the card's `image` plus a quality and extension suffix (TCGdex's convention; not yet
  confirmed here).
- **Two things worth taking that the CSV never gave us: prices and artists.**
  - **The artist** is `illustrator` on the full card. `cards.artist` already exists (`Card.Artist`)
    and nothing fills it today.
  - **Prices** are `pricing` on the full card. `pricing.tcgplayer` (USD) gives per-variant prices,
    keyed `normal`, `reverse-holofoil` and so on, each with `lowPrice`, `midPrice`, `highPrice`,
    `marketPrice` and `directLowPrice`. `pricing.cardmarket` (EUR) gives `avg`, `low`, `trend` and
    the 1/7/30-day averages, with `-holo` versions of each. Both carry an `updated` timestamp. The
    catalog has nowhere to put any of this yet: no price column or table exists.

### What the validation step should report

Enough for someone to say yes with their eyes open — the counts first, then anything that will be
wrong afterwards. Worth flagging, and the list is open:

- **Cards with no `thirdParty.tcgplayer`** -- `cards.tcgPlayerId` is `NOT NULL`, so one without it
  cannot be inserted as the table stands.
- **Cards whose category is not one of the three super types** — they import, and then they are
  invisible to the filters, like the ETL's `UNKNOWN` placeholder.
- **Missing `localId` or `rarity`.** `cards.cardNumber` and `cards.rarity` are `NOT NULL`, so a
  blank is an insert failure, not a blank card.
- **TCGplayer ids already in the catalog**, which means either a reload or a set overlapping one
  that is already loaded.
- **A set name or code that already exists.**
- **The summary itself**: cards, distinct rarities, and the rarities by name -- they become the
  `rarityBySet` rows this set is filtered by, so a name that does not match the catalog's spelling
  is worth showing next to the one it probably means.

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
per-card report may not fit that shape — worth deciding rather than assuming.

The slice gets tests. See "Rules for tests" in `CLAUDE.md`.

### Open questions

- **Where do `tcgPlayerCode` and `fullName` come from?** `tcgDexCode` is the TCGdex set id, but
  `sets.tcgPlayerCode` (today's `code`, renamed in step 2) is what the ETL upserts sets on (`ME03`,
  `SV01`), and `fullName` is the long form ("Mega Evolution: Perfect Order" against "ME: Perfect
  Order"). Either the form grows two fields or something derives them.
- **Which series is "currently running"?** `series.endDateUnix` holds **0** for the open one, not
  null, even though the entity types it `long?` — today that is `Mega Evolution`, and a
  `EndDateUnix == null` check would find nothing at all. Is the open series the one with 0, or simply
  the latest `startDateUnix`?
- **Card art.** The ETL downloads it per card from TCGplayer (`TcgPlayerImgDownloadService`) and
  fills `imageUrl`. Does a load do that too, take TCGdex's `image` instead, leave the cards
  imageless until a later pass, or queue it?
- **Card text.** `pkmnCardText` and `nonPkmnCardText` are written by the ETL from the CSV. TCGdex
  carries attacks, abilities and effects on the full card. In scope here or not?
- **Rarity spelling.** Map TCGdex's names onto the catalog's ("Special illustration rare" to
  "Special Illustration Rare"), store them as TCGdex writes them, or both? Mapping is what keeps a
  new set inside the existing rarity filters and weights.
- **Card number format.** Store `localId` as it comes (`129`), or rebuild the catalog's `129/094`
  from the set's `cardCount.official`?
- **Which fields the form still needs.** TCGdex carries the set's name, release date and series, so
  the name, date and series fields could prefill from it rather than be typed. Kept as overrides, or
  dropped?
- **A card per request.** A set costs one request per card. Is that fine for an admin action, or is
  TCGdex's GraphQL endpoint worth using to fetch the set with its cards in one call? And whether
  TCGdex is called live on each load or through a client with a timeout and retry of its own.
- **What the reorder needs from it.** The reorder sorts by `sets.releaseDateUnix` and by the two
  weights on each `rarityBySet` row, so a set loaded here has to arrive with a release date
  (TCGdex's `releaseDate`, or the form's "date published") and with weights for the rarities it
  creates. TCGdex has no weights, so those still come from somewhere else. `/admin/setRarity` edits
  weights but cannot add a row, so a set loaded without them sorts as unweighted -- last, either
  direction -- for every card in it.
- **Prices: where they live and how they stay current.** They need a home in the schema, which
  variants and which market to keep, and a decision on freshness: a set is loaded once, but prices
  move daily, so the load's prices are a snapshot from that day and step 4 is what refreshes them.
- **Reloading.** Does loading a set that already exists update it, or is that refused outright?

---

## 4. Refreshing a set's card prices from TCGdex

An admin action that does one thing: **tell the backend to update the prices of every card in one
set**. It fetches that set's cards from TCGdex and writes their prices onto the catalog cards they
match. It loads nothing new. Cards are only matched and priced, and a TCGdex card with no catalog
match is left alone.

A **Razor page** on `_AdminLayout` and a **vertical slice** for the request. Desktop only, like
every admin page.

### Where it plugs in

- **The page.** `Pages/Admin/SetRarity.cshtml` is the pattern to follow: one page, a set picked from
  the catalog's sets, and HTMX posting from inside a `<form method="post">`, so the antiforgery token
  survives the swap (the comment there says why). The pages sit in `Pages/Admin/`.
- **The slice** goes under `PokeBinder.Features/CardAdmin/`, beside `UpdateRarityBySet`, with a
  validator and tests per "Rules for tests" in `CLAUDE.md`. The TCGdex call goes behind something
  the tests can replace, since a test never reaches a real service any more than it reaches a real
  database.
- **What TCGdex gives** is in step 3: the set's card list (`GET /v2/en/sets/{id}`, briefs only), and
  `pricing` on each full card (`GET /v2/en/cards/{id}`), one request per card.
- **Matching.** `thirdParty.tcgplayer` on a TCGdex card against `cards.tcgPlayerId`, which is
  `UNIQUE` -- the one key both sides already carry. The catalog set's TCGdex id is
  `sets.tcgDexCode` (step 2).

### Open questions

- **Where the prices are stored.** The catalog has no price column or table yet. Step 3 raises the
  same question; whichever step is built first answers it for both.
- **What the result reports.** Cards updated, catalog cards TCGdex had no price for, and TCGdex
  cards that matched nothing are the obvious three.

---

## 5. Move code shared between slices into each section's `Shared` folder

Scan `PokeBinder.Features` for code that more than one slice of a section uses, and move it into
that section's `Shared` folder, following "Rules for code shared between slices" in `CLAUDE.md`.
Code general to every section stays in `PokeBinder.Features/Utils` and is not part of this.
`Binder/Shared/Arrangement` is the pattern. A move changes namespaces and nothing else: no
behaviour, and the tests pass unchanged before and after.

### Already known

Found in passing, not by a scan, so the list is a start rather than the whole of it:

- **A binder's limits** -- `MaxNameLength`, `MaxDescriptionLength`, `MaxPages` -- are constants on
  `CreateBinder`, which `UpdateBinderValidator` reads.
- **Two `CardSearchResult` models**, one under `SearchCardsByFilter` and one under
  `SearchCardWithSimpleSearch`. Worth checking whether they are the same shape before deciding
  they are one model.
