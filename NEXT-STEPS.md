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

The groups age at very different rates, and the cache respects that rather than expiring everything
together. `GetSearchStarterFilters.Response` has seven; five of them get cached:

| Group | Changes | Cached |
| --- | --- | --- |
| Pokemon | On a new game — years apart | yes |
| Generations | With a new generation — years apart | yes |
| Series | Every 2-3 years | yes |
| Sets, rarity-by-set | Every 2–3 months, with each release | yes |
| Card types, super types | Effectively never, and likely a code change when they do | **no** |

Card types and super types stay embedded in the page exactly as they are today. They are a handful
of rows against 141 KB, and caching them would mean caching them *forever*: the one thing worse than
re-sending 2 KB is a group with no invalidation path, stuck on every client that already has it the
day one gets added by data alone. Leaving them inline drops them out of the machinery below
entirely — no stamp, no storage slot, no fetch branch.

### How the cache invalidates

A **stamp** per cached group: an opaque value the client compares against its own copy, and
re-fetches that group when the two differ. Five stamps, in a new table in `TcgCatalog` alongside the
data — group name, stamp, last-bumped timestamp.

The stamp is a **GUID, and the comparison is equality only**. The stamps require an admin server page using Razor Pages and HTMX with a 
button that trigger a regeneration of the stamp value.

**Nothing derives the stamps automatically.** The ETL does not write them, and loading new sets is
moving off the ETL anyway — `Pages/Admin/LoadSet.cshtml` is the page that will do it and is
currently an empty stub. So the stamps are bumped by hand from an admin screen, one **Bump** button
per group plus a bump-all, on `_AdminLayout` (plain Tailwind and Pines UI there — *not* the binder
vocabulary; see `CLAUDE.md`). A button, not a GUID field: a hand-typed stamp is either a silent
no-op or a mystery a month later, while a bump costs at worst one re-fetch of one group.

When `LoadSet` is built, it should bump `Sets` and `RarityBySet` itself, in the same transaction as
the rows it writes. Split across two transactions, a crash in between leaves new data in the
database with every client still holding the old stamp — permanently stale, with nothing surfacing
it. The admin screen stays regardless, both as the escape hatch for data corrected outside that page
and as the only place to see what each stamp is and when it last moved.

### The shape on the wire

The page keeps `<server-preload>` but carries **only the stamp map** — five names and five GUIDs, on
the order of 200 bytes in place of 141 KB. That is what makes a warm load cost *nothing*: the client
compares the embedded map against `localStorage`, finds everything matching, and never opens a
request if all the stamps match.

The filters themselves move to an endpoint, which is where they were before this was inlined. The
client sends a set of boolean values based on the stamps that didn't match and gets back only the groups that do not match, plus their new
stamps. One round trip, and only when something is actually stale.

**No cookie is involved, and that is worth knowing deliberately.** An earlier sketch had the server
infer what the client held from an HTTP-only cookie. That was only ever needed because the decision
was being made during the Razor render, before any client JS had run, so the server had nothing else
to go on. Once the client is the one asking, it simply says what it has — which removes the cookie,
the desync between cookie and `localStorage`, `Vary: Cookie` on a shared cache, and the cookie
riding along with every same-origin `/cardImages` request.

### Details that bite

- **The server's own cache has to answer to the stamps.** `GetSearchStarterFilters` holds its
  response in `IMemoryCache` for 24 hours at `Priority = NeverRemove`. Left alone, a bump would have
  every client correctly decide it is stale and re-fetch — and be handed the same cached payload for
  up to a day. Do not fix this by evicting on bump: `IMemoryCache` is per-process, so that breaks
  again the moment there is more than one instance. **Put the stamp in the cache key**
  (`CardSearch:Filters:pokemon:{stamp}`) and a bump misses everywhere at once, with the old entries
  ageing out on their own.
- **`localStorage`, not IndexedDB.** 141 KB against a ~5 MB cap, and the read is synchronous, so the
  filters are in hand before first paint instead of behind an async gate the search panel would wait
  on. Wrap every access in try/catch: blocked or full storage has to degrade to fetching every time,
  not throw.
- **Write the data, then the stamp.** Reversed, a write that fails on quota leaves the client
  believing it is current with nothing stored.
- **This is catalog data, not user data.** It is shared by every account on the device and survives
  sign-out, which is correct and worth not "fixing" later by scoping it per user.

While doing this, make sure to that the bumping of the cache in the admin side gets its own separate vertical slice. One for retrieving the previous stamps and when they were last bumped, and another for bumping them.
This whole "Step" should be split in three. First handling the admin side of things. Second making changes in the cache structure of `IMemoryCache` and the retrieval of the filters to go back to using an API call, as well as using a server preload for the cache map.
And as a third step, the changes in the Svelte app to know how to request the filters, if at all necessary. Stop after every step for review and human re-testing, and only continue when prompted to do so.

---

## 2. Third tab: binder settings

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
