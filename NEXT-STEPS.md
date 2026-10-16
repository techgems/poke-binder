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

## 1. Copy and paste

A toolbox action that **copies a card**, and a **Paste** on empty pockets that places a copy of it.

### What it does

- **Copy** holds the card until another is copied, so one copy can be pasted into **as many empty
  pockets as you like**.
- **Paste appears on hover over an empty pocket** while there is something to paste, in the same
  toolbox position as on a full one.
- **A paste is the card only:** the same catalog card, with no label and not missing, whatever the
  source pocket had.
- **A paste never touches the tray.** The copy is a new placement, not a card taken from anywhere.
- **Undoable and saved** like any other slot change. The toolbox is hidden in Spotlight mode, so
  neither action is reachable there -- the mode's own guard, not a separate one.

### Where it plugs in

- **What is copied** is a module like `click-add.svelte.ts`, holding one `CardSearchResult`. It
  lives for the page, so it is per binder and gone on a reload.
- **A paste** is `changeSlot(index, card)` inside `history.act`, the same path a tray drop takes
  minus the tray.
- **The same card in several pockets** is already allowed: placing a tray card with a quantity
  above one does it.

### Open questions

- **Showing what is copied.** Does anything say which card is waiting to be pasted (a chip in the
  sidebar, or the card's art on the Paste button), or is the Paste button enough?

---

## 2. Card labels

A toolbox action that opens a modal to **give a card a label**: a short text and one of **8
colours**. The label shows as a chip in the card's **bottom-left corner**.

### What it does

- **A short tag, about 20 characters** ("Trade", "PSA 10"), so it fits the corner at small pocket
  sizes without covering the art.
- **One label per pocket.** On a card that already has one, the action edits it, and the modal can
  remove it.
- **The label belongs to the card in that pocket:** it travels with the card when it moves between
  pockets or is reordered, and Delete from binder takes it away. Undo restores it.
- **Copy does not carry it** (step 1).
- **The spotlight does not show it.** Spotlight mode is for looking at the art; the chip hides
  while a card is lifted.

### Where it plugs in

- **The modal** is `components/Modal.svelte` (Skeleton `Dialog` in a `Portal`). The delete-binder
  dialog in `BinderSettingsTab.svelte` uses it at `max-w-lg`.
- **Nothing like it exists yet:** no label, note or colour on a binder card anywhere -- schema,
  slices or SPA. So this one has server work, which goes after the UI per the usual order:
  - **a migration** adding the label and its colour to `binderCards` -- schema, so a migration,
    not a script;
  - the `BinderCard` entity and its mapping in `BinderDbContext`;
  - `PlacedCard` and the `SaveBinderChanges` diff and validator (length, colour from the set);
  - `BinderPlacement` in `GetFullBinder`, and the preload that carries it to the client;
  - **every slice that rewrites pockets has to carry it or it is lost:** the reorder
    (`Shared/Arrangement`, `ReorderedCard`) and `AddSetToBinder`;
  - on the client, the slot, `ArrangedPocket`, the history deltas and the save request.
- **Store the colour as a name, not a hex**, so the 8 swatches can be restyled without rewriting
  stored rows.

### Substeps

1. **The UI, with nothing wired:** the modal, the 8 swatches and the chip on the card.
2. **The schema, the slices and the client wiring**, with tests per "Rules for tests" in
   `CLAUDE.md`.

### Open questions

- **Which 8 colours.** The theme's named colours, or a set of their own?
- **Moving to the tray.** Tray entries are one per card with a count, so a labelled card sent to
  the tray has nowhere to keep its label. Is it dropped, or does Move to tray warn first?

---

## 3. One code per data source on `sets`

Before LoadSet (step 4): a catalog migration that **renames `sets.code` to `tcgPlayerCode`** and
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

## 4. LoadSet: loading a set from the TCGdex API

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
  what goes in `sets.tcgDexCode` (step 3). The TCGplayer code is usually the same id upper-cased
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
  `sets.tcgPlayerCode` (today's `code`, renamed in step 3) is what the ETL upserts sets on (`ME03`,
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
  move daily, so the load's prices are a snapshot from that day and step 5 is what refreshes them.
- **Reloading.** Does loading a set that already exists update it, or is that refused outright?

---

## 5. Refreshing a set's card prices from TCGdex

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
- **What TCGdex gives** is in step 4: the set's card list (`GET /v2/en/sets/{id}`, briefs only), and
  `pricing` on each full card (`GET /v2/en/cards/{id}`), one request per card.
- **Matching.** `thirdParty.tcgplayer` on a TCGdex card against `cards.tcgPlayerId`, which is
  `UNIQUE` -- the one key both sides already carry. The catalog set's TCGdex id is
  `sets.tcgDexCode` (step 3).

### Open questions

- **Where the prices are stored.** The catalog has no price column or table yet. Step 4 raises the
  same question; whichever step is built first answers it for both.
- **What the result reports.** Cards updated, catalog cards TCGdex had no price for, and TCGdex
  cards that matched nothing are the obvious three.

---

## 6. Move code shared between slices into each section's `Shared` folder

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
