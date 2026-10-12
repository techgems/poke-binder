<!--
  Adds a whole set to the binder in one action, rather than card by card through search. A wizard:
  pick the set, then answer what is left -- which rarities, in what order, and where it goes.

  The server places the cards (AddSetToBinder); what this tab shows beforehand is a preview of the
  same arithmetic, so it can ask about a set that does not fit before anything is sent. Once it is
  in, the workspace turns to the Binder tab, and one Undo takes the set out again.
-->
<script lang="ts">
  import ArrowLeftIcon from '@lucide/svelte/icons/arrow-left'
  import ArrowRightToLineIcon from '@lucide/svelte/icons/arrow-right-to-line'
  import EraserIcon from '@lucide/svelte/icons/eraser'
  import FileIcon from '@lucide/svelte/icons/file'
  import LibraryBigIcon from '@lucide/svelte/icons/library-big'
  import SquareDashedIcon from '@lucide/svelte/icons/square-dashed'
  import TriangleAlertIcon from '@lucide/svelte/icons/triangle-alert'
  import type { Component } from 'svelte'

  import {
    CardSearchClient,
    type SetRarityCount,
    type SetsFilter,
    type StarterFilters,
  } from '../../clients/CardSearchClient'
  import { toaster } from '../../components/toaster'
  import { loadSearchFilters } from '../../preloads/search-filters'
  import { binderPage } from '../../stores/binder-page.svelte'
  import type { CriterionKey, SortCriterion } from '../binder/sort/reorder-criteria'
  import SortCriteriaEditor from '../binder/sort/SortCriteriaEditor.svelte'
  import { MAX_PAGES } from '../settings/binder-sizes'
  import { addSetToBinder } from './add-set'
  import { setPlacement, type ExistingCards, type PageKind } from './set-placement'
  import SetImage from './SetImage.svelte'
  import SetPicker from './SetPicker.svelte'

  interface Props {
    /** A set went in. The workspace turns to the binder to show it. */
    onadded?: () => void
    /** The add is out. Bindable, so the page can keep undo away from the binder meanwhile. */
    busy?: boolean
  }

  let { onadded, busy = $bindable(false) }: Props = $props()

  /** No set criterion: every card in the wizard comes from the one set. */
  const ORDER_KEYS: readonly CriterionKey[] = ['rarity', 'name', 'number']

  // The sets and their series are the search's filter groups, loaded once for both tabs.
  const { preloaded, refreshed } = loadSearchFilters()

  let filters = $state<StarterFilters>(preloaded.filters)
  let loading = $state(refreshed !== null && preloaded.filters.sets.length === 0)
  let loadError = $state(false)

  refreshed
    ?.then((loaded) => (filters = loaded))
    .catch(() => (loadError = true))
    .finally(() => (loading = false))

  let picked = $state<SetsFilter | null>(null)
  let step = $state<'pick' | 'options'>('pick')

  // The answers. Reset whenever a different set is picked, since a rarity left out of one set
  // means nothing for the next. Rarities by name, which is how the server matches them.
  let excluded = $state<string[]>([])
  let criteria = $state<SortCriterion[]>([{ key: 'number', direction: 'asc' }])
  let emptyFirst = $state(false)
  let existing = $state<ExistingCards>('after')
  let addPages = $state(false)

  // How many cards the picked set has of each rarity, commonest first -- null while they are being
  // counted. Counted by the server, since nothing the browser holds says how big a set is.
  let rarities = $state<SetRarityCount[] | null>(null)
  let countError = $state(false)
  let counting: AbortController | null = null

  function pick(set: SetsFilter): void {
    if (picked?.id !== set.id) {
      excluded = []
      addPages = false
      count(set.id)
    }

    picked = set
    step = 'options'
  }

  function count(setId: number): void {
    counting?.abort()

    const controller = new AbortController()

    counting = controller
    rarities = null
    countError = false

    CardSearchClient.getSetRarityCounts(setId, controller.signal)
      .then((response) => {
        if (counting === controller) rarities = response.rarities
      })
      .catch(() => {
        if (counting === controller) countError = true
      })
  }

  const seriesName = $derived(
    picked ? (filters.series.find((s) => s.id === picked!.seriesId)?.name ?? '') : '',
  )

  const totalCards = $derived((rarities ?? []).reduce((sum, r) => sum + r.count, 0))

  // Cards with no rarity cannot be left out -- there is no name to leave out -- so they always go in.
  const setCards = $derived(
    (rarities ?? [])
      .filter((r) => r.rarity === null || !excluded.includes(r.rarity))
      .reduce((sum, r) => sum + r.count, 0),
  )

  function toggleRarity(rarity: SetRarityCount): void {
    const name = rarity.rarity

    if (name === null) return

    if (excluded.includes(name)) {
      excluded = excluded.filter((e) => e !== name)
    } else if (setCards > rarity.count) {
      // Something always stays in: a set with every card left out is no set at all.
      excluded = [...excluded, name]
    }
  }

  const placement = $derived(
    setPlacement(binderPage.slots, binderPage.cardsPerPage, binderPage.pages, {
      setCards,
      emptyFirst,
      existing,
    }),
  )

  const hasCards = $derived(placement.placed > 0)
  const tooBig = $derived(placement.pagesNeeded > MAX_PAGES)
  const needsPages = $derived(placement.pagesToAdd > 0)

  const pageRange = $derived(
    placement.startPage === placement.endPage
      ? `page ${placement.startPage}`
      : `pages ${placement.startPage}–${placement.endPage}`,
  )

  const canAdd = $derived(
    !busy && picked !== null && setCards > 0 && !tooBig && (!needsPages || addPages),
  )

  function plural(count: number, one: string, many = `${one}s`): string {
    return `${count} ${count === 1 ? one : many}`
  }

  interface ExistingOption {
    value: ExistingCards
    icon: Component
    title: string
    detail: string
  }

  const existingOptions = $derived<ExistingOption[]>([
    {
      value: 'after',
      icon: FileIcon,
      title: 'Right after them',
      detail: `The set starts on page ${placement.usedPages + 1}, the first page after the last card.`,
    },
    {
      value: 'buffer',
      icon: SquareDashedIcon,
      title: 'After a blank page',
      detail: `Page ${placement.usedPages + 1} is left empty and the set starts on page ${placement.usedPages + 2}.`,
    },
    {
      value: 'end',
      icon: ArrowRightToLineIcon,
      title: 'Set first',
      detail: 'The set takes the front of the binder and the cards already here move after it.',
    },
  ])

  const PAGE_STYLES: Record<PageKind, string> = {
    existing: 'bg-surface-500/60',
    set: 'bg-primary-500',
    buffer: 'border border-dashed border-surface-500',
    blank: 'bg-surface-500/15',
  }

  const PAGE_NAMES: Record<PageKind, string> = {
    existing: 'cards already in the binder',
    set: 'the set',
    buffer: 'blank, as a buffer',
    blank: 'empty',
  }

  // Only the kinds the map is actually showing, so the legend never explains a colour that is not
  // there.
  const legend = $derived(
    (['existing', 'buffer', 'set', 'blank'] as PageKind[]).filter((kind) =>
      placement.pages.includes(kind),
    ),
  )

  /**
   * Stays on the options until the server answers, so a failure has somewhere to be reported and
   * the answers are still there to try again with. Nothing on screen changes until it succeeds;
   * then the binder is shown and the tab is back at the picker for the next set.
   */
  async function add(): Promise<void> {
    if (!canAdd || !picked) return

    const set = picked

    busy = true

    try {
      const response = await addSetToBinder({
        setId: set.id,
        leaveOut: [...excluded],
        criteria: criteria.map((criterion) => ({ ...criterion })),
        emptyFirst: hasCards && emptyFirst,
        existing,
        addPages: needsPages && addPages,
      })

      if (!response) return

      toaster.success({
        title: `${set.name} added`,
        description: `${plural(response.added.length, 'card')} placed. Undo takes them out again.`,
      })

      step = 'pick'
      emptyFirst = false
      addPages = false

      onadded?.()
    } catch {
      toaster.error({
        title: 'Not added',
        description: 'The binder is as it was. Try again in a moment.',
      })
    } finally {
      busy = false
    }
  }
</script>

{#if binderPage.binderId === null}
  <p class="p-4 opacity-60">Open a binder to add a set to it.</p>
{:else if step === 'pick'}
  <div class="flex min-h-0 flex-1 flex-col gap-4">
    <header class="mx-auto w-full max-w-[88rem] space-y-1">
      <h2 class="h5">Pick a set</h2>
      <p class="text-sm opacity-75">
        Every card in it goes straight into the pockets. The tray is left alone.
      </p>
    </header>

    {#if loading}
      <p class="py-8 text-center opacity-60">Loading sets…</p>
    {:else if loadError && filters.sets.length === 0}
      <p class="py-8 text-center text-error-500">The sets could not be loaded. Try again later.</p>
    {:else}
      <SetPicker sets={filters.sets} series={filters.series} {picked} onpick={pick} />
    {/if}
  </div>
{:else if picked}
  <div class="flex min-h-0 flex-1 flex-col gap-3">
    <!-- relative for the same reason as the settings tab: the option cards hide their inputs with
         sr-only, which is absolutely positioned and needs a positioned ancestor in the scroller. -->
    <div class="relative min-h-0 flex-1 overflow-y-auto pr-1">
      <div class="mx-auto w-full max-w-[88rem] space-y-6">
        <!-- The set, as picked. -->
        <header class="flex items-center gap-4">
          <SetImage url={picked.imageUrl} class="w-32 shrink-0" />
          <div class="min-w-0 flex-1">
            <p class="text-sm opacity-60">{seriesName}</p>
            <h2 class="h4 truncate">{picked.name}</h2>
            <p class="text-sm opacity-75">
              {#if rarities}
                {plural(totalCards, 'card')} in {plural(rarities.length, 'rarity', 'rarities')}
              {:else if countError}
                <span class="text-error-500">The set's cards could not be counted.</span>
              {:else}
                Counting the set's cards…
              {/if}
            </p>
          </div>
          <button
            type="button"
            class="btn preset-tonal"
            disabled={busy}
            onclick={() => (step = 'pick')}
          >
            <ArrowLeftIcon class="size-4" />
            <span>Change set</span>
          </button>
        </header>

        <div class="grid gap-x-20 gap-y-6 lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)]">
          <!-- What goes in: which rarities, in what order. -->
          <div class="space-y-6">
            <section class="space-y-3" aria-labelledby="rarities-heading">
              <div class="space-y-1">
                <h3 id="rarities-heading" class="h5">Rarities</h3>
                <p class="text-sm opacity-75">
                  Leave out any you don't want. {plural(setCards, 'card')} of {totalCards} go in.
                </p>
              </div>

              <div class="flex flex-wrap gap-2">
                {#each rarities ?? [] as rarity (rarity.rarity)}
                  {@const included = rarity.rarity === null || !excluded.includes(rarity.rarity)}
                  {@const label = rarity.rarity ?? 'No rarity'}
                  <button
                    type="button"
                    class="btn btn-sm border-2 {included
                      ? 'border-primary-500 preset-tonal-primary'
                      : 'border-transparent preset-tonal opacity-60'}"
                    aria-pressed={included}
                    disabled={rarity.rarity === null}
                    title={rarity.rarity === null
                      ? 'Cards with no rarity always go in'
                      : included
                        ? `Leave out ${label}`
                        : `Put ${label} back`}
                    onclick={() => toggleRarity(rarity)}
                  >
                    <span class={included ? '' : 'line-through'}>{label}</span>
                    <span class="badge preset-tonal text-xs">{rarity.count}</span>
                  </button>
                {/each}
              </div>

              {#if excluded.length > 0}
                <button type="button" class="btn btn-sm hover:preset-tonal" onclick={() => (excluded = [])}>
                  Put them all back
                </button>
              {/if}
            </section>

            <section class="space-y-3" aria-labelledby="order-heading">
              <div class="space-y-1">
                <h3 id="order-heading" class="h5">Order</h3>
                <p class="text-sm opacity-75">
                  The first criterion decides; the ones below it only break its ties.
                </p>
              </div>
              <SortCriteriaEditor bind:criteria keys={ORDER_KEYS} />
            </section>
          </div>

          <!-- Where it goes. -->
          <div class="space-y-6">
            <section class="space-y-3" aria-labelledby="placement-heading">
              <div class="space-y-1">
                <h3 id="placement-heading" class="h5">In the binder</h3>
                <p class="text-sm opacity-75">
                  {#if hasCards}
                    The binder holds {plural(placement.placed, 'card')} on its first {plural(
                      placement.usedPages,
                      'page',
                    )}.
                  {:else}
                    The binder is empty, so the set starts on page 1.
                  {/if}
                </p>
              </div>

              {#if hasCards}
                <label
                  class="card flex cursor-pointer gap-3 border-2 px-3 py-2 transition-colors has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-primary-500 {emptyFirst
                    ? 'border-error-500 preset-tonal-error'
                    : 'border-transparent preset-tonal hover:border-surface-400-600'}"
                >
                  <input class="sr-only" type="checkbox" bind:checked={emptyFirst} />
                  <EraserIcon class="mt-0.5 size-4 shrink-0" />
                  <span>
                    <span class="block font-semibold">Empty the binder first</span>
                    <span class="block text-sm opacity-75">
                      Its {plural(placement.placed, 'card')} are removed, not sent to the tray. Undo
                      brings them back.
                    </span>
                  </span>
                </label>

                {#if !emptyFirst}
                  <fieldset class="space-y-2">
                    <legend class="label-text mb-2">Where the set goes</legend>
                    <div class="grid gap-2 md:grid-cols-3">
                      {#each existingOptions as option (option.value)}
                        {@const selected = existing === option.value}
                        {@const Icon = option.icon}
                        <label
                          class="card flex cursor-pointer gap-3 border-2 px-3 py-2 transition-colors has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-primary-500 {selected
                            ? 'border-primary-500 preset-tonal-primary'
                            : 'border-transparent preset-tonal hover:border-surface-400-600'}"
                        >
                          <input
                            class="sr-only"
                            type="radio"
                            name="existing-cards"
                            value={option.value}
                            bind:group={existing}
                          />
                          <Icon class="mt-0.5 size-4 shrink-0" />
                          <span>
                            <span class="block font-semibold">{option.title}</span>
                            <span class="block text-sm opacity-75">{option.detail}</span>
                          </span>
                        </label>
                      {/each}
                    </div>
                  </fieldset>
                {/if}
              {/if}
            </section>

            <!-- The binder afterwards, one block per page. Pages past the binder's current last
                 page are the ones that would be added, and carry a ring to say so. -->
            <section class="space-y-2" aria-labelledby="map-heading">
              <div class="flex items-baseline justify-between gap-4">
                <h4 id="map-heading" class="font-semibold">Afterwards</h4>
                <p class="text-sm opacity-75">
                  The set fills {pageRange}.
                </p>
              </div>

              <ol class="flex flex-wrap gap-1" aria-label="Pages of the binder afterwards">
                {#each placement.pages as kind, index (index)}
                  {@const added = index >= binderPage.pages}
                  <li
                    class="h-8 w-6 rounded-[3px] {PAGE_STYLES[kind]} {added
                      ? 'ring-2 ring-warning-500 ring-offset-1 ring-offset-surface-50-950'
                      : ''}"
                    title="Page {index + 1}: {PAGE_NAMES[kind]}{added ? ' (added)' : ''}"
                  ></li>
                {/each}
              </ol>

              <ul class="flex flex-wrap gap-x-4 gap-y-1 text-xs opacity-75">
                {#each legend as kind (kind)}
                  <li class="flex items-center gap-1.5">
                    <span class="inline-block h-3 w-2.5 rounded-[2px] {PAGE_STYLES[kind]}"></span>
                    {PAGE_NAMES[kind][0].toUpperCase() + PAGE_NAMES[kind].slice(1)}
                  </li>
                {/each}
                {#if needsPages}
                  <li class="flex items-center gap-1.5">
                    <span class="inline-block h-3 w-2.5 rounded-[2px] ring-2 ring-warning-500"></span>
                    Added
                  </li>
                {/if}
              </ul>
            </section>

            <!-- Only when the set does not fit. Pages go on at the end, which moves no card that is
                 already placed. -->
            {#if tooBig}
              <section class="card flex gap-3 border border-error-500/50 bg-error-500/10 p-4">
                <TriangleAlertIcon class="size-5 shrink-0 text-error-500" />
                <p class="text-sm">
                  <strong>This set does not fit.</strong> It would take {plural(
                    placement.pagesNeeded,
                    'page',
                  )}, and a binder has at most {MAX_PAGES}. Leave out some rarities, or empty the
                  binder first.
                </p>
              </section>
            {:else if needsPages}
              <section
                class="card space-y-3 border border-warning-500/50 bg-warning-500/10 p-4"
                aria-labelledby="fit-heading"
              >
                <header class="flex items-start gap-3">
                  <TriangleAlertIcon class="size-5 shrink-0 text-warning-500" />
                  <div class="space-y-1">
                    <h4 id="fit-heading" class="font-semibold">The binder is too small for it</h4>
                    <p class="text-sm opacity-75">
                      It needs {plural(placement.pagesNeeded, 'page')} and has {binderPage.pages}.
                    </p>
                  </div>
                </header>

                <label
                  class="card flex cursor-pointer gap-3 border-2 px-3 py-2 transition-colors has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-primary-500 {addPages
                    ? 'border-primary-500 preset-tonal-primary'
                    : 'border-transparent preset-tonal hover:border-surface-400-600'}"
                >
                  <input class="sr-only" type="checkbox" bind:checked={addPages} />
                  <LibraryBigIcon class="mt-0.5 size-4 shrink-0" />
                  <span>
                    <span class="block font-semibold">
                      Add {plural(placement.pagesToAdd, 'page')} at the end
                    </span>
                    <span class="block text-sm opacity-75">
                      No card already in the binder moves. Undo puts the cards back and keeps the
                      pages.
                    </span>
                  </span>
                </label>
              </section>
            {/if}
          </div>
        </div>
      </div>
    </div>

    <footer
      class="flex shrink-0 items-center justify-end gap-3 border-t border-surface-200-800/50 pt-3"
    >
      <p class="mr-auto text-sm opacity-60">
        {#if rarities === null}
          {countError ? 'Nothing to add until the set has been counted.' : 'Counting the set…'}
        {:else if tooBig}
          Too big for any binder as it stands.
        {:else if needsPages && !addPages}
          Add the pages it needs, or leave the set out.
        {:else}
          {plural(setCards, 'card')} into {pageRange}.
        {/if}
      </p>
      <button
        type="button"
        class="btn preset-tonal"
        disabled={busy}
        onclick={() => (step = 'pick')}
      >
        Back
      </button>
      <button type="button" class="btn preset-filled-primary-500" disabled={!canAdd} onclick={add}>
        {busy ? 'Adding…' : `Add ${plural(setCards, 'card')}`}
      </button>
    </footer>
  </div>
{/if}
