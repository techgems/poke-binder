<script lang="ts">
  import { Toast } from '@skeletonlabs/skeleton-svelte'
  import MousePointerClickIcon from '@lucide/svelte/icons/mouse-pointer-click'
  import PlusIcon from '@lucide/svelte/icons/plus'
  import RedoIcon from '@lucide/svelte/icons/redo-2'
  import SearchIcon from '@lucide/svelte/icons/search'
  import Trash2Icon from '@lucide/svelte/icons/trash-2'
  import UndoIcon from '@lucide/svelte/icons/undo-2'

  import ActionSidebar from './components/ActionSidebar.svelte'
  import Modal from './components/Modal.svelte'
  import { toaster } from './components/toaster'
  import WorkspacePanel from './components/WorkspacePanel.svelte'
  import { DEFAULT_WORKSPACE_TAB, type WorkspaceTab } from './components/workspace-tab'
  import { clickAdd } from './stores/click-add.svelte'
  import { history } from './stores/history.svelte'
  import { save } from './stores/save.svelte'

  let searchOpen = $state(false)

  // Failures already reported. The save store counts them; this decides which are news.
  let toastedFailures = 0

  /**
   * Says so when a save could not be stored.
   *
   * The workspace saves silently and has nowhere else to say anything, so a failure that is not
   * raised here is a failure the user finds out about by losing work. It reads the count rather
   * than the status because a second failed save leaves the status on `error` and would never
   * announce itself; the guard is what keeps the first run, on mount, from reporting nothing.
   *
   * The scheduler has already retried once by the time this fires and does not keep retrying on a
   * timer, so what happens next is the user's next edit or page flip -- which is what this says,
   * rather than promising a retry that is not scheduled.
   */
  $effect(() => {
    if (save.failures <= toastedFailures) return

    toastedFailures = save.failures

    toaster.error({
      title: 'Not saved',
      description: 'Your changes are still here. The next edit you make will save them too.',
    })
  })

  // Held here rather than inside the panel so the sidebar can switch tabs as well as the tab strip.
  let tab = $state<WorkspaceTab>(DEFAULT_WORKSPACE_TAB)

  /**
   * Ctrl/Cmd+Z and its redo pair, bound to the window because what they undo is the binder rather
   * than whatever has focus -- but not while focus is in a field, where the browser's own undo is
   * the one the user means.
   */
  function onKeyDown(event: KeyboardEvent): void {
    if (!(event.ctrlKey || event.metaKey) || event.altKey) return

    const key = event.key.toLowerCase()

    if (key !== 'z' && key !== 'y') return

    // instanceof rather than a cast: a keydown can be dispatched at the window itself, which has no
    // closest() and would throw out of the handler.
    if (
      event.target instanceof Element &&
      event.target.closest('input, textarea, select, [contenteditable]')
    ) {
      return
    }

    event.preventDefault()

    if (key === 'y' || event.shiftKey) history.redo()
    else history.undo()
  }
</script>

<svelte:window onkeydown={onKeyDown} />

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
    <!-- A switch, not an action: aria-pressed is what says so, and the filled state is the same
         claim made in colour. It sits with the other rail buttons rather than in the binder view
         because it governs both halves of that view -- the pockets and the tray strip. -->
    <button
      type="button"
      class="btn-icon btn-icon-lg {clickAdd.enabled ? 'preset-tonal-primary' : 'hover:preset-tonal'}"
      title={clickAdd.enabled
        ? 'Click-add mode is on — clicking a card moves it'
        : 'Click-add mode is off — clicking a card opens it for a look'}
      aria-label="Click-add mode"
      aria-pressed={clickAdd.enabled}
      onclick={() => clickAdd.toggle()}
    >
      <MousePointerClickIcon class="size-6" />
    </button>
    <button
      type="button"
      class="btn-icon btn-icon-lg hover:preset-tonal"
      title="Undo"
      aria-label="Undo"
      disabled={!history.canUndo}
      onclick={() => history.undo()}
    >
      <UndoIcon class="size-6" />
    </button>
    <button
      type="button"
      class="btn-icon btn-icon-lg hover:preset-tonal"
      title="Redo"
      aria-label="Redo"
      disabled={!history.canRedo}
      onclick={() => history.redo()}
    >
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

<!-- Positioned by Zag from the toaster's own placement, so it needs no wrapper of its own. It sits
     outside the workspace column because a toast belongs to the page rather than to the panel. -->
<Toast.Group {toaster}>
  {#snippet children(toast)}
    <Toast
      {toast}
      class="card grid w-80 grid-cols-[1fr_auto] items-start gap-3 p-4 shadow-xl {toast.type ===
      'error'
        ? 'preset-filled-error-500'
        : 'preset-filled-surface-100-900'}"
    >
      <div class="space-y-1">
        <Toast.Title class="font-semibold">{toast.title}</Toast.Title>
        {#if toast.description}
          <Toast.Description class="text-sm opacity-90">{toast.description}</Toast.Description>
        {/if}
      </div>
      <Toast.CloseTrigger class="btn-icon btn-icon-sm hover:preset-tonal" aria-label="Dismiss" />
    </Toast>
  {/snippet}
</Toast.Group>

<Modal bind:open={searchOpen} title="Search cards">
  <input class="input" type="search" placeholder="Search by card name…" />
  <p class="opacity-75 text-sm">Start typing to look up a card in the catalog.</p>
</Modal>
