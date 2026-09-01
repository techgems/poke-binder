<script lang="ts">
  import {
    Combobox,
    Portal,
    useListCollection,
    type ComboboxRootProps,
  } from '@skeletonlabs/skeleton-svelte'

  import type { FilterOption } from './mock-filters'

  interface Props {
    /** Field label. */
    label: string
    /** Full option list for this field. */
    data: FilterOption[]
    /** Input placeholder. */
    placeholder?: string
  }

  let { label, data, placeholder = 'Search…' }: Props = $props()

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

  const onOpenChange = () => {
    query = ''
  }

  const onInputValueChange: ComboboxRootProps['onInputValueChange'] = (event) => {
    query = event.inputValue
  }
</script>

<Combobox
  {collection}
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
      <Combobox.Content>
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
