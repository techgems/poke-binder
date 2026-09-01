<script lang="ts" module>
  /** The filtering systems the Add Cards modal can be driven by. Exactly one is ever active. */
  export type SearchMode = 'simple' | 'advanced' | 'ai'

  interface SearchModeOption {
    value: SearchMode
    label: string
    /** Set while the mode has nothing behind it yet: shown, but greyed out and unselectable. */
    comingSoon?: boolean
  }

  export const SEARCH_MODES: SearchModeOption[] = [
    { value: 'simple', label: 'Simple Search' },
    { value: 'advanced', label: 'Advanced Filters' },
    { value: 'ai', label: 'AI Search', comingSoon: true },
  ]

  /** Advanced is the only system that is actually wired up, so it is where the modal starts. */
  export const DEFAULT_SEARCH_MODE: SearchMode = 'advanced'
</script>

<script lang="ts">
  import { SegmentedControl } from '@skeletonlabs/skeleton-svelte'

  interface Props {
    /** The active filtering system. Bindable. */
    mode?: SearchMode
  }

  let { mode = $bindable(DEFAULT_SEARCH_MODE) }: Props = $props()
</script>

<SegmentedControl
  value={mode}
  onValueChange={(details) => (mode = details.value as SearchMode)}
  class="w-full"
>
  <SegmentedControl.Label>Search by</SegmentedControl.Label>
  <SegmentedControl.Control>
    <SegmentedControl.Indicator />
    {#each SEARCH_MODES as option (option.value)}
      <SegmentedControl.Item value={option.value} disabled={option.comingSoon}>
        <SegmentedControl.ItemText>{option.label}</SegmentedControl.ItemText>
        {#if option.comingSoon}
          <span class="badge preset-tonal-secondary ml-2">Coming Soon</span>
        {/if}
        <SegmentedControl.ItemHiddenInput />
      </SegmentedControl.Item>
    {/each}
  </SegmentedControl.Control>
</SegmentedControl>
