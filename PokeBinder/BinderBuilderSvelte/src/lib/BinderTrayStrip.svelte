<script lang="ts">
  import BetweenHorizontalStartIcon from '@lucide/svelte/icons/between-horizontal-start'
  import ChevronDownIcon from '@lucide/svelte/icons/chevron-down'
  import ChevronLeftIcon from '@lucide/svelte/icons/chevron-left'
  import ChevronRightIcon from '@lucide/svelte/icons/chevron-right'
  import Trash2Icon from '@lucide/svelte/icons/trash-2'

  import { tick } from 'svelte'
  import { cubicInOut } from 'svelte/easing'
  import { slide } from 'svelte/transition'

  import type { CardSearchResult } from '../clients/CardSearchClient'
  import { binderPage, type DragSource } from './binder-page.svelte'
  import { clickAdd } from './click-add.svelte'
  import { prefersReducedMotion } from './tilt/prefers-reduced-motion.svelte'
  import { tray } from './tray.svelte'

  interface Props {
    /** A tile was clicked with click-add mode off, which lifts the card rather than moving it. */
    onspotlight?: (source: DragSource) => void
  }

  let { onspotlight }: Props = $props()

  /** Stand-in art for cards the catalog has no image for. */
  const CARD_BACK_URL = '/images/TcgImages/card-back.png'

  /** How far an arrow press travels, as a share of the visible strip. */
  const SCROLL_STEP = 0.8

  let collapsed = $state(false)

  let scroller = $state<HTMLElement | null>(null)

  // Whether either end has been reached, which is the only thing the arrows need to know. Held as
  // state rather than read during render because scrolling does not invalidate anything on its own.
  let atStart = $state(true)
  let atEnd = $state(true)

  function syncEnds() {
    if (!scroller) return

    // A pixel of slack: fractional scroll positions never land exactly on the end.
    atStart = scroller.scrollLeft <= 1
    atEnd = scroller.scrollLeft + scroller.clientWidth >= scroller.scrollWidth - 1
  }

  // Re-measure whenever the strip could have changed shape: cards added or removed, or the panel
  // opened back up. Reading the count is what subscribes this to the tray.
  $effect(() => {
    tray.entries.length
    collapsed

    // tick(), not requestAnimationFrame: it resolves once Svelte has flushed the DOM change, which
    // is all this needs. A frame callback additionally waits for the page to paint, and a tab in
    // the background never does -- the arrows would come back wrong the moment the workspace was
    // opened in a tab the user was not looking at.
    void tick().then(syncEnds)
  })

  // Long enough to read as the panel folding rather than vanishing, short enough not to be in the
  // way of someone toggling it to get at the page. Zero when the OS asks for less motion, which
  // keeps the same markup and just removes the travel.
  const slideOptions = $derived({
    duration: prefersReducedMotion() ? 0 : 200,
    easing: cubicInOut,
  })

  // Lit up only for a card coming off the page: a tray card dropped back on the tray it came from
  // has nothing to do, so promising otherwise would be a lie.
  let overTray = $state(false)

  const acceptsDrop = $derived(binderPage.isDraggingFromSlot)

  // Whether the place action has anywhere to put a card. placeInFirstFreeSlot only ever looks at
  // the open spread, so a full spread means the button does nothing -- better to say so than to
  // offer a press that goes nowhere.
  const hasFreePocket = $derived(binderPage.visibleSlots.some((slot) => slot.card === null))

  function tileTitle(card: CardSearchResult) {
    const name = [card.name, card.setName].filter(Boolean).join(' · ')

    return clickAdd.enabled
      ? `${name} — drag onto a pocket, or click to use the first free one`
      : `${name} — drag onto a pocket, or click to take a closer look`
  }

  // Removing takes the whole tile with it, copies and all, so the label counts them rather than
  // letting one press quietly discard four cards.
  function removeLabel(entry: { card: CardSearchResult; quantity: number }) {
    const name = entry.card.name ?? 'card'

    return entry.quantity > 1
      ? `Remove all ${entry.quantity} copies of ${name} from the tray`
      : `Remove ${name} from the tray`
  }

  function scrollBy(direction: -1 | 1) {
    if (!scroller) return

    scroller.scrollBy({ left: direction * scroller.clientWidth * SCROLL_STEP, behavior: 'smooth' })
  }
</script>

<!--
  The tray as it appears while placing cards: a strip along the foot of the binder rather than a
  column beside it, because the page above is the thing that needs the room, and it collapses
  entirely when even that is too much.

  One tile per card, not per copy — a row of four identical Goldeens says nothing a chip reading 4
  does not say in a quarter of the space. No tilt either: these are pieces waiting to be placed, and
  a tile that swung under the pointer would fight the drag it is about to be part of.
-->
<section class="shrink-0 rounded-container border border-surface-200-800/50">
  <!-- svelte-ignore a11y_click_events_have_key_events -->
  <!-- svelte-ignore a11y_no_static_element_interactions -->
  <header
    class="flex cursor-pointer items-center justify-between gap-2 px-3 py-2 select-none"
    onclick={() => (collapsed = !collapsed)}
  >
    <h3 class="text-sm font-semibold">
      Cards in Tray
      <span class="opacity-60">({tray.totalQuantity})</span>
    </h3>

    <button type="button" class="btn btn-sm hover:preset-tonal" aria-expanded={!collapsed}>
      <span>{collapsed ? 'Show' : 'Hide'}</span>
      <ChevronDownIcon class="size-4 transition-transform {collapsed ? 'rotate-180' : ''}" />
    </button>
  </header>

  {#if !collapsed}
    <!-- svelte-ignore a11y_no_static_element_interactions -->
    <div
      transition:slide={slideOptions}
      ondragover={(event) => {
        if (!acceptsDrop) return

        // Every pass, or the browser refuses the drop.
        event.preventDefault()
        overTray = true
      }}
      ondragleave={() => (overTray = false)}
      ondrop={(event) => {
        event.preventDefault()
        overTray = false
        binderPage.dropOnTray()
      }}
      class="flex items-center gap-1 border-t border-surface-200-800/50 p-2 transition-colors {overTray
        ? 'bg-primary-500/15'
        : ''}"
    >
      {#if tray.isEmpty}
        <p class="w-full py-4 text-center text-sm opacity-60">
          {acceptsDrop
            ? 'Drop the card here to take it off the page.'
            : 'Cards you add in the Card Tray tab wait here. Drag one onto a pocket to place it.'}
        </p>
      {:else}
        <button
          type="button"
          class="btn-icon btn-icon-sm shrink-0 hover:preset-tonal"
          aria-label="Scroll the tray left"
          disabled={atStart}
          onclick={() => scrollBy(-1)}
        >
          <ChevronLeftIcon class="size-4" />
        </button>

        <!-- scrollbar-none: the arrows are the control, and a scrollbar under a single row of small
             tiles costs more height than the tiles can spare. -->
        <ul
          bind:this={scroller}
          onscroll={syncEnds}
          class="flex min-w-0 flex-1 gap-2 overflow-x-auto scroll-smooth p-1 [scrollbar-width:none]"
        >
          {#each tray.entries as entry (entry.card.id)}
            {@const card = entry.card}
            <!-- A seventh of the strip each, so seven cards fill it and the eighth is what starts
                 the overflow the arrows are for. The gap subtracted is six of them, one between
                 each pair.

                 Both bounds matter more than the seven does. Below the floor a card is too small to
                 recognise, so a narrow window keeps the size and shows fewer -- which is the right
                 way round. Above the ceiling the strip would go on growing with the window: a
                 seventh of 1920px is a 232px card, and a row of those is a third of the workspace
                 tall, stealing from the page the tray exists to serve. Past that width the cards
                 hold still and an eighth simply fits. -->
            <li
              class="group relative shrink-0 grow-0 basis-[calc((100%-3.36rem)/7)] min-w-20 max-w-32"
            >
              <!-- A button as well as a drag source: with click-add mode on, clicking drops the
                   card into the first free pocket, which is the same transfer without needing a
                   mouse to complete it. With the mode off it lifts the card instead, and the
                   transfer moves to the overlay below. -->
              <button
                type="button"
                class="block w-full cursor-grab active:cursor-grabbing"
                title={tileTitle(card)}
                aria-label={clickAdd.enabled
                  ? `Place ${card.name ?? 'card'} in the first free pocket`
                  : `Take a closer look at ${card.name ?? 'card'}`}
                draggable="true"
                ondragstart={(event) => {
                  // Firefox will not start a drag without something on the DataTransfer, even
                  // though the payload this app reads is the module's own state.
                  event.dataTransfer?.setData('text/plain', String(card.id))
                  binderPage.startDrag({ kind: 'tray', card })
                }}
                ondragend={() => binderPage.endDrag()}
                onclick={() => {
                  if (clickAdd.enabled) {
                    binderPage.placeInFirstFreeSlot(card)

                    return
                  }

                  onspotlight?.({ kind: 'tray', card })
                }}
              >
                <img
                  src={card.imageUrl ?? CARD_BACK_URL}
                  alt={card.imageUrl ? (card.name ?? 'Card') : 'No image available'}
                  loading="lazy"
                  class="w-full rounded bg-surface-200-800/40"
                />
              </button>

              {#if !clickAdd.enabled}
                <!-- What the click used to do, said out loud instead: the two transfers a tile has,
                     as buttons you have to aim at. Revealed on hover and on focus-within both --
                     the second is what keeps them off the mouse, since a button at opacity 0 is
                     still in the tab order and tabbing to one brings the pair into view. A touch
                     screen has no hover at all, and reaches the same two actions under the lifted
                     card instead.

                     pointer-events-none on the wrapper so the gaps around the buttons still belong
                     to the tile underneath, which is the drag handle. -->
                <div
                  class="pointer-events-none absolute inset-0 flex items-end justify-center gap-1 rounded bg-surface-50-950/70 p-1 opacity-0 transition-opacity group-focus-within:opacity-100 group-hover:opacity-100 motion-reduce:transition-none"
                >
                  <button
                    type="button"
                    class="btn-icon btn-icon-sm pointer-events-auto preset-filled-primary-500"
                    title={hasFreePocket
                      ? `Place ${card.name ?? 'card'} in the first free pocket`
                      : 'Every pocket on this spread is full'}
                    aria-label="Place {card.name ?? 'card'} in the first free pocket"
                    disabled={!hasFreePocket}
                    onclick={() => binderPage.placeInFirstFreeSlot(card)}
                  >
                    <BetweenHorizontalStartIcon class="size-4" />
                  </button>
                  <button
                    type="button"
                    class="btn-icon btn-icon-sm pointer-events-auto preset-filled-error-500"
                    title={removeLabel(entry)}
                    aria-label={removeLabel(entry)}
                    onclick={() => tray.remove(card.id)}
                  >
                    <Trash2Icon class="size-4" />
                  </button>
                </div>
              {/if}

              <!-- How many of this card are still waiting, sitting on the art so the strip stays one
                   tile tall. -->
              <span
                class="badge-icon preset-filled-primary-500 absolute -top-1 -right-1 text-xs font-semibold shadow"
                aria-label="{entry.quantity} left in the tray"
              >
                {entry.quantity}
              </span>
            </li>
          {/each}
        </ul>

        <button
          type="button"
          class="btn-icon btn-icon-sm shrink-0 hover:preset-tonal"
          aria-label="Scroll the tray right"
          disabled={atEnd}
          onclick={() => scrollBy(1)}
        >
          <ChevronRightIcon class="size-4" />
        </button>
      {/if}
    </div>
  {/if}
</section>
