<script lang="ts">
  import {
    CardSearchClient,
    type CardSearchRequest,
    type CardSearchResult,
  } from '../clients/CardSearchClient'
  import AddCardFilters, {
    effectiveSelection,
    emptySelection,
    type FilterSelection,
  } from './AddCardFilters.svelte'
  import Modal from './Modal.svelte'
  import SearchModeSelector, {
    DEFAULT_SEARCH_MODE,
    type SearchMode,
  } from './SearchModeSelector.svelte'
  import SimpleSearchFilters, {
    emptyTerms,
    type SimpleSearchTerms,
  } from './SimpleSearchFilters.svelte'

  /** Stand-in art for cards the catalog has no image for. */
  const CARD_BACK_URL = '/images/TcgImages/card-back.png'

  interface Props {
    /** Whether the modal is open. Bindable. */
    open?: boolean
  }

  let { open = $bindable(false) }: Props = $props()

  // Which filtering system is driving the search. Only the active one is rendered, so its state is
  // the only state the results can be built from; the others keep theirs for when they come back.
  let mode = $state<SearchMode>(DEFAULT_SEARCH_MODE)

  // Advanced filters: what the user has picked, including choices the current super type has hidden.
  let selection = $state<FilterSelection>(emptySelection())

  // What an advanced search is actually run with: the picks above minus the ones the super type
  // rules out. This is the only selection that should ever reach the server.
  const appliedSelection = $derived(effectiveSelection(selection))

  // Simple search: the card name and identifier fields.
  let terms = $state<SimpleSearchTerms>(emptyTerms())

  let results = $state<CardSearchResult[]>([])
  let page = $state(1)
  let hasMore = $state(false)
  let searching = $state(false)
  let searchError = $state<string | null>(null)
  let hasSearched = $state(false)

  /** How the effective selection looks while the user has still touched nothing. */
  const untouched = JSON.stringify(effectiveSelection(emptySelection()))

  // Plain variables on purpose: these steer when a search runs and are never rendered, so making
  // them reactive would only risk feeding the effect below back into itself.
  let armed = false
  let lastSearched: string | null = null
  let inFlight: AbortController | null = null

  $effect(() => {
    const key = JSON.stringify(appliedSelection)

    // The modal opens with nothing picked, and searching that would fetch page one of the whole
    // catalog for nothing. The first real filter change arms the search; from then on every change
    // re-runs it — including clearing the filters back to empty again.
    if (!armed) {
      if (key === untouched) return

      armed = true
    }

    // effectiveSelection() returns a fresh object whenever the raw selection changes, so the same
    // effective filters can arrive twice: picking a generation while a non-Pokemon super type is
    // zeroing it out, for one. Only a real change should cost a request.
    if (key === lastSearched) return

    lastSearched = key

    void search(1)
  })

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
      const found = await CardSearchClient.searchByFilter(
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
      superTypes: [...selected.superTypes],
      generations: selected.generations.map(Number),
      series: selected.series.map(Number),
      sets: selected.sets.map(Number),
      pokemon: selected.pokemon.map(Number),
      rarities: selected.rarities.map(Number),
      cardTypes: selected.cardTypes.map(Number),
      pageNumber,
    }
  }

  function loadMore() {
    if (!searching && hasMore) void search(page + 1)
  }
</script>

<!-- Wider and taller than the other modals: the extra room all goes to the results grid, since the
     two side columns are fixed and only the middle one flexes. -->
<Modal bind:open title="Add Cards" width="max-w-[100rem]" class="flex flex-col h-[92dvh]">
  <div class="grid flex-1 min-h-0 grid-cols-[18rem_1fr_16rem] gap-4">
    <!-- Filters. The mode selector sits in this column because all it does is choose which filter
         set is shown; the rows are auto/minmax(0,1fr) so the filters below can shrink and scroll. -->
    <div class="grid min-h-0 grid-rows-[auto_minmax(0,1fr)] gap-4">
      <SearchModeSelector bind:mode />
      {#if mode === 'simple'}
        <SimpleSearchFilters bind:terms />
      {:else if mode === 'advanced'}
        <AddCardFilters active={open} bind:selection />
      {/if}
    </div>

    <!-- Results -->
    <section class="flex min-h-0 flex-col rounded-container border border-surface-200-800/50">
      {#if !hasSearched}
        <p class="m-auto opacity-60">Pick a filter to search</p>
      {:else if searchError}
        <div class="m-auto space-y-2 text-center">
          <p class="text-error-500">{searchError}</p>
          <button type="button" class="btn btn-sm preset-tonal" onclick={() => search(1)}>
            Retry
          </button>
        </div>
      {:else if results.length === 0}
        <p class="m-auto opacity-60">
          {searching ? 'Searching…' : 'No cards match these filters'}
        </p>
      {:else}
        <!-- Fixed at five columns: the cards scale with the modal instead of the count changing. -->
        <ul
          class="grid min-h-0 flex-1 grid-cols-5 content-start gap-3 overflow-y-auto p-3"
        >
          {#each results as card (card.id)}
            {@const detail = [card.setName, card.rarity, card.cardNumber]
              .filter(Boolean)
              .join(' · ')}
            <li>
              <figure class="space-y-1" title={[card.name, detail].filter(Boolean).join('\n')}>
                <!-- Cards are portrait at a consistent 719:1000, so the box is reserved up front
                     and lazy loading keeps a long results page from fetching everything at once.
                     Cards with no art fall back to the card back, which keeps every tile the same
                     shape instead of leaving a hole in the grid. -->
                <img
                  src={card.imageUrl ?? CARD_BACK_URL}
                  alt={card.imageUrl ? (card.name ?? 'Card') : 'No image available'}
                  loading="lazy"
                  class="rounded-container aspect-[719/1000] w-full bg-surface-200-800/40 object-cover"
                />
                <figcaption class="space-y-0.5">
                  <p class="truncate text-xs">{card.name}</p>
                  <p class="truncate text-[0.625rem] opacity-60">
                    {[card.setName, card.cardNumber].filter(Boolean).join(' · ')}
                  </p>
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
    <aside
      class="grid min-h-0 place-items-center rounded-container border border-surface-200-800/50"
    >
      <p class="opacity-60">Selected cards will appear here</p>
    </aside>
  </div>
</Modal>
