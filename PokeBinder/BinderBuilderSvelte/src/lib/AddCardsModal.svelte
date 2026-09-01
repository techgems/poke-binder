<script lang="ts">
  import { ToggleGroup } from '@skeletonlabs/skeleton-svelte'

  import FilterCombobox from './FilterCombobox.svelte'
  import Modal from './Modal.svelte'
  import { cardTypes, generations, pokemon, rarities, sets } from './mock-filters'

  interface Props {
    /** Whether the modal is open. Bindable. */
    open?: boolean
  }

  let { open = $bindable(false) }: Props = $props()

  let selectedCardTypes = $state<string[]>([])
</script>

<Modal bind:open title="Add Cards" width="max-w-6xl" class="flex flex-col h-[85dvh]">
  <div class="grid flex-1 min-h-0 grid-cols-[18rem_1fr_16rem] gap-4">
    <!-- Filters -->
    <aside class="min-h-0 space-y-5 overflow-y-auto pr-2">
      <header class="flex items-center justify-between">
        <h3 class="font-semibold">Filters</h3>
        <button type="button" class="btn btn-sm hover:preset-tonal opacity-75">Reset</button>
      </header>

      <FilterCombobox label="Generations" data={generations} placeholder="All generations" />
      <FilterCombobox label="Sets" data={sets} placeholder="All sets" />
      <FilterCombobox label="Pokemon" data={pokemon} placeholder="All Pokemon" />
      <FilterCombobox label="Rarity" data={rarities} placeholder="All rarities" />

      <div class="space-y-2">
        <span class="label-text">Card Type</span>
        <ToggleGroup
          multiple
          value={selectedCardTypes}
          onValueChange={(details) => (selectedCardTypes = details.value)}
          class="flex-wrap"
        >
          {#each cardTypes as cardType (cardType.value)}
            <ToggleGroup.Item value={cardType.value}>{cardType.label}</ToggleGroup.Item>
          {/each}
        </ToggleGroup>
      </div>
    </aside>

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
