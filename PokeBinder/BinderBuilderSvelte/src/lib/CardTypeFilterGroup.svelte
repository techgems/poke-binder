<script lang="ts" module>
  import type { FilterOption } from './filter-option'

  /** A card type option, shown as its energy symbol rather than its name. */
  export interface CardTypeOption extends FilterOption {
    /** Symbol art for the type, or null when there is none and the name is shown instead. */
    imageUrl: string | null
  }
</script>

<script lang="ts">
  import { ToggleGroup } from '@skeletonlabs/skeleton-svelte'

  interface Props {
    /** Field label. */
    label?: string
    /** The card types to offer. */
    options: CardTypeOption[]
    /** Selected card type values. Bindable. */
    value?: string[]
  }

  let { label = 'Card Type', options, value = $bindable([]) }: Props = $props()

  // Selecting nothing already means every type, so dimming would read as "all disabled" on open.
  // The unpicked types are only played down once a pick has actually narrowed the search.
  const dimUnpicked = $derived(value.length > 0)

  // Skeleton styles a toggle group as a segmented bar: one outlined box, dividers between items,
  // and a filled selected item. All three fight symbol art — the fill in particular swallows the
  // darker symbols — so the chrome is stripped back and selection is carried by a ring instead.
  const rootClass = 'flex flex-wrap gap-2 overflow-visible border-0 bg-transparent shadow-none'

  function itemClass(picked: boolean): string {
    return [
      'h-auto rounded-full border-0 bg-transparent p-0.5 transition duration-150',
      'hover:bg-transparent hover:opacity-100',
      picked ? 'ring-primary-500 ring-2' : '',
      dimUnpicked && !picked ? 'opacity-40 grayscale' : '',
    ].join(' ')
  }
</script>

<div class="space-y-2">
  <span class="label-text">{label}</span>
  <ToggleGroup
    multiple
    {value}
    onValueChange={(details) => (value = details.value)}
    class={rootClass}
  >
    {#each options as option (option.value)}
      {@const picked = value.includes(option.value)}
      <!-- The symbol carries no text, so the name has to reach assistive tech by label. -->
      <ToggleGroup.Item
        value={option.value}
        title={option.label}
        aria-label={option.label}
        class={itemClass(picked)}
      >
        {#if option.imageUrl}
          <img src={option.imageUrl} alt="" class="size-9 shrink-0" draggable="false" />
        {:else}
          <span
            class="border-surface-200-800 flex h-9 items-center rounded-full border px-3 text-xs"
          >
            {option.label}
          </span>
        {/if}
      </ToggleGroup.Item>
    {/each}
  </ToggleGroup>
</div>
