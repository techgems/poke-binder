<script lang="ts">
  import {
    Combobox,
    Portal,
    useListCollection,
    type ComboboxRootProps,
  } from '@skeletonlabs/skeleton-svelte'

  import type { FilterOption } from './filter-option'

  interface Props {
    /** Field label. */
    label: string
    /** Full option list for this field. */
    data: FilterOption[]
    /** Input placeholder. */
    placeholder?: string
    /** Selected option values. Bindable. */
    value?: string[]
  }

  let { label, data, placeholder = 'Search…', value = $bindable([]) }: Props = $props()

  let query = $state('')

  // Fall back to the full list when nothing matches, so the popup is never empty.
  const items = $derived.by(() => {
    const search = query.trim().toLowerCase()
    if (!search) return data

    const filtered = data.filter((item) => item.label.toLowerCase().includes(search))
    return filtered.length > 0 ? filtered : data
  })

  const collection = $derived(
    useListCollection({
      items,
      itemToString: (item) => item.label,
      itemToValue: (item) => item.value,
    }),
  )

  const selected = $derived(data.filter((option) => value.includes(option.value)))

  const onOpenChange = () => {
    query = ''
  }

  const onInputValueChange: ComboboxRootProps['onInputValueChange'] = (event) => {
    query = event.inputValue
  }

  const onValueChange: ComboboxRootProps['onValueChange'] = (event) => {
    value = event.value
  }
</script>

<div class="space-y-2">
  <Combobox
    multiple
    closeOnSelect
    {collection}
    {value}
    {onValueChange}
    {onOpenChange}
    {onInputValueChange}
    {placeholder}
    inputBehavior="autohighlight"
  >
    <Combobox.Label>{label}</Combobox.Label>
    <Combobox.Control>
      <Combobox.Input />
      <Combobox.Trigger />
    </Combobox.Control>
    <Portal>
      <Combobox.Positioner>
        <!-- Cap the popup so long lists scroll instead of running off the screen. -->
        <Combobox.Content class="max-h-[min(20rem,var(--available-height,20rem))] overflow-y-auto">
          {#each items as item (item.value)}
            <Combobox.Item {item}>
              <Combobox.ItemText>{item.label}</Combobox.ItemText>
              <Combobox.ItemIndicator />
            </Combobox.Item>
          {/each}
        </Combobox.Content>
      </Combobox.Positioner>
    </Portal>
  </Combobox>

  {#if selected.length > 0}
    <!-- Skeleton recommends rendering the selection outside the combobox when multiple. -->
    <div class="flex flex-wrap gap-1">
      {#each selected as option (option.value)}
        <span class="badge preset-filled-primary-500">{option.label}</span>
      {/each}
    </div>
  {/if}
</div>
