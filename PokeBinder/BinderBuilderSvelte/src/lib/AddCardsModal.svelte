<script lang="ts">
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
</script>

<Modal bind:open title="Add Cards" width="max-w-6xl" class="flex flex-col h-[85dvh]">
  <SearchModeSelector bind:mode />

  <div class="grid flex-1 min-h-0 grid-cols-[18rem_1fr_16rem] gap-4">
    <!-- Filters -->
    {#if mode === 'simple'}
      <SimpleSearchFilters bind:terms />
    {:else if mode === 'advanced'}
      <AddCardFilters active={open} bind:selection />
    {/if}

    <!-- Results -->
    <section
      class="grid min-h-0 place-items-center rounded-container border border-surface-200-800/50"
    >
      <p class="opacity-60">Results will appear here</p>
    </section>

    <!-- Selected -->
    <aside
      class="grid min-h-0 place-items-center rounded-container border border-surface-200-800/50"
    >
      <p class="opacity-60">Selected cards will appear here</p>
    </aside>
  </div>
</Modal>
