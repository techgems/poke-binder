<!--
  Sorts the whole binder. Up to three criteria, ranked by dragging -- or by the arrow buttons, for
  anyone not using a mouse -- each with its own direction, and any of them dropped. Applying asks
  the server to sort and store the binder, then lays out the order it answered with; one Undo puts
  the old arrangement back.
-->
<script lang="ts">
  import ArrowDownIcon from '@lucide/svelte/icons/arrow-down'
  import ArrowUpIcon from '@lucide/svelte/icons/arrow-up'
  import GripVerticalIcon from '@lucide/svelte/icons/grip-vertical'
  import PlusIcon from '@lucide/svelte/icons/plus'
  import XIcon from '@lucide/svelte/icons/x'
  import { SegmentedControl } from '@skeletonlabs/skeleton-svelte'
  import { flip } from 'svelte/animate'

  import Modal from '../../../components/Modal.svelte'
  import { prefersReducedMotion } from '../../../components/tilt/prefers-reduced-motion.svelte'
  import { toaster } from '../../../components/toaster'
  import { binderPage } from '../../../stores/binder-page.svelte'
  import { reorderBinder } from './reorder-binder'
  import {
    CRITERION_NAMES,
    DIRECTION_LABELS,
    newCriterion,
    RARITY_KEY_LABELS,
    type CriterionKey,
    type RarityKey,
    type SortCriterion,
    type SortDirection,
  } from './reorder-criteria'
  import { sortSession } from './sort-session'

  interface Props {
    /** Whether the modal is open. Bindable. */
    open?: boolean
    /** A reorder is out. Bindable, so the page can keep undo away from the binder meanwhile. */
    busy?: boolean
  }

  let { open = $bindable(false), busy = $bindable(false) }: Props = $props()

  const ALL_KEYS: CriterionKey[] = ['set', 'rarity', 'name']

  let criteria = $state<SortCriterion[]>(sortSession.criteria)

  // Every opening starts from what was last applied, so criteria edited and then cancelled do not
  // come back as though they had been.
  $effect(() => {
    if (open) criteria = sortSession.criteria
  })

  const dropped = $derived(ALL_KEYS.filter((key) => !criteria.some((c) => c.key === key)))

  const placedCount = $derived(binderPage.slots.reduce((count, card) => count + (card ? 1 : 0), 0))

  const pagesFilled = $derived(Math.ceil(placedCount / binderPage.cardsPerPage))

  // The row being dragged, and the row it would land on -- the one that lights up.
  let dragging = $state<number | null>(null)
  let over = $state<number | null>(null)

  function move(from: number, to: number): void {
    if (from === to || to < 0 || to >= criteria.length) return

    const [criterion] = criteria.splice(from, 1)

    criteria.splice(to, 0, criterion)
  }

  function drop(index: number): void {
    criteria.splice(index, 1)
  }

  // Back at the bottom, as the least significant: putting a criterion back is asking it to break
  // ties, and outranking the ones already chosen is a drag away.
  function restore(key: CriterionKey): void {
    criteria.push(newCriterion(key))
  }

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

  function directionsOf(key: CriterionKey): SortDirection[] {
    return Object.keys(DIRECTION_LABELS[key]) as SortDirection[]
  }

  const segmentControl = 'flex gap-1 rounded-base preset-tonal p-1'
  const segmentItem =
    'btn btn-sm cursor-pointer rounded-base hover:preset-tonal data-[state=checked]:preset-filled-primary-500 data-[focus-visible]:ring-2 data-[focus-visible]:ring-primary-500'
</script>

<!-- Wide rather than tall: each criterion is one row, its switches beside its name. -->
<Modal bind:open title="Reorder binder" width="max-w-4xl">
  <p class="text-sm opacity-75">
    Sorts every card in the binder and lays them out again from the first pocket, replacing the
    arrangement it has now. The first criterion decides; the ones below it only break its ties.
    Undo puts the old arrangement back.
  </p>

  <ol class="space-y-2" aria-label="Sort criteria, most significant first">
    {#each criteria as criterion, index (criterion.key)}
      <li
        animate:flip={{ duration: prefersReducedMotion() ? 0 : 150 }}
        draggable="true"
        class="card border-2 px-3 py-2 transition-colors {over === index && dragging !== index
          ? 'border-primary-500 bg-primary-500/15'
          : 'border-transparent preset-tonal'} {dragging === index ? 'opacity-50' : ''}"
        ondragstart={(event) => {
          // Firefox will not start a drag without something on the DataTransfer, though what this
          // reads is `dragging`.
          event.dataTransfer?.setData('text/plain', criterion.key)
          if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move'
          dragging = index
        }}
        ondragover={(event) => {
          if (dragging === null) return

          event.preventDefault()
          over = index
        }}
        ondragleave={() => {
          if (over === index) over = null
        }}
        ondrop={(event) => {
          event.preventDefault()
          if (dragging !== null) move(dragging, index)
          dragging = over = null
        }}
        ondragend={() => (dragging = over = null)}
      >
        <div class="flex items-center gap-2">
          <GripVerticalIcon class="size-4 shrink-0 cursor-grab opacity-50" aria-hidden="true" />
          <span
            class="flex size-6 shrink-0 items-center justify-center rounded-full preset-filled-primary-500 text-xs font-bold"
            aria-hidden="true"
          >
            {index + 1}
          </span>
          <!-- A fixed width, so every row's switches start at the same place and read as a column. -->
          <span class="w-24 shrink-0 font-semibold">{CRITERION_NAMES[criterion.key]}</span>

          <SegmentedControl
            value={criterion.direction}
            onValueChange={(details) => {
              if (details.value) criterion.direction = details.value as SortDirection
            }}
          >
            <SegmentedControl.Label class="sr-only">
              {CRITERION_NAMES[criterion.key]} direction
            </SegmentedControl.Label>
            <SegmentedControl.Control class={segmentControl}>
              {#each directionsOf(criterion.key) as direction (direction)}
                <SegmentedControl.Item value={direction} class={segmentItem}>
                  <SegmentedControl.ItemText>
                    {DIRECTION_LABELS[criterion.key][direction]}
                  </SegmentedControl.ItemText>
                  <SegmentedControl.ItemHiddenInput />
                </SegmentedControl.Item>
              {/each}
            </SegmentedControl.Control>
          </SegmentedControl>

          <!-- Which ordering Rarity reads. Its own control, and exclusive: one criterion with a
               choice of key, never two criteria competing for a slot. -->
          {#if criterion.key === 'rarity'}
            <SegmentedControl
              value={criterion.rarityKey}
              onValueChange={(details) => {
                if (details.value && criterion.key === 'rarity') {
                  criterion.rarityKey = details.value as RarityKey
                }
              }}
            >
              <SegmentedControl.Label class="sr-only">Rarity ordering</SegmentedControl.Label>
              <SegmentedControl.Control class={segmentControl}>
                {#each Object.entries(RARITY_KEY_LABELS) as [key, label] (key)}
                  <SegmentedControl.Item value={key} class={segmentItem}>
                    <SegmentedControl.ItemText>{label}</SegmentedControl.ItemText>
                    <SegmentedControl.ItemHiddenInput />
                  </SegmentedControl.Item>
                {/each}
              </SegmentedControl.Control>
            </SegmentedControl>
          {/if}

          <button
            type="button"
            class="btn-icon btn-icon-sm ml-auto hover:preset-tonal"
            title="Rank higher"
            aria-label="Rank {CRITERION_NAMES[criterion.key]} higher"
            disabled={index === 0}
            onclick={() => move(index, index - 1)}
          >
            <ArrowUpIcon class="size-4" />
          </button>
          <button
            type="button"
            class="btn-icon btn-icon-sm hover:preset-tonal"
            title="Rank lower"
            aria-label="Rank {CRITERION_NAMES[criterion.key]} lower"
            disabled={index === criteria.length - 1}
            onclick={() => move(index, index + 1)}
          >
            <ArrowDownIcon class="size-4" />
          </button>
          <!-- One criterion is the fewest a sort can have, so the last one cannot go. -->
          <button
            type="button"
            class="btn-icon btn-icon-sm hover:preset-tonal-error"
            title="Don't sort by this"
            aria-label="Don't sort by {CRITERION_NAMES[criterion.key]}"
            disabled={criteria.length === 1}
            onclick={() => drop(index)}
          >
            <XIcon class="size-4" />
          </button>
        </div>
      </li>
    {/each}
  </ol>

  {#if dropped.length > 0}
    <div class="flex flex-wrap items-center gap-2">
      <span class="text-sm opacity-60">Also sort by</span>
      {#each dropped as key (key)}
        <button type="button" class="btn btn-sm preset-tonal" onclick={() => restore(key)}>
          <PlusIcon class="size-4" />
          <span>{CRITERION_NAMES[key]}</span>
        </button>
      {/each}
    </div>
  {/if}

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
