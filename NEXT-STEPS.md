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

## 1. New tab: adding whole sets to the binder

A new tab in the workspace (`WorkspacePanel.svelte`, beside Card Tray and Binder) from which a whole
set goes into the binder in one action, rather than card by card through search. **The details are
still to come** -- this entry records only why it is first and where the code sits, so nothing
beyond that should be built on it until they arrive.

**Why it comes first:** it is what makes the reorder worth trying on a real binder. The reorder is
built (`ReorderBinder`, and the modal on the action sidebar), but a sort only shows anything on a
binder holding many cards across several sets and rarities, and filling one today means adding each
card from search by hand. Being able to drop two or three whole sets into a binder gives it
something to sort.

### Where it plugs in

- **The tab itself.** `WorkspaceTab` in `components/workspace-tab.ts` is the union of panels
  (`'add' | 'binder'`); a new panel is a member there plus a `Tabs.Trigger` and a `Tabs.Content` in
  `WorkspacePanel.svelte`. Both existing panels stay mounted and that file's comment on why no
  `display` utility may go on `Tabs.Content` applies to the new one too.
- **A set's cards are more than one search page.** `SearchCardsByFilter` caps a page at
  `MaxPageSize = 200`, and SV01 alone has 258 cards, so the client either pages through the search
  or the tab gets a by-set read of its own.
- **The write already exists.** A set placed into the binder is far more than a spread, and
  `SaveBinderChanges` takes that too: beside a claim of up to two pages it accepts a claim of every
  page (`SaveBinderChanges.ClaimsWholeBinder`), as a snapshot of the whole binder with the tray in
  the same transaction. The client's save store already promotes any pending save wider than a
  spread to that claim, so placing a set is an ordinary recorded edit -- build on that rather than
  adding a third writer.

---

## 2. Another tab: binder settings

A tab beside Card Tray, Binder and step 1's set tab (`WorkspacePanel.svelte`), for the binder's
own properties: name, dimensions (grid size), page count, and whatever else belongs to the binder
rather than its cards. `SaveBinder` already accepts all of these, and `GetFullBinder` already returns them.

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

**How a rearrangement writes the cards is undecided, but the pieces it needs are built.** Two things
write a binder's contents: the debounced save (`SaveBinderChanges`, `PUT api/binderCards/{binderId}`)
and the reorder (`ReorderBinder`, `POST api/binderCards/{binderId}/reorder`). The debounced save
already accepts a claim of every page with the tray in the same transaction, which is the shape "empty
the binder into the tray" needs. But it diffs against the binder's *current* page count, so a claim
for a binder that is changing size at the same time is a new case -- whether the resize and the
re-flow go in one request, and through which slice, is the open part. Whatever does it:

- **writes the cards and the tray in one transaction**, because "empty the binder into the tray"
  moves every placed card from one to the other, and half of that committing is a card both placed
  and waiting;
- **must not be able to run against the debounced save.** That save fires up to five seconds after
  an edit and again when the tab closes, so a resize that lands in between writes a layout the next
  tick overwrites with the old one. `save.afterSettling` in `stores/save.svelte.ts` is the existing
  answer -- it sends what is pending, waits, and holds the queue while a write of its own is out --
  and the reorder already goes through it.

**Option 1 keeps the order the cards are in; it is not a sort.** Sorting is the reorder's job, with
its own criteria; the re-flow should not grow an ordering model of its own.

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
  - the test fixtures that build sets (`CatalogFixture.cs`, `GetSearchStarterFiltersTests.cs`).
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
  move daily, so the load's prices are a snapshot from that day and step 6 is what refreshes them.
- **Reloading.** Does loading a set that already exists update it, or is that refused outright?

---

## 5. A/B: the card tray beside the binder instead of under it

A **dev-only switch** between two layouts of the Binder tab, so the two can be tried side by side
and the better one kept. Today the tray is a strip along the foot of the binder; the alternative is
a column along its left side that **works the same way, scrolling vertically instead of horizontally**.
Nothing about what the tray does changes, only where it sits. The goal is to find out which layout
makes for the better UX.

### Where it plugs in

- **The layout.** `features/binder/BinderView.svelte` stacks the page and the tray as a column
  (`flex flex-col`), with `BinderTrayStrip` last. The side variant is the same two children in a
  row, tray first.
- **The strip is horizontal all the way through.** `BinderTrayStrip.svelte` scrolls with
  `overflow-x-auto`, decides whether its arrows are live from `scrollLeft` / `scrollWidth`
  (`syncEnds`), pages with `scrollBy({ left })`, uses left/right chevrons, and folds away with a
  vertical `slide`. Every one of those has a vertical counterpart -- `scrollTop`, `scrollHeight`,
  `scrollBy({ top })`, up/down chevrons, `slide` with `axis: 'x'` -- and they come as a set, so it
  is worth deciding whether that is one component with an orientation or two.
- **What has to keep working in both.** Dragging a card out of a pocket onto the tray
  (`dropOnTray`), dragging from the tray into a pocket, click-add mode, the spotlight on click, the
  strip expanding when a drag reaches it while empty, and the `ResizeObserver` that re-measures
  when the hidden tab comes back (see the comment above it in `BinderTrayStrip.svelte`).
- **The pockets size themselves off the space left.** `BinderPage.svelte` measures its panel in
  `cqw` / `cqh`, so a tray taking width rather than height gives the pages a narrower, taller box
  -- on a wide screen that may suit a spread better, which is part of what is being tested.

### Decided

- **The switch is a `sessionStorage` flag named `leftSideTrayOn`, for now.** No UI and nothing from
  the server: it is set by hand from the console, and an absent flag means today's strip along the
  foot. Session storage keeps the choice across reloads of the same tab, and a new tab starts on the
  default again. Read it guarded, since storage access can throw (a private window, blocked site
  data), and fall back to the default when it does.
- **The side tray goes on the left**, between the action sidebar and the binder, as the flag's name
  says.
- **This step builds both layouts and deletes neither.** Which one goes, and when, is decided
  separately.

---

## 6. Refreshing a set's card prices from TCGdex

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
