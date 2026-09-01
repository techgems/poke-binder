<script lang="ts">
  import AddCardFilters, { emptySelection, type FilterSelection } from './AddCardFilters.svelte'
  import Modal from './Modal.svelte'

  interface Props {
    /** Whether the modal is open. Bindable. */
    open?: boolean
  }

  let { open = $bindable(false) }: Props = $props()

  let selection = $state<FilterSelection>(emptySelection())
</script>

<Modal bind:open title="Add Cards" width="max-w-6xl" class="flex flex-col h-[85dvh]">
  <div class="grid flex-1 min-h-0 grid-cols-[18rem_1fr_16rem] gap-4">
    <!-- Filters -->
    <AddCardFilters active={open} bind:selection />

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
