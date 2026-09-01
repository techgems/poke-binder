<script lang="ts">
  import PlusIcon from '@lucide/svelte/icons/plus'
  import RedoIcon from '@lucide/svelte/icons/redo-2'
  import SearchIcon from '@lucide/svelte/icons/search'
  import Trash2Icon from '@lucide/svelte/icons/trash-2'
  import UndoIcon from '@lucide/svelte/icons/undo-2'
  import { AppBar, Switch } from '@skeletonlabs/skeleton-svelte'

  import ActionSidebar from './lib/ActionSidebar.svelte'
  import AddCardsModal from './lib/AddCardsModal.svelte'
  import BackgroundBlobs from './lib/BackgroundBlobs.svelte'
  import Modal from './lib/Modal.svelte'

  let showDetails = $state(false)
  let searchOpen = $state(false)
  let addOpen = $state(false)
</script>

<!-- No opaque background here: the page colour comes from <html>, so the fixed -z-10 blob layer stays visible. -->
<div data-theme="PokeBinder" class="min-h-dvh">
  <BackgroundBlobs />

  <!-- The app bar is taken out of flow so the sticky sidebar rail spans the full viewport height. -->
  <AppBar class="fixed inset-x-0 top-0 z-10">
    <AppBar.Toolbar class="grid-cols-[1fr_2fr_1fr]">
      <AppBar.Lead>
        <span class="text-xl font-bold">PokeBinder</span>
      </AppBar.Lead>
      <AppBar.Headline class="flex justify-center">
        <span class="opacity-75">Binder Builder</span>
      </AppBar.Headline>
      <AppBar.Trail class="justify-end">
        <button type="button" class="btn preset-tonal-primary">Binders</button>
      </AppBar.Trail>
    </AppBar.Toolbar>
  </AppBar>

  <div class="flex items-start gap-4 px-4">
    <ActionSidebar>
      <button
        type="button"
        class="btn-icon btn-icon-lg preset-filled-primary-500"
        title="Add"
        aria-label="Add"
        onclick={() => (addOpen = true)}
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

    <main class="flex flex-1 justify-center pt-20 pb-4">
      <div class="card preset-filled-surface-100-900 p-6 space-y-4 w-full max-w-2xl">
        <header class="space-y-1">
          <h2 class="h2">Svelte SPA</h2>
          <p class="opacity-75">
            Rendered by Vite.NET inside an authenticated Razor page, styled with Skeleton.
          </p>
        </header>

        <Switch checked={showDetails} onCheckedChange={(e) => (showDetails = e.checked)}>
          <Switch.HiddenInput />
          <Switch.Control>
            <Switch.Thumb />
          </Switch.Control>
          <Switch.Label>Show details</Switch.Label>
        </Switch>

        {#if showDetails}
          <p>
            This component tree is mounted into the <code class="pre">#app</code> container emitted by
            the Vite.NET tag helper.
          </p>
        {/if}

        <footer>
          <button type="button" class="btn preset-filled-primary-500">Skeleton button</button>
        </footer>
      </div>
    </main>
  </div>

  <AddCardsModal bind:open={addOpen} />

  <Modal bind:open={searchOpen} title="Search cards">
    <input class="input" type="search" placeholder="Search by card name…" />
    <p class="opacity-75 text-sm">Start typing to look up a card in the catalog.</p>
  </Modal>
</div>
