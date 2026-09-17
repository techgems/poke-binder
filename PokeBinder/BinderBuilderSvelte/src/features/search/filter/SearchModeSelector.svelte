<script lang="ts">
  import { SegmentedControl } from '@skeletonlabs/skeleton-svelte'

  import { DEFAULT_SEARCH_MODE, SEARCH_MODES, type SearchMode } from '../utils/search-mode'

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
      <SegmentedControl.Item value={option.value}>
        <SegmentedControl.ItemText>{option.label}</SegmentedControl.ItemText>
        <SegmentedControl.ItemHiddenInput />
      </SegmentedControl.Item>
    {/each}
  </SegmentedControl.Control>
</SegmentedControl>
