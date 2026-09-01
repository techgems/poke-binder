<script lang="ts" module>
  /** The current state of every filter field, keyed by field. */
  export interface FilterSelection {
    generations: string[]
    sets: string[]
    pokemon: string[]
    rarities: string[]
    cardTypes: string[]
  }

  export function emptySelection(): FilterSelection {
    return { generations: [], sets: [], pokemon: [], rarities: [], cardTypes: [] }
  }
</script>

<script lang="ts">
  import { ToggleGroup } from '@skeletonlabs/skeleton-svelte'

  import { CardSearchClient, type StarterFilters } from '../clients/CardSearchClient'
  import type { FilterOption } from './filter-option'
  import FilterCombobox from './FilterCombobox.svelte'

  interface Props {
    /** Load the filter options once this turns true — lets the caller defer the request. */
    active?: boolean
    /** The current filter selection. Bindable. */
    selection?: FilterSelection
  }

  let { active = true, selection = $bindable(emptySelection()) }: Props = $props()

  let filters = $state<StarterFilters | null>(null)
  let loading = $state(false)
  let loadError = $state<string | null>(null)

  $effect(() => {
    if (active) void loadFilters()
  })

  async function loadFilters() {
    if (filters || loading) return

    loading = true
    loadError = null

    try {
      filters = await CardSearchClient.getStarterFilters()
    } catch (error) {
      loadError = error instanceof Error ? error.message : String(error)
    } finally {
      loading = false
    }
  }

  function resetFilters() {
    selection = emptySelection()
  }

  const generations: FilterOption[] = $derived(
    (filters?.generations ?? []).map((generation) => ({
      label: generation.name,
      value: String(generation.id),
    })),
  )

  const sets: FilterOption[] = $derived(
    (filters?.sets ?? []).map((set) => ({ label: set.name, value: String(set.id) })),
  )

  const pokemon: FilterOption[] = $derived(
    (filters?.pokemon ?? []).map((mon) => ({ label: mon.name, value: String(mon.id) })),
  )

  // Rarities arrive per set, so collapse them to the distinct rarity names.
  const rarities: FilterOption[] = $derived(
    [...new Set((filters?.rarityBySet ?? []).map((rarity) => rarity.rarity))]
      .sort((a, b) => a.localeCompare(b))
      .map((rarity) => ({ label: rarity, value: rarity })),
  )

  const cardTypes: FilterOption[] = $derived(
    (filters?.cardType ?? []).map((cardType) => ({
      label: cardType.name,
      value: String(cardType.id),
    })),
  )
</script>

<aside class="min-h-0 space-y-5 overflow-y-auto pr-2">
  <header class="flex items-center justify-between">
    <h3 class="font-semibold">Filters</h3>
    <button type="button" class="btn btn-sm hover:preset-tonal opacity-75" onclick={resetFilters}>
      Reset
    </button>
  </header>

  {#if loading}
    <p class="opacity-60">Loading filters…</p>
  {:else if loadError}
    <div class="space-y-2">
      <p class="text-error-500">{loadError}</p>
      <button type="button" class="btn btn-sm preset-tonal" onclick={loadFilters}>Retry</button>
    </div>
  {:else}
    <FilterCombobox
      label="Generations"
      data={generations}
      placeholder="All generations"
      bind:value={selection.generations}
    />
    <FilterCombobox
      label="Sets"
      data={sets}
      placeholder="All sets"
      bind:value={selection.sets}
    />
    <FilterCombobox
      label="Pokemon"
      data={pokemon}
      placeholder="All Pokemon"
      bind:value={selection.pokemon}
    />
    <FilterCombobox
      label="Rarity"
      data={rarities}
      placeholder="All rarities"
      bind:value={selection.rarities}
    />

    <div class="space-y-2">
      <span class="label-text">Card Type</span>
      <ToggleGroup
        multiple
        value={selection.cardTypes}
        onValueChange={(details) => (selection.cardTypes = details.value)}
        class="flex-wrap"
      >
        {#each cardTypes as cardType (cardType.value)}
          <ToggleGroup.Item value={cardType.value}>{cardType.label}</ToggleGroup.Item>
        {/each}
      </ToggleGroup>
    </div>
  {/if}
</aside>
