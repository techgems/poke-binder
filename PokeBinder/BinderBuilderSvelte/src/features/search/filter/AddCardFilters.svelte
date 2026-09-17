<script lang="ts">
  import { ToggleGroup } from '@skeletonlabs/skeleton-svelte'

  import type { StarterFilters } from '../../../clients/CardSearchClient'
  import CardTypeFilterGroup from './CardTypeFilterGroup.svelte'
  import type { CardTypeOption, FilterOption } from '../types/filter-option'
  import {
    cardTypeFieldApplies,
    emptySelection,
    pokemonFieldsApply,
    type FilterSelection,
  } from '../utils/filter-selection'
  import FilterCombobox from './FilterCombobox.svelte'

  interface Props {
    /**
     * The catalog's filter options, or null while they are still being fetched. Loading them is the
     * search tab's job, not this component's: nothing in the Add Cards tab works without them, so a
     * failure has to be reported at that level rather than inside one of its three columns.
     */
    filters?: StarterFilters | null
    /** Whether that fetch is still in flight. */
    loading?: boolean
    /** The current filter selection. Bindable. */
    selection?: FilterSelection
  }

  let {
    filters = null,
    loading = false,
    selection = $bindable(emptySelection()),
  }: Props = $props()

  function resetFilters() {
    selection = emptySelection()
  }

  const superTypes: FilterOption[] = $derived(
    (filters?.superTypes ?? []).map((superType) => ({
      label: superType.name,
      value: superType.name,
    })),
  )

  // Hides the Pokemon-only fields once a non-Pokemon super type is in play. Their selections are
  // deliberately left untouched so they return intact; effectiveSelection() is what keeps them out
  // of the search while they are hidden.
  const showPokemonFields = $derived(pokemonFieldsApply(selection.superTypes))

  const showCardTypeField = $derived(cardTypeFieldApplies(selection.superTypes))

  const generations: FilterOption[] = $derived(
    (filters?.generations ?? []).map((generation) => ({
      label: generation.name,
      value: String(generation.id),
    })),
  )

  const series: FilterOption[] = $derived(
    (filters?.series ?? []).map((entry) => ({ label: entry.name, value: String(entry.id) })),
  )

  // Narrowed by the chosen series, but always shown: no series selected means every set.
  const sets: FilterOption[] = $derived.by(() => {
    const selectedSeriesIds = new Set(selection.series)

    return (filters?.sets ?? [])
      .filter((set) => selectedSeriesIds.size === 0 || selectedSeriesIds.has(String(set.seriesId)))
      .map((set) => ({ label: set.name, value: String(set.id) }))
  })

  // Drop any set the current series selection no longer offers.
  $effect(() => {
    const available = new Set(sets.map((set) => set.value))
    const kept = selection.sets.filter((set) => available.has(set))

    if (kept.length !== selection.sets.length) {
      selection.sets = kept
    }
  })

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

  // The sets whose rarities are worth offering. Chosen sets win over chosen series, because naming
  // sets is the narrower statement; a series stands in for all of the sets inside it. Scoping at
  // all is what stops the user pairing a rarity with a set that never printed one, which could
  // only ever return nothing.
  const rarityScopeSetIds: Set<number> = $derived.by(() => {
    if (selection.sets.length > 0) {
      return new Set(selection.sets.map(Number))
    }

    if (selection.series.length > 0) {
      const selectedSeriesIds = new Set(selection.series)

      return new Set(
        (filters?.sets ?? [])
          .filter((set) => selectedSeriesIds.has(String(set.seriesId)))
          .map((set) => set.id),
      )
    }

    return new Set<number>()
  })

  // A rarity name means the same thing wherever it appears — a Special Illustration Rare in Mega
  // Evolution is the one in Phantasmal Flames — so the options are the distinct names in scope and
  // the name is what gets filtered on. The rarity-by-set rows only decide what is on offer; which
  // set a name came from stops mattering once it is listed.
  const rarities: FilterOption[] = $derived.by(() => {
    if (rarityScopeSetIds.size === 0) return []

    const names = new Set(
      (filters?.rarityBySet ?? [])
        .filter((rarity) => rarityScopeSetIds.has(rarity.setId))
        .map((rarity) => rarity.rarity),
    )

    return [...names]
      .sort((left, right) => left.localeCompare(right))
      .map((name) => ({ label: name, value: name }))
  })

  // Drop any rarity the current series and set selection no longer offers.
  $effect(() => {
    const available = new Set(rarities.map((rarity) => rarity.value))
    const kept = selection.rarities.filter((rarity) => available.has(rarity))

    if (kept.length !== selection.rarities.length) {
      selection.rarities = kept
    }
  })

  const cardTypes: CardTypeOption[] = $derived(
    (filters?.cardType ?? []).map((cardType) => ({
      label: cardType.name,
      value: String(cardType.id),
      imageUrl: cardType.imageUrl,
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

  <!-- Above the option-driven fields, and outside the loading branch below: matching a name asks
       nothing of the catalog's filter options, so this one stays usable while they are still on
       their way. -->
  <label class="label">
    <span class="label-text">Card Name</span>
    <input
      class="input"
      type="text"
      placeholder="Type a card name"
      bind:value={selection.cardName}
    />
  </label>

  <!-- No error branch: a failed load takes the whole search tab, so this only ever sees the fetch
       succeed or still be running. -->
  {#if loading}
    <p class="opacity-60">Loading filters…</p>
  {:else}
    <div class="space-y-2">
      <span class="label-text">Super Type</span>
      <ToggleGroup
        multiple
        value={selection.superTypes}
        onValueChange={(details) => (selection.superTypes = details.value)}
        class="flex-wrap"
      >
        {#each superTypes as superType (superType.value)}
          <ToggleGroup.Item value={superType.value}>{superType.label}</ToggleGroup.Item>
        {/each}
      </ToggleGroup>
    </div>

    <FilterCombobox
      label="Series"
      data={series}
      placeholder="Type a series name"
      bind:value={selection.series}
    />
    <FilterCombobox
      label="Sets"
      data={sets}
      placeholder="Type a set name"
      bind:value={selection.sets}
    />
    {#if showPokemonFields}
      <FilterCombobox
        label="Pokemon Generations"
        data={generations}
        placeholder="Select a Pokemon Generation"
        bind:value={selection.generations}
      />
      <FilterCombobox
        label="Pokemon"
        data={pokemon}
        placeholder="Type a Pokemon"
        bind:value={selection.pokemon}
      />
    {/if}
    <!-- Shown once a series or a set has put some rarities in scope. -->
    {#if rarities.length > 0}
      <FilterCombobox
        label="Rarity"
        data={rarities}
        placeholder="Type a rarity"
        bind:value={selection.rarities}
      />
    {/if}

    {#if showCardTypeField}
      <CardTypeFilterGroup options={cardTypes} bind:value={selection.cardTypes} />
    {/if}
  {/if}
</aside>
