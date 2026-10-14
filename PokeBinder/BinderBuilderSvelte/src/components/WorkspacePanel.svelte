<script lang="ts">
  import BookOpenIcon from '@lucide/svelte/icons/book-open'
  import LibraryBigIcon from '@lucide/svelte/icons/library-big'
  import SearchIcon from '@lucide/svelte/icons/search'
  import SettingsIcon from '@lucide/svelte/icons/settings'
  import ShoppingBasketIcon from '@lucide/svelte/icons/shopping-basket'
  import { Tabs } from '@skeletonlabs/skeleton-svelte'

  import AddSetTab from '../features/add-set/AddSetTab.svelte'
  import BinderView from '../features/binder/BinderView.svelte'
  import SearchTab from '../features/search/SearchTab.svelte'
  import BinderSettingsTab from '../features/settings/BinderSettingsTab.svelte'
  import { binderPage } from '../stores/binder-page.svelte'
  import { tray } from '../stores/tray.svelte'
  import { DEFAULT_WORKSPACE_TAB, type WorkspaceTab } from './workspace-tab'

  interface Props {
    /** The selected panel. Bindable, so the sidebar can steer it too. */
    tab?: WorkspaceTab
    /** A set is being added. Bindable, so the page can keep undo away from the binder meanwhile. */
    addingSet?: boolean
    /** Additional classes for the panel. */
    class?: string
  }

  let {
    tab = $bindable(DEFAULT_WORKSPACE_TAB),
    addingSet = $bindable(false),
    class: classes = '',
  }: Props = $props()

  // The card tray, laid over the binder's pages (see BinderView). Closed unless someone opens it,
  // and closed again when they leave the Binder tab, so it is never found already open over them.
  let trayOpen = $state(false)

  $effect(() => {
    if (tab !== 'binder') trayOpen = false
  })

  // The tray is for placing cards, which only happens on the binder, so opening it from another
  // tab goes there too rather than opening over a panel it has nothing to do with.
  function toggleTray() {
    if (tab !== 'binder') {
      tab = 'binder'
      trayOpen = true

      return
    }

    trayOpen = !trayOpen
  }

  // The button is also somewhere to drop a card taken off a pocket, so sending one back to the
  // tray does not first need the tray opened. Lit only for a card coming off the page, as the
  // tray itself is.
  let overTrayButton = $state(false)

  // A drag can end without a dragleave here (dropped on a pocket after all, or cancelled with
  // Escape), so the highlight is only ever on while there is a drag to show it for.
  $effect(() => {
    if (!binderPage.isDraggingFromSlot) overTrayButton = false
  })

  function onDragOverTrayButton(event: DragEvent) {
    if (!binderPage.isDraggingFromSlot) return

    event.preventDefault()
    overTrayButton = true
  }
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
      <span>Card Search</span>
    </Tabs.Trigger>
    <Tabs.Trigger
      value="binder"
      class="btn hover:preset-tonal data-[selected]:preset-filled-primary-500"
    >
      <BookOpenIcon class="size-4" />
      <span>Binder</span>
    </Tabs.Trigger>
    <Tabs.Trigger
      value="set"
      class="btn hover:preset-tonal data-[selected]:preset-filled-primary-500"
    >
      <LibraryBigIcon class="size-4" />
      <span>Add a Set</span>
    </Tabs.Trigger>
    <Tabs.Trigger
      value="settings"
      class="btn hover:preset-tonal data-[selected]:preset-filled-primary-500"
    >
      <SettingsIcon class="size-4" />
      <span>Settings</span>
    </Tabs.Trigger>

    <!-- Not a tab: it opens the tray over the binder rather than selecting a panel, and standing
         apart at the right end is what says so. Otherwise it looks like its neighbours. -->
    <button
      type="button"
      class="btn relative ml-auto transition-colors {overTrayButton
        ? 'preset-tonal-primary ring-2 ring-primary-500'
        : trayOpen
          ? 'preset-filled-primary-500'
          : 'hover:preset-tonal'}"
      data-tray-toggle
      aria-expanded={trayOpen}
      aria-label="Card Tray, {tray.totalQuantity} {tray.totalQuantity === 1 ? 'card' : 'cards'}"
      onclick={toggleTray}
      ondragenter={onDragOverTrayButton}
      ondragover={onDragOverTrayButton}
      ondragleave={(event) => {
        // Crossing onto the icon or the count fires this too; only leaving the button counts.
        if (event.currentTarget.contains(event.relatedTarget as Node | null)) return

        overTrayButton = false
      }}
      ondrop={(event) => {
        event.preventDefault()
        overTrayButton = false
        binderPage.dropOnTray()
      }}
    >
      <ShoppingBasketIcon class="size-4" />
      <span>Card Tray</span>
      <!-- The count is always there, so the tray can stay shut and still say what is in it. -->
      <span
        class="badge-icon absolute -top-2 -right-2 text-xs font-semibold shadow {trayOpen
          ? 'preset-filled-surface-50-950'
          : 'preset-filled-primary-500'}"
        aria-hidden="true"
      >
        {tray.totalQuantity}
      </span>
    </button>
  </Tabs.List>

  <!--
    Both panels stay mounted — Zag hides the unselected one with the `hidden` attribute rather than
    tearing it down — so the search keeps its filters and results while you are over in the binder.
    That means no `display` utility may go on Tabs.Content: it would beat `[hidden]` and leave the
    inactive panel on screen. The inner div owns the layout instead.
  -->
  <Tabs.Content value="add" class="min-h-0 flex-1">
    <div class="flex h-full flex-col">
      <SearchTab />
    </div>
  </Tabs.Content>

  <Tabs.Content value="binder" class="min-h-0 flex-1">
    <div class="flex h-full flex-col">
      <BinderView {trayOpen} onclosetray={() => (trayOpen = false)} />
    </div>
  </Tabs.Content>

  <Tabs.Content value="set" class="min-h-0 flex-1">
    <div class="flex h-full flex-col">
      <AddSetTab bind:busy={addingSet} onadded={() => (tab = 'binder')} />
    </div>
  </Tabs.Content>

  <Tabs.Content value="settings" class="min-h-0 flex-1">
    <div class="flex h-full flex-col">
      <BinderSettingsTab />
    </div>
  </Tabs.Content>
</Tabs>
