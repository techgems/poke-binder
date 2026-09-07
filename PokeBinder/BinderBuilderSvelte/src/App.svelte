<script lang="ts">
  import PlusIcon from '@lucide/svelte/icons/plus'
  import RedoIcon from '@lucide/svelte/icons/redo-2'
  import SearchIcon from '@lucide/svelte/icons/search'
  import Trash2Icon from '@lucide/svelte/icons/trash-2'
  import UndoIcon from '@lucide/svelte/icons/undo-2'

  import ActionSidebar from './lib/ActionSidebar.svelte'
  import Modal from './lib/Modal.svelte'
  import WorkspacePanel, {
    DEFAULT_WORKSPACE_TAB,
    type WorkspaceTab,
  } from './lib/WorkspacePanel.svelte'

  let searchOpen = $state(false)

  // Held here rather than inside the panel so the sidebar can switch tabs as well as the tab strip.
  let tab = $state<WorkspaceTab>(DEFAULT_WORKSPACE_TAB)
</script>

<!-- The page frame -- theme, background, blobs and app bar -- belongs to the Razor layout
     (Pages/Shared/_BinderLayout.cshtml). What is left here is the workspace itself. -->
<div class="flex items-start gap-4 px-4">
  <ActionSidebar>
    <!-- Adding cards is a tab now rather than a dialog, so this jumps to it instead of opening
         anything. It stays because it is the one action the rail leads with. -->
    <button
      type="button"
      class="btn-icon btn-icon-lg preset-filled-primary-500"
      title="Add cards"
      aria-label="Add cards"
      onclick={() => (tab = 'add')}
    >
      <PlusIcon class="size-6" />
    </button>
    <button
      type="button"
      class="btn-icon btn-icon-lg hover:preset-tonal"
      title="Search"
      aria-label="Search"
      onclick={() => (searchOpen = true)}
    >
      <SearchIcon class="size-6" />
    </button>
    <button type="button" class="btn-icon btn-icon-lg hover:preset-tonal" title="Undo" aria-label="Undo">
      <UndoIcon class="size-6" />
    </button>
    <button type="button" class="btn-icon btn-icon-lg hover:preset-tonal" title="Redo" aria-label="Redo">
      <RedoIcon class="size-6" />
    </button>

    {#snippet danger()}
      <button
        type="button"
        class="btn-icon btn-icon-lg text-error-500 hover:preset-tonal-error"
        title="Delete"
        aria-label="Delete"
      >
        <Trash2Icon class="size-6" />
      </button>
    {/snippet}
  </ActionSidebar>

  <!-- One viewport tall and a column, so the panel below can claim the leftover height with
       flex-1 rather than a calc() that would have to restate this pt/pb in rem — the theme
       scales the root font size, so that arithmetic does not stay true. min-w-0 lets the
       workspace shrink past its content width instead of shoving the sidebar off screen. -->
  <main class="flex h-dvh min-w-0 flex-1 flex-col pt-20 pb-4">
    <WorkspacePanel bind:tab class="min-h-0 flex-1" />
  </main>
</div>

<Modal bind:open={searchOpen} title="Search cards">
  <input class="input" type="search" placeholder="Search by card name…" />
  <p class="opacity-75 text-sm">Start typing to look up a card in the catalog.</p>
</Modal>
