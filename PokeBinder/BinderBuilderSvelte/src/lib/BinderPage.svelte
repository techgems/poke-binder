<script lang="ts">
  import { binderPage, type DragSource } from './binder-page.svelte'
  import { clickAdd } from './click-add.svelte'

  interface Props {
    /**
     * A placed card was clicked with click-add mode off. Nothing has moved: the caller decides what
     * looking at a card means, which here is lifting it into the spotlight.
     */
    onspotlight?: (source: DragSource) => void
  }

  let { onspotlight }: Props = $props()

  /** Stand-in art for cards the catalog has no image for. */
  const CARD_BACK_URL = '/images/TcgImages/card-back.png'

  /** Between pockets on a page, and between the two pages of a spread. Both in rem. */
  const POCKET_GAP = 0.75
  const SPINE_GAP = 1.5

  /** A trading card, wide over tall. */
  const CARD_RATIO = 719 / 1000

  // Which pocket the pointer is currently over mid-drag, so exactly one lights up.
  let over = $state<number | null>(null)

  const spreadPages = $derived(binderPage.spreadPages)

  /**
   * One pocket's width, and with it the size of everything on screen.
   *
   * Two pages have to fit the box one page used to, so height can no longer drive this on its own:
   * on a wide, short panel the height allows a card the width has no room for. min() takes whichever
   * bound is tighter. cqw and cqh measure the panel around this rather than the viewport, so folding
   * the tray strip away re-sizes the pages instead of overflowing them.
   */
  const pocketWidth = $derived.by(() => {
    const pagesShown = spreadPages.length
    const columns = binderPage.columns
    const rows = binderPage.rows

    const gapsAcross = pagesShown * (columns - 1) * POCKET_GAP + (pagesShown - 1) * SPINE_GAP
    const gapsDown = (rows - 1) * POCKET_GAP

    const byWidth = `(100cqw - ${gapsAcross}rem) / ${columns * pagesShown}`
    const byHeight = `((100cqh - ${gapsDown}rem) / ${rows}) * ${CARD_RATIO}`

    return `min(${byWidth}, ${byHeight})`
  })

  // Says what a click will actually do, which is the whole point of the mode being a mode.
  function slotLabel(index: number) {
    const card = binderPage.slots[index]

    if (!card) return `Empty pocket ${index + 1}`

    return clickAdd.enabled
      ? `${card.name ?? 'Card'} in pocket ${index + 1}. Click to send it back to the tray.`
      : `${card.name ?? 'Card'} in pocket ${index + 1}. Click to take a closer look.`
  }
</script>

<!--
  The binder as it falls open: page one alone, then facing pairs.

  container-type: size is what lets the pockets be measured against this panel in CSS rather than in
  a resize handler -- 100cqw and 100cqh below are its width and height.
-->
<div
  class="flex size-full items-center justify-center"
  style="container-type: size; --pocket: {pocketWidth};"
>
  <div class="flex items-stretch" style="gap: {SPINE_GAP}rem;">
    {#each spreadPages as page, position (page.pageNumber)}
      {#if position > 0}
        <!-- The gutter. Without it two 4x3 pages read as one 8x3 grid, which is the wrong mental
             model for where a card lands -- and for which page it is saved on. -->
        <div class="w-px self-stretch bg-surface-200-800/60" aria-hidden="true"></div>
      {/if}

      <!-- Columns sized off the pocket rather than fractions of the page: the page is then exactly
           as wide as its cards, and the two halves of a spread cannot drift apart. -->
      <div
        class="grid self-center"
        style="gap: {POCKET_GAP}rem;
               grid-template-columns: repeat({binderPage.columns}, var(--pocket));"
      >
        {#each page.slots as slot (slot.index)}
          {@const index = slot.index}
          {@const card = slot.card}
          {@const isTarget = over === index}
          <!-- A button, so a pocket is reachable and operable without a drag: clicking a full one
               sends its card back to the tray, or lifts it for a look with click-add mode off.
               p-0 because a native button carries its own padding,
               which on a pocket this size leaves the card floating in the middle of its own slot.
               dragover has to preventDefault on every pass or the browser refuses the drop -- that
               is the API's way of asking whether this element accepts it. -->
          <button
            type="button"
            aria-label={slotLabel(index)}
            title={slotLabel(index)}
            style="width: var(--pocket);"
            class="relative aspect-[719/1000] overflow-hidden rounded-container border-2 p-0 transition-colors {isTarget
              ? 'border-primary-500 bg-primary-500/15'
              : card
                ? 'border-transparent'
                : 'border-dashed border-surface-200-800/60 bg-surface-200-800/20 hover:border-surface-200-800'}"
            ondragover={(event) => {
              if (!binderPage.dragged) return

              event.preventDefault()
              over = index
            }}
            ondragleave={() => {
              if (over === index) over = null
            }}
            ondrop={(event) => {
              event.preventDefault()
              over = null
              binderPage.dropOnSlot(index)
            }}
            onclick={() => {
              if (clickAdd.enabled) {
                binderPage.returnToTray(index)

                return
              }

              // Off, a click is a look. The way back to the tray is a button under the lifted
              // card, so the keyboard route this click used to be survives the mode being off.
              if (card) onspotlight?.({ kind: 'slot', card, index })
            }}
          >
            {#if card}
              <img
                src={card.imageUrl ?? CARD_BACK_URL}
                alt={card.imageUrl ? (card.name ?? 'Card') : 'No image available'}
                draggable="true"
                ondragstart={(event) => {
                  // Firefox will not start a drag without something on the DataTransfer, even
                  // though the payload this app reads is the module's own state.
                  event.dataTransfer?.setData('text/plain', String(card.id))
                  binderPage.startDrag({ kind: 'slot', card, index })
                }}
                ondragend={() => binderPage.endDrag()}
                class="size-full cursor-grab rounded-container object-contain active:cursor-grabbing"
              />
            {/if}
          </button>
        {/each}
      </div>
    {/each}
  </div>
</div>
