<!--
  Sorts the whole binder by up to three criteria, edited in SortCriteriaEditor. Applying asks
  the server to sort and store the binder, then lays out the order it answered with; one Undo puts
  the old arrangement back.
-->
<script lang="ts">
  import Modal from '../../../components/Modal.svelte'
  import { toaster } from '../../../components/toaster'
  import { binderPage } from '../../../stores/binder-page.svelte'
  import { reorderBinder } from './reorder-binder'
  import type { SortCriterion } from './reorder-criteria'
  import SortCriteriaEditor from './SortCriteriaEditor.svelte'
  import { sortSession } from './sort-session'

  interface Props {
    /** Whether the modal is open. Bindable. */
    open?: boolean
    /** A reorder is out. Bindable, so the page can keep undo away from the binder meanwhile. */
    busy?: boolean
  }

  let { open = $bindable(false), busy = $bindable(false) }: Props = $props()

  let criteria = $state<SortCriterion[]>(sortSession.criteria)

  // Every opening starts from what was last applied, so criteria edited and then cancelled do not
  // come back as though they had been.
  $effect(() => {
    if (open) criteria = sortSession.criteria
  })

  const placedCount = $derived(binderPage.slots.reduce((count, card) => count + (card ? 1 : 0), 0))

  const pagesFilled = $derived(Math.ceil(placedCount / binderPage.cardsPerPage))

  /**
   * Stays open until the server answers, so a failure has somewhere to be reported and the
   * criteria are still there to try again with. Nothing on screen changes until it succeeds.
   */
  async function apply(): Promise<void> {
    if (busy) return

    busy = true

    try {
      await reorderBinder(criteria)

      sortSession.remember(criteria)
      open = false

      toaster.success({
        title: 'Binder reordered',
        description: 'Undo puts the old arrangement back.',
      })
    } catch {
      toaster.error({
        title: 'Not reordered',
        description: 'The binder is as it was. Try again in a moment.',
      })
    } finally {
      busy = false
    }
  }
</script>

<!-- Wide rather than tall: each criterion is one row, its switches beside its name. -->
<Modal bind:open title="Reorder binder" width="max-w-4xl">
  <p class="text-sm opacity-75">
    Sorts every card in the binder and lays them out again from the first pocket, replacing the
    arrangement it has now. The first criterion decides; the ones below it only break its ties.
    Undo puts the old arrangement back.
  </p>

  <SortCriteriaEditor bind:criteria />

  <footer class="flex items-center justify-between gap-4">
    <p class="text-sm opacity-60">
      {#if placedCount === 0}
        Nothing is placed in this binder yet.
      {:else}
        {placedCount} {placedCount === 1 ? 'card' : 'cards'}, filling {pagesFilled}
        {pagesFilled === 1 ? 'page' : 'pages'} from the first.
      {/if}
    </p>

    <div class="flex gap-2">
      <button type="button" class="btn preset-tonal" disabled={busy} onclick={() => (open = false)}>
        Cancel
      </button>
      <button
        type="button"
        class="btn preset-filled-primary-500"
        disabled={placedCount === 0 || busy}
        onclick={apply}
      >
        {busy ? 'Reordering…' : 'Reorder'}
      </button>
    </div>
  </footer>
</Modal>
