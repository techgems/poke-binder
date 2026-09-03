<script lang="ts" module>
  /** The panels the workspace can show. Exactly one is ever selected. */
  export type WorkspaceTab = 'add' | 'binder'

  /** Add Cards is the only panel that is actually built, so it is where the workspace starts. */
  export const DEFAULT_WORKSPACE_TAB: WorkspaceTab = 'add'
</script>

<script lang="ts">
  import BookOpenIcon from '@lucide/svelte/icons/book-open'
  import SearchIcon from '@lucide/svelte/icons/search'
  import { Tabs } from '@skeletonlabs/skeleton-svelte'

  import AddCardsWorkspace from './AddCardsWorkspace.svelte'
  import BinderView from './BinderView.svelte'

  interface Props {
    /** The selected panel. Bindable, so the sidebar can steer it too. */
    tab?: WorkspaceTab
    /** Additional classes for the panel. */
    class?: string
  }

  let { tab = $bindable(DEFAULT_WORKSPACE_TAB), class: classes = '' }: Props = $props()
</script>

<!-- min-h-0 so the panel can be shorter than its content and hand the leftover height to whichever
     tab is showing, rather than growing the page into a scrollbar. -->
<Tabs
  value={tab}
  onValueChange={(details) => (tab = details.value as WorkspaceTab)}
  class="card preset-glass-surface border border-surface-200-800/50 flex min-h-0 w-full flex-col gap-4 p-4 shadow-xl {classes}"
>
  <Tabs.List class="flex shrink-0 gap-2">
    <Tabs.Trigger
      value="add"
      class="btn hover:preset-tonal data-[selected]:preset-filled-primary-500"
    >
      <SearchIcon class="size-4" />
      <span>Add Cards</span>
    </Tabs.Trigger>
    <Tabs.Trigger
      value="binder"
      class="btn hover:preset-tonal data-[selected]:preset-filled-primary-500"
    >
      <BookOpenIcon class="size-4" />
      <span>Binder</span>
    </Tabs.Trigger>
  </Tabs.List>

  <!--
    Both panels stay mounted — Zag hides the unselected one with the `hidden` attribute rather than
    tearing it down — so the search keeps its filters and results while you are over in the binder.
    That means no `display` utility may go on Tabs.Content: it would beat `[hidden]` and leave the
    inactive panel on screen. The inner div owns the layout instead.
  -->
  <Tabs.Content value="add" class="min-h-0 flex-1">
    <div class="flex h-full flex-col">
      <AddCardsWorkspace active={tab === 'add'} />
    </div>
  </Tabs.Content>

  <Tabs.Content value="binder" class="min-h-0 flex-1">
    <div class="flex h-full flex-col">
      <BinderView />
    </div>
  </Tabs.Content>
</Tabs>
