<script lang="ts">
  import EyeIcon from '@lucide/svelte/icons/eye'
  import EyeOffIcon from '@lucide/svelte/icons/eye-off'
  import ShoppingBasketIcon from '@lucide/svelte/icons/shopping-basket'
  import Trash2Icon from '@lucide/svelte/icons/trash-2'
  import type { Snippet } from 'svelte'

  import { CARD_BACK_URL } from '../../card-art'
  import { binderPage, type DragSource } from '../../stores/binder-page.svelte'
  import { clickAdd } from '../../stores/click-add.svelte'
  import { spotlight } from '../../stores/spotlight.svelte'

  interface Props {
    /**
     * A placed card was clicked with click-add mode off. Nothing has moved: the caller decides what
     * looking at a card means, which here is lifting it into the spotlight.
     */
    onspotlight?: (source: DragSource) => void
    /**
     * Laid over the top of the pages, inside the box they are measured against, so it can size
     * itself off a pocket with var(--pocket). The card tray is what uses it.
     */
    overlay?: Snippet
  }

  let { onspotlight, overlay }: Props = $props()

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
   * bound is tighter. cqw and cqh measure the panel around this rather than the viewport, so
   * anything that changes the panel's size re-sizes the pages instead of overflowing them.
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

  /**
   * Takes focus off a toolbox button after it fires.
   *
   * The toolbox reveals on `group-hover` and on `group-focus-within`, which is right for keyboard
   * users -- Tab onto a button and the toolbox comes up. It is wrong after a click: the clicked
   * button keeps focus, so `group-focus-within` holds the toolbox open after the pointer has moved
   * away. Blurring on commit lets the opacity transition back out as soon as the hover is gone,
   * and a keyboard user who wants another action still has a Tab to the next button.
   */
  function fire(action: () => void): (event: MouseEvent) => void {
    return (event) => {
      action()

      if (event.currentTarget instanceof HTMLElement) event.currentTarget.blur()
    }
  }

  // Says what a click will actually do, which is the whole point of the mode being a mode.
  function slotLabel(index: number) {
    const card = binderPage.slots[index]

    if (!card) return `Empty pocket ${index + 1}`

    const missing = binderPage.isMissing(index) ? ' (missing)' : ''
    const placed = `${card.name ?? 'Card'} in pocket ${index + 1}${missing}.`

    if (spotlight.enabled) return `${placed} Click to take a closer look.`
    if (clickAdd.enabled) return `${placed} Click to send it back to the tray.`

    return placed
  }
</script>

<!--
  The binder as it falls open: page one alone, then facing pairs.

  container-type: size is what lets the pockets be measured against this panel in CSS rather than in
  a resize handler -- 100cqw and 100cqh below are its width and height.

  The clip is on the box around the pages, not this one or anything above it, so the overlay can
  reach out of the panel -- up to the tab bar, in the tray's case -- while the pages still cannot.
-->
<div class="relative size-full" style="container-type: size; --pocket: {pocketWidth};">
  {@render overlay?.()}

  <div class="flex size-full items-center justify-center overflow-hidden">
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
            {@const missing = slot.missing}
            {@const isTarget = over === index}
            <!-- A wrapper rather than the button alone, so the hover toolbox can sit beside the
                 pocket instead of inside it -- buttons cannot nest. group is what the toolbox's
                 group-hover/group-focus-within read; relative is the box its absolute overlay
                 covers. -->
            <div class="group relative">
              <!-- A button, so a pocket is reachable and operable without a drag: clicking a full
                   one sends its card back to the tray, or lifts it for a look with click-add mode
                   off. p-0 because a native button carries its own padding, which on a pocket this
                   size leaves the card floating in the middle of its own slot. dragover has to
                   preventDefault on every pass or the browser refuses the drop -- that is the
                   API's way of asking whether this element accepts it. -->
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
                  // Spotlight takes a click on a placed card for itself -- the toolbox is gone in
                  // this mode, so a click is never ambiguous. Edits are not blocked: the card can
                  // still be dragged, undone and so on.
                  if (spotlight.enabled) {
                    if (card) onspotlight?.({ kind: 'slot', card, index })

                    return
                  }

                  // Editing with click-add off, a plain click does nothing: the card's toolbox is
                  // how it is acted on.
                  if (clickAdd.enabled) binderPage.returnToTray(index)
                }}
              >
                {#if card}
                  <!-- Missing dims the art and desaturates it: a pocket flagged for a card the
                       collector does not own yet reads as a placeholder rather than the real thing.
                       The chip below says what the dimming is for. -->
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
                    class="size-full rounded-container object-contain {spotlight.enabled
                      ? 'cursor-zoom-in'
                      : 'cursor-grab active:cursor-grabbing'} {missing ? 'opacity-40 grayscale' : ''}"
                  />
                  {#if missing}
                    <!-- aria-hidden because the slot's own aria-label already says "(missing)";
                         the chip is for the eye, not the screen reader. The title is a tooltip
                         for the pointer: a chip saying "Missing" is cryptic until you know what
                         marking a pocket as missing means. Bottom-right so step 3's label chip can
                         have the bottom-left corner it is specced for. -->
                    <span
                      aria-hidden="true"
                      title="This pocket is held for a card the collector does not own yet"
                      class="badge preset-filled-warning-500 absolute right-1 bottom-1 text-xs font-semibold shadow"
                    >
                      Missing
                    </span>
                  {/if}
                {/if}
              </button>

              {#if card && !spotlight.enabled && !binderPage.dragged}
                <!-- The hover toolbox: edit actions for the card, revealed on hover and on
                     focus-within so a keyboard reaches it too. pointer-events-none on the
                     wrapper hands the gaps around the buttons back to the pocket beneath, which
                     is the drag handle and the click target. Hidden while any drag is in flight
                     -- the one on this card as well as one from somewhere else landing here --
                     so it never shows between a pointer-down and a drop.

                     content-start keeps it along the top of the card, out of the way of the
                     spotlight click target and leaving the art visible under it. No size class
                     on the glyphs: btn-icon-xl sizes its own svg from --btn-size, the same
                     reasoning as the tray tile buttons. -->
                <div
                  class="pointer-events-none absolute inset-x-0 top-0 flex flex-wrap items-start justify-center gap-1 p-1 opacity-0 transition-opacity group-focus-within:opacity-100 group-hover:opacity-100 motion-reduce:transition-none"
                >
                  <button
                    type="button"
                    class="btn-icon btn-icon-xl pointer-events-auto preset-filled-primary-500"
                    title="Move {card.name ?? 'card'} to the tray"
                    aria-label="Move {card.name ?? 'card'} to the tray"
                    onclick={fire(() => binderPage.returnToTray(index))}
                  >
                    <ShoppingBasketIcon />
                  </button>
                  <!-- One button that toggles: a missing pocket is one the collector has flagged
                       for a card they do not own yet, and pressing again says they have it after
                       all. The icon is the action, not the state -- eye-off while the card is
                       present (press to hide/mark), eye while it is missing (press to bring it
                       back) -- the look and label say which side of the toggle this press is on. -->
                  <button
                    type="button"
                    class="btn-icon btn-icon-xl pointer-events-auto {missing
                      ? 'preset-filled-warning-500'
                      : 'preset-filled-surface-100-900'}"
                    title={missing
                      ? `Unmark ${card.name ?? 'card'} as missing — the card is in the collection`
                      : `Mark ${card.name ?? 'card'} as missing — the pocket is held for a card not owned yet`}
                    aria-label={missing
                      ? `Unmark ${card.name ?? 'card'} as missing`
                      : `Mark ${card.name ?? 'card'} as missing`}
                    aria-pressed={missing}
                    onclick={fire(() => binderPage.toggleMissing(index))}
                  >
                    {#if missing}
                      <EyeIcon />
                    {:else}
                      <EyeOffIcon />
                    {/if}
                  </button>
                  <button
                    type="button"
                    class="btn-icon btn-icon-xl pointer-events-auto preset-filled-error-500"
                    title="Delete {card.name ?? 'card'} from the binder"
                    aria-label="Delete {card.name ?? 'card'} from the binder"
                    onclick={fire(() => binderPage.deleteFromBinder(index))}
                  >
                    <Trash2Icon />
                  </button>
                </div>
              {/if}
            </div>
          {/each}
        </div>
      {/each}
    </div>
  </div>
</div>
