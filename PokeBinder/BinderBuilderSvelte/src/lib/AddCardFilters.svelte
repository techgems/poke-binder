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

  // Narrowed by the chosen generations, but always shown. Server order is pokedex order.
  const pokemon: FilterOption[] = $derived.by(() => {
    const selectedGenerationIds = new Set(selection.generations)

    return (filters?.pokemon ?? [])
      .filter(
        (mon) =>
          selectedGenerationIds.size === 0 || selectedGenerationIds.has(String(mon.generationId)),
      )
      .map((mon) => ({
        // Pad to three digits so the list lines up; anything four digits or longer is left alone.
        label: `${String(mon.pokedexNumber).padStart(3, '0')} - ${mon.name}`,
        value: String(mon.id),
      }))
  })

  // Drop any Pokemon the current generation selection no longer offers.
  $effect(() => {
    const available = new Set(pokemon.map((mon) => mon.value))
    const kept = selection.pokemon.filter((mon) => available.has(mon))

    if (kept.length !== selection.pokemon.length) {
      selection.pokemon = kept
    }
  })

  // Rarity is scoped to the chosen sets: no set, no rarity field. Each option is a rarity-by-set
  // row, so the value is that row's own id rather than the rarity name.
  const rarities: FilterOption[] = $derived.by(() => {
    if (selection.sets.length === 0) return []

    const selectedSetIds = new Set(selection.sets)
    const setNames = new Map((filters?.sets ?? []).map((set) => [set.id, set.name]))
    // The same rarity name repeats across sets, so name the set once more than one is selected.
    const qualify = selection.sets.length > 1

    return (filters?.rarityBySet ?? [])
      .filter((rarity) => selectedSetIds.has(String(rarity.setId)))
      .map((rarity) => ({
        label: qualify ? `${rarity.rarity} · ${setNames.get(rarity.setId) ?? ''}` : rarity.rarity,
        value: String(rarity.id),
      }))
      .sort((a, b) => a.label.localeCompare(b.label))
  })

  // Drop any rarity that the current set selection no longer offers.
  $effect(() => {
    const available = new Set(rarities.map((rarity) => rarity.value))
    const kept = selection.rarities.filter((rarity) => available.has(rarity))

    if (kept.length !== selection.rarities.length) {
      selection.rarities = kept
    }
  })

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
    {#if selection.sets.length > 0}
      <FilterCombobox
        label="Rarity"
        data={rarities}
        placeholder="All rarities"
        bind:value={selection.rarities}
      />
    {/if}

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
