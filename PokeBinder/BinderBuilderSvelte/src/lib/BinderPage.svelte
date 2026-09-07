<script lang="ts">
  import { binderPage } from './binder-page.svelte'

  /** Stand-in art for cards the catalog has no image for. */
  const CARD_BACK_URL = '/images/TcgImages/card-back.png'

  // Which pocket the pointer is currently over mid-drag, so exactly one lights up.
  let over = $state<number | null>(null)

  function slotLabel(index: number) {
    const card = binderPage.slots[index]

    return card
      ? `${card.name ?? 'Card'} in pocket ${index + 1}. Click to send it back to the tray.`
      : `Empty pocket ${index + 1}`
  }
</script>

<!-- The page itself: nine pockets that take cards from the tray and give them back.

     Sized off the available height, not the available width: three rows of 719:1000 cards are far
     taller than they are wide, so letting width lead would push the bottom row off the panel. The
     card shape is on each pocket rather than on the sheet, and the sheet is w-fit around them: an
     aspect on the sheet has to include the gaps, which are the same pixel count horizontally and
     vertically and therefore not in the card's proportion -- pockets came out narrower than a card
     and every placed card sat letterboxed inside its own slot. -->
<div class="grid h-full w-fit grid-cols-3 grid-rows-3 gap-3">
  {#each binderPage.slots as card, index (index)}
    {@const isTarget = over === index}
    <!-- A button, so a pocket is reachable and operable without a drag: clicking a full one sends
         its card back to the tray. p-0 because a native button carries its own padding, which on a
         pocket this size leaves the card floating in the middle of its own slot. dragover has to
         preventDefault on every pass or the browser refuses the drop -- that is the API's way of
         asking whether this element accepts it. -->
    <button
      type="button"
      aria-label={slotLabel(index)}
      title={slotLabel(index)}
      class="relative aspect-[719/1000] h-full overflow-hidden rounded-container border-2 p-0 transition-colors {isTarget
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
      onclick={() => binderPage.returnToTray(index)}
    >
      {#if card}
        <img
          src={card.imageUrl ?? CARD_BACK_URL}
          alt={card.imageUrl ? (card.name ?? 'Card') : 'No image available'}
          draggable="true"
          ondragstart={(event) => {
            // Firefox will not start a drag without something on the DataTransfer, even though the
            // payload this app reads is the module's own state.
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
