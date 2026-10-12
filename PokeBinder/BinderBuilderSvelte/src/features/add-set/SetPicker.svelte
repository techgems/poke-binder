<!--
  The wizard's first step: every set in the catalog, grouped by series, each as a tile with its
  image. Picking one is the whole of the step.
-->
<script lang="ts">
  import SearchIcon from '@lucide/svelte/icons/search'

  import type { SeriesFilter, SetsFilter } from '../../clients/CardSearchClient'
  import FilterCombobox from '../search/filter/FilterCombobox.svelte'
  import type { FilterOption } from '../search/types/filter-option'
  import SetImage from './SetImage.svelte'

  interface Props {
    sets: readonly SetsFilter[]
    series: readonly SeriesFilter[]
    /** The set picked last, if the user came back to change it. */
    picked: SetsFilter | null
    onpick: (set: SetsFilter) => void
  }

  let { sets, series, picked, onpick }: Props = $props()

  let query = $state('')

  // Series ids, as strings because that is what the combobox holds. None picked means every
  // series, not none of them.
  let pickedSeries = $state<string[]>([])

  // Newest set first. The sets filter carries no release date, so the id -- which grows as sets
  // are loaded -- stands in for one.
  const newestFirst = $derived(sets.toSorted((a, b) => b.id - a.id))

  // In the order the groups below appear, which is the order of each series' newest set.
  const seriesOptions = $derived.by((): FilterOption[] => {
    const ids = [...new Set(newestFirst.map((set) => set.seriesId))]

    return ids.flatMap((id) => {
      const found = series.find((s) => s.id === id)

      return found ? [{ label: found.name, value: String(found.id) }] : []
    })
  })

  // Newest series first, and newest set first inside each.
  const groups = $derived.by(() => {
    const term = query.trim().toLowerCase()

    const matching = newestFirst.filter(
      (set) =>
        (pickedSeries.length === 0 || pickedSeries.includes(String(set.seriesId))) &&
        (term === '' || set.name.toLowerCase().includes(term)),
    )

    const bySeries = new Map<number, SetsFilter[]>()

    for (const set of matching) {
      const members = bySeries.get(set.seriesId)

      if (members) members.push(set)
      else bySeries.set(set.seriesId, [set])
    }

    return [...bySeries].map(([seriesId, members]) => ({
      seriesId,
      name: series.find((s) => s.id === seriesId)?.name ?? 'Other sets',
      sets: members,
    }))
  })
</script>

<div class="flex min-h-0 flex-1 flex-col gap-4">
  <!-- The series field against the left edge of the sets below, the name field against the right.
       items-start: the series' picks wrap under its own field, and the name field stays put.

       The wrapper reserves the same scrollbar gutter and right padding as the scroller below, so
       the edges are the tiles' rather than the panel's. It never scrolls; the
       -m-1 p-1 only keeps the fields' focus rings clear of the edge an overflow box clips at. -->
  <div class="-m-1 shrink-0 overflow-y-auto p-1 pr-2 [scrollbar-gutter:stable]">
    <div class="mx-auto flex w-full max-w-[88rem] items-start justify-between gap-3">
      <div class="w-full max-w-72">
        <FilterCombobox
          label="Series"
          data={seriesOptions}
          placeholder="All series"
          bind:value={pickedSeries}
        />
      </div>

      <!-- flex gap-2, with the margin .label puts on the field taken off: the same space under the
           label as the combobox root puts under its own, so the two fields sit level. -->
      <label class="label flex w-full max-w-md flex-col gap-2">
        <span class="label-text">Set name</span>
        <span class="relative mt-0 block">
          <SearchIcon
            class="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 opacity-50"
            aria-hidden="true"
          />
          <input
            class="input pl-9"
            type="search"
            placeholder="Find a set by name…"
            bind:value={query}
          />
        </span>
      </label>
    </div>
  </div>

  <div class="min-h-0 flex-1 overflow-y-auto pr-1 [scrollbar-gutter:stable]">
    <div class="mx-auto w-full max-w-[88rem] space-y-6">
      {#each groups as group (group.seriesId)}
        <section class="space-y-2" aria-labelledby="series-{group.seriesId}">
          <h3 id="series-{group.seriesId}" class="h6 opacity-75">{group.name}</h3>

          <div class="grid grid-cols-[repeat(auto-fill,minmax(11rem,1fr))] gap-3">
            {#each group.sets as set (set.id)}
              {@const selected = picked?.id === set.id}
              <button
                type="button"
                class="card flex flex-col gap-2 border-2 p-2 text-left transition-colors focus-visible:ring-2 focus-visible:ring-primary-500 {selected
                  ? 'border-primary-500 preset-tonal-primary'
                  : 'border-transparent preset-tonal hover:border-surface-400-600'}"
                aria-pressed={selected}
                onclick={() => onpick(set)}
              >
                <SetImage url={set.imageUrl} class="w-full" />
                <span class="line-clamp-2 px-1 text-sm font-semibold">{set.name}</span>
              </button>
            {/each}
          </div>
        </section>
      {:else}
        <p class="py-8 text-center opacity-60">
          {query.trim() === '' && pickedSeries.length === 0
            ? 'No sets to show.'
            : 'No set matches the name and series picked.'}
        </p>
      {/each}
    </div>
  </div>
</div>
