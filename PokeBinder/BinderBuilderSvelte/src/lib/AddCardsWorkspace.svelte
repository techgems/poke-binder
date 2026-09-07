<script lang="ts">
  import PlusIcon from '@lucide/svelte/icons/plus'
  import TriangleAlertIcon from '@lucide/svelte/icons/triangle-alert'

  import {
    CardSearchClient,
    type CardSearchRequest,
    type CardSearchResult,
    type SimpleCardSearchRequest,
  } from '../clients/CardSearchClient'
  import { preloadedSearchFilters } from '../clients/preload'
  import AddCardFilters, {
    effectiveSelection,
    emptySelection,
    type FilterSelection,
  } from './AddCardFilters.svelte'
  import SearchModeSelector, {
    DEFAULT_SEARCH_MODE,
    type SearchMode,
  } from './SearchModeSelector.svelte'
  import SimpleSearchFilters, {
    emptyTerms,
    type SimpleSearchTerms,
  } from './SimpleSearchFilters.svelte'
  import AddToTrayButton from './AddToTrayButton.svelte'
  import CardTray from './CardTray.svelte'
  import { tray } from './tray.svelte'
  import CardSpotlight from './tilt/CardSpotlight.svelte'
  import TiltCard from './tilt/TiltCard.svelte'

  /** Stand-in art for cards the catalog has no image for. */
  const CARD_BACK_URL = '/images/TcgImages/card-back.png'

  interface Props {
    /** Additional classes for the workspace grid. */
    class?: string
  }

  let { class: classes = '' }: Props = $props()

  // Which filtering system is driving the search. Only the active one is rendered, so its state is
  // the only state the results can be built from; the others keep theirs for when they come back.
  let mode = $state<SearchMode>(DEFAULT_SEARCH_MODE)

  // The catalog's filter options, handed over by the Razor page before this app started. They are
  // read once: the page embeds one snapshot, and nothing here can change it.
  const starterFilters = preloadedSearchFilters()

  // Advanced filters: what the user has picked, including choices the current super type has hidden.
  let selection = $state<FilterSelection>(emptySelection())

  // What an advanced search is actually run with: the picks above minus the ones the super type
  // rules out. This is the only selection that should ever reach the server.
  const appliedSelection = $derived(effectiveSelection(selection))

  // Simple search: the card name and identifier fields.
  let terms = $state<SimpleSearchTerms>(emptyTerms())

  // What a simple search is actually run with. Trimming here rather than at the request is what
  // keeps the search key below stable — a trailing space is not a different search.
  const appliedTerms = $derived({
    cardName: terms.cardName.trim(),
    cardNumber: terms.cardIdentifier.trim(),
  })

  /** Whether either field holds something to search for. */
  const hasTerms = $derived(appliedTerms.cardName !== '' || appliedTerms.cardNumber !== '')

  // One pool of results for both modes. Simple search and advanced filters differ only in which
  // endpoint fills this and what the paging cursor means to it; everything downstream — the grid,
  // Load more, the eventual selection — is shared and stays that way.
  let results = $state<CardSearchResult[]>([])
  let page = $state(1)
  let hasMore = $state(false)
  let searching = $state(false)
  let searchError = $state<string | null>(null)
  let hasSearched = $state(false)

  /** How the effective selection looks while the user has still touched nothing. */
  const untouched = JSON.stringify(effectiveSelection(emptySelection()))

  /** How long both simple search and advanced filters wait out typing before it goes to the server. */
  const SEARCH_DEBOUNCE_MS = 300

  // Plain variables on purpose: these steer when a search runs and are never rendered, so making
  // them reactive would only risk feeding the effect below back into itself.
  let armed = false
  let lastSearched: string | null = null
  let inFlight: AbortController | null = null

  $effect(() => {
    const advancedKey = JSON.stringify(appliedSelection)

    // The key names the mode as well as the state, so switching modes counts as a change even when
    // neither side has moved: the cards on screen came from the other endpoint and no longer
    // answer the question being asked.
    const key =
      mode === 'simple' ? `simple:${JSON.stringify(appliedTerms)}` : `advanced:${advancedKey}`

    if (mode === 'simple') {
      // Two empty boxes are not a search. The endpoint answers them with an empty page, so asking
      // would spend a request to arrive exactly where the user already is.
      if (!hasTerms) {
        clearResults()

        return
      }
    } else if (!armed && advancedKey === untouched) {
      // Advanced mode starts with nothing picked, and searching that would fetch page one of the
      // whole catalog for nothing. The first real filter change arms the search; from then on
      // every change re-runs it — including clearing the filters back to empty again. Anything a
      // simple search left on screen still goes: it does not answer these filters.
      clearResults()

      return
    }

    // Both modes wait out a pause before asking the server. Every keystroke in a card name box
    // changes the key, so without this a typed name would be one request per character; the same
    // wait also collapses a burst of picks — three sets chosen in a row — into the single search
    // the user meant. The cleanup is the other half of it: a pending wait is thrown away as soon
    // as the next change makes it stale.
    const timer = setTimeout(() => {
      // Deliberately inside the wait rather than guarding the effect: the cleanup below cancels
      // the pending search on every re-run, and a re-run can land on a key that has already been
      // searched — a trailing space makes a new terms object out of the same terms, and
      // effectiveSelection() returns a fresh object whenever the raw selection changes. Skipping
      // early there would cancel a search without ever scheduling its replacement.
      if (key === lastSearched) return

      // Simple mode must not arm the advanced search: arriving at untouched filters afterwards
      // would then fetch page one of the whole catalog.
      if (mode === 'advanced') armed = true

      lastSearched = key

      void search(1)
    }, SEARCH_DEBOUNCE_MS)

    return () => clearTimeout(timer)
  })

  /** Empties the results pool and drops the request that was filling it. */
  function clearResults() {
    inFlight?.abort()
    inFlight = null

    // Nothing on screen came from a search any more, so no key may be treated as already answered.
    // Without this, coming back to a mode whose terms are still typed in would dedupe against the
    // search whose results were just thrown away and leave the user staring at an empty column.
    lastSearched = null

    results = []
    page = 1
    hasMore = false
    searching = false
    searchError = null
    hasSearched = false
  }

  async function search(pageNumber: number) {
    // Whatever is still in the air is stale now. Without this a slow earlier response could land
    // after a fast later one and leave the wrong cards on screen.
    inFlight?.abort()

    const controller = new AbortController()
    inFlight = controller

    searching = true
    hasSearched = true
    searchError = null

    try {
      // Which endpoint answers is the only thing the two modes disagree about — both reply with
      // the same page shape, so everything past this line is shared.
      const found =
        mode === 'simple'
          ? await CardSearchClient.searchSimple(
              toSimpleRequest(appliedTerms, pageNumber),
              controller.signal,
            )
          : await CardSearchClient.searchByFilter(
              toRequest(appliedSelection, pageNumber),
              controller.signal,
            )

      results = pageNumber === 1 ? found.results : [...results, ...found.results]
      page = found.pageNumber
      hasMore = found.hasMore
    } catch (error) {
      // An aborted request was replaced deliberately, so its failure is not worth reporting.
      if (controller.signal.aborted) return

      searchError = error instanceof Error ? error.message : String(error)
    } finally {
      // Only the newest request owns the spinner; one that got aborted must not switch it off.
      if (inFlight === controller) {
        inFlight = null
        searching = false
      }
    }
  }

  function toRequest(selected: FilterSelection, pageNumber: number): CardSearchRequest {
    // The filter widgets are string-valued, so every id arrives as a string; the API takes numbers.
    return {
      // Left out when the box is empty, the same way the simple search leaves it out of its URL.
      cardName: selected.cardName || undefined,
      superTypes: [...selected.superTypes],
      generations: selected.generations.map(Number),
      series: selected.series.map(Number),
      sets: selected.sets.map(Number),
      pokemon: selected.pokemon.map(Number),
      // Rarities travel as names, not ids; the rest of the fields are ids.
      rarities: [...selected.rarities],
      cardTypes: selected.cardTypes.map(Number),
      pageNumber,
    }
  }

  function toSimpleRequest(
    typed: { cardName: string; cardNumber: string },
    pageNumber: number,
  ): SimpleCardSearchRequest {
    // The field is labelled Card Identifier in the UI, but what the API matches it against is the
    // card number printed on the card, so it travels under that name.
    return {
      cardName: typed.cardName,
      cardNumber: typed.cardNumber,
      pageNumber,
    }
  }

  function loadMore() {
    if (!searching && hasMore) void search(page + 1)
  }

  /** What the results column says with nothing searched yet, which differs per mode. */
  const promptMessage = $derived(
    mode === 'simple' ? 'Type a card name or number to search' : 'Pick a filter to search',
  )

  /** What it says when the search came back empty. */
  const noMatchMessage = $derived(
    mode === 'simple' ? 'No cards match these terms' : 'No cards match these filters',
  )

  // The card currently lifted to the front, or null for none. Holding the card rather than a bare
  // open flag is what lets the overlay show its details without a second copy of them.
  let spotlit = $state<CardSearchResult | null>(null)
</script>

<!-- The two side columns are fixed and only the middle one flexes, so whatever extra room the host
     hands this grid all goes to the results. The tray column is the wider of the two: each of its
     rows carries art, a card name, a set name and a stepper, and at the filter column's width the
     names were cut to three characters. -->
<!-- relative so the spotlight below covers exactly this workspace: the tab strip above it stays
     reachable, and nothing has to stack a second dialog to get a card to the front. -->
<div class="relative grid min-h-0 flex-1 grid-cols-[18rem_1fr_20rem] gap-4 {classes}">
  <!-- Filters. The mode selector sits in this column because all it does is choose which filter
       set is shown; the rows are auto/minmax(0,1fr) so the filters below can shrink and scroll. -->
  <div class="grid min-h-0 grid-rows-[auto_minmax(0,1fr)] gap-4">
    <SearchModeSelector bind:mode />
    {#if mode === 'simple'}
      <SimpleSearchFilters bind:terms />
    {:else if starterFilters}
      <AddCardFilters filters={starterFilters} loading={false} bind:selection />
    {/if}
  </div>

  {#if mode === 'advanced' && !starterFilters}
    <!-- Promoted across the results and selected columns rather than tucked into the filter column:
         with no options to pick there is nothing to filter by and nothing to search, so those two
         columns would only be separate ways of showing an empty box. The mode selector stays
         outside it deliberately — this says nothing about simple search, which has its own
         endpoint, and hiding the selector behind it would strand the user in the one mode that is
         actually broken.

         There is nothing to retry: the options arrive with the page, so a missing set means the
         page itself was served without them and only a fresh one can fix it. -->
    <div
      class="col-span-2 grid min-h-0 place-items-center rounded-container border border-error-500/40 bg-error-500/5 p-6"
    >
      <div class="max-w-md space-y-3 text-center">
        <TriangleAlertIcon class="mx-auto size-8 text-error-500" />
        <h3 class="h4">Card filters were not sent with this page</h3>
        <p class="text-sm opacity-75">
          Advanced filters are built from options the server embeds when the page loads, so there is
          nothing here to filter by. Reloading should bring them back.
        </p>
        <button type="button" class="btn preset-filled-primary-500" onclick={() => location.reload()}>
          Reload
        </button>
      </div>
    </div>
  {:else}
    <!-- Results -->
    <section class="flex min-h-0 flex-col rounded-container border border-surface-200-800/50">
      {#if !hasSearched}
        <p class="m-auto opacity-60">{promptMessage}</p>
      {:else if searchError}
        <div class="m-auto space-y-2 text-center">
          <p class="text-error-500">{searchError}</p>
          <button type="button" class="btn btn-sm preset-tonal" onclick={() => search(1)}>
            Retry
          </button>
        </div>
      {:else if results.length === 0}
        <p class="m-auto opacity-60">
          {searching ? 'Searching…' : noMatchMessage}
        </p>
      {:else}
        <!-- Fixed at five columns: the cards scale with the panel instead of the count changing. -->
        <ul class="grid min-h-0 flex-1 grid-cols-5 content-start gap-3 overflow-y-auto p-3">
          {#each results as card (card.id)}
            {@const detail = [card.setName, card.rarity, card.cardNumber]
              .filter(Boolean)
              .join(' · ')}
            <li>
              <figure class="space-y-1" title={[card.name, detail].filter(Boolean).join('\n')}>
                <!-- TiltCard stands in for a plain <img> and reserves the same 719:1000 box, so
                     taking the effect back out is a one-line swap. Cards with no art fall back to
                     the card back, which keeps every tile the same shape instead of leaving a hole
                     in the grid.

                     The effect is deliberately faint here. Tilt is a transform, so a tile paints
                     outside its own box: rotation swings the near corners down over the name
                     underneath, and growing on hover did the same from every edge at once. A
                     quarter of the library's default rotation still reads as a lift at this size,
                     and scaleFactor 1 keeps the card exactly as large as it was — it just stops
                     growing into its own caption. -->
                <TiltCard
                  src={card.imageUrl ?? CARD_BACK_URL}
                  alt={card.imageUrl ? (card.name ?? 'Card') : 'No image available'}
                  label={card.name ?? 'Card'}
                  class="w-full rounded-container"
                  tiltFactor={0.25}
                  scaleFactor={1}
                  onclick={() => (spotlit = card)}
                />
                <!-- The control keeps to the right and drops to the foot of the caption, level
                     with the set line rather than the title. min-w-0 on the text column is what
                     lets a long name ellipse against it instead of pushing it out of the tile. -->
                <figcaption class="flex items-end justify-between gap-1">
                  <div class="min-w-0 space-y-0.5">
                    <p class="truncate text-xs">{card.name}</p>
                    <p class="truncate text-[0.625rem] opacity-60">
                      {[card.setName, card.cardNumber].filter(Boolean).join(' · ')}
                    </p>
                  </div>
                  <AddToTrayButton {card} />
                </figcaption>
              </figure>
            </li>
          {/each}
        </ul>
        <footer
          class="flex items-center justify-between gap-2 border-t border-surface-200-800/50 px-3 py-2"
        >
          <span class="text-xs opacity-60">
            {results.length} shown{hasMore ? ' — more available' : ''}
          </span>
          {#if hasMore}
            <button
              type="button"
              class="btn btn-sm preset-tonal"
              onclick={loadMore}
              disabled={searching}
            >
              {searching ? 'Loading…' : 'Load more'}
            </button>
          {/if}
        </footer>
      {/if}
    </section>

    <!-- Selected -->
    <CardTray />
  {/if}

  <!-- A card with no art still gets lifted — its details are worth reading either way — but it is
       held flat here, so the one card that will not move is the one whose art the catalog is
       missing. Only here: in the results grid every tile tilts, art or not, because a tile that
       sat still among moving neighbours would read as broken rather than as missing art. -->
  <CardSpotlight
    open={spotlit !== null}
    src={spotlit?.imageUrl ?? CARD_BACK_URL}
    alt={spotlit?.imageUrl ? (spotlit.name ?? 'Card') : 'No image available'}
    label={spotlit?.name ? `${spotlit.name} preview` : 'Card preview'}
    tilt={spotlit?.imageUrl != null}
    onclose={() => (spotlit = null)}
  >
    {#snippet details()}
      <p class="font-semibold">{spotlit?.name}</p>
      <p class="text-sm opacity-60">
        {[spotlit?.setName, spotlit?.rarity, spotlit?.cardNumber].filter(Boolean).join(' · ')}
      </p>
      {#if spotlit && spotlit.imageUrl === null}
        <p class="text-sm text-warning-500">This card has no image in the catalog.</p>
      {/if}
    {/snippet}

    {#snippet actions()}
      <!-- The reason to lift a card is to decide whether you want it, so the button that acts on
           that answer is the widest thing on the row. Placing a card on a page of the binder is
           still unbuilt; the tray is where a wanted card goes for now. -->
      <button
        type="button"
        class="btn btn-lg preset-filled-primary-500 min-w-64 shadow-lg shadow-primary-500/30"
        onclick={() => spotlit && tray.add(spotlit)}
      >
        <PlusIcon class="size-5" />
        <span>
          {spotlit && tray.quantityOf(spotlit.id) > 0
            ? `Add another — ${tray.quantityOf(spotlit.id)} in the tray`
            : 'Add to tray'}
        </span>
      </button>
      <button type="button" class="btn btn-lg preset-tonal" onclick={() => (spotlit = null)}>
        Close
      </button>
    {/snippet}
  </CardSpotlight>
</div>
