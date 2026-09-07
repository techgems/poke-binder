<script lang="ts">
  import ChevronLeftIcon from '@lucide/svelte/icons/chevron-left'
  import ChevronRightIcon from '@lucide/svelte/icons/chevron-right'

  import { binderPage } from './binder-page.svelte'
</script>

<!-- Under the pages rather than over them: the top of this panel was reclaimed for the binder, and
     a control the user reaches for between placements belongs near the tray they are placing from.

     Arrow keys work too. They are bound to the window rather than to a page, because the thing
     being turned is the binder, not whichever pocket happens to have focus -- but only when focus
     is not in a field, or typing a card name in the search box would flip pages under the user. -->
<svelte:window
  onkeydown={(event) => {
    const target = event.target as HTMLElement | null

    if (target?.closest('input, textarea, select, [contenteditable]')) return

    if (event.key === 'ArrowLeft') binderPage.goBack()
    if (event.key === 'ArrowRight') binderPage.goForward()
  }}
/>

<nav class="flex shrink-0 items-center justify-center gap-3" aria-label="Binder pages">
  <button
    type="button"
    class="btn-icon btn-icon-sm hover:preset-tonal"
    aria-label="Previous pages"
    disabled={!binderPage.canGoBack}
    onclick={() => binderPage.goBack()}
  >
    <ChevronLeftIcon class="size-4" />
  </button>

  <!-- aria-live so turning a page announces where you landed; the buttons alone would leave a
       screen reader user with no idea the binder moved. -->
  <p class="min-w-40 text-center text-sm opacity-75 tabular-nums" aria-live="polite">
    {binderPage.spreadLabel}
  </p>

  <button
    type="button"
    class="btn-icon btn-icon-sm hover:preset-tonal"
    aria-label="Next pages"
    disabled={!binderPage.canGoForward}
    onclick={() => binderPage.goForward()}
  >
    <ChevronRightIcon class="size-4" />
  </button>
</nav>
