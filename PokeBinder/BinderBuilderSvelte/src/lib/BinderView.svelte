<!--
  Not implemented yet. Draws the shape the real view is meant to take — a page of empty slots with
  a card tray beside it — so the surrounding layout sits at the right proportions. There is no drag
  and drop, no page navigation and no state here: it is scaffolding, not a first cut.
-->
<script lang="ts">
  /** Slots on one binder page. Hardcoded to a 3x3 sheet purely to fill the layout out. */
  const SLOTS_PER_PAGE = 9
</script>

<!-- The tray is the same width as the Add tab's Selected column, so the two tabs line up and
     switching between them does not shift the layout under the cursor. -->
<div class="grid min-h-0 flex-1 grid-cols-[1fr_16rem] gap-4">
  <section class="flex min-h-0 flex-col gap-3 rounded-container border border-surface-200-800/50 p-3">
    <header class="flex shrink-0 items-center justify-between">
      <h3 class="font-semibold">Page 1</h3>
      <span class="text-xs opacity-60">Binder view — not implemented</span>
    </header>

    <!-- The page is sized off the available height, not the available width: three rows of 719:1000
         cards are far taller than they are wide, so letting width lead would push the bottom row
         off the panel. aspect-[2157/3000] is the 3x3 sheet's own ratio, so width follows height and
         the whole page always fits. -->
    <div class="flex min-h-0 flex-1 items-center justify-center overflow-hidden">
      <div class="grid aspect-[2157/3000] h-full grid-cols-3 grid-rows-3 gap-3">
        {#each Array(SLOTS_PER_PAGE) as _, slot (slot)}
          <div
            class="rounded-container border-2 border-dashed border-surface-200-800/60 bg-surface-200-800/20"
          ></div>
        {/each}
      </div>
    </div>
  </section>

  <aside class="grid min-h-0 place-items-center rounded-container border border-surface-200-800/50">
    <p class="px-3 text-center opacity-60">
      Card tray — cards picked in Add Cards will land here to drag into the page
    </p>
  </aside>
</div>
