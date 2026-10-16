<script lang="ts">
  import BetweenHorizontalStartIcon from '@lucide/svelte/icons/between-horizontal-start'
  import ChevronLeftIcon from '@lucide/svelte/icons/chevron-left'
  import ChevronRightIcon from '@lucide/svelte/icons/chevron-right'
  import Trash2Icon from '@lucide/svelte/icons/trash-2'

  import { tick } from 'svelte'

  import { CARD_BACK_URL } from '../../card-art'
  import type { CardSearchResult } from '../../clients/CardSearchClient'
  import { binderPage } from '../../stores/binder-page.svelte'
  import { clickAdd } from '../../stores/click-add.svelte'
  import { tray } from '../../stores/tray.svelte'

  /** How far an arrow press travels, as a share of the visible strip. */
  const SCROLL_STEP = 0.8

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

  // Re-measure whenever the strip could have changed shape: cards placed or removed while it is
  // open. Reading the count is what subscribes this to the tray.
  $effect(() => {
    tray.entries.length

    // tick(), not requestAnimationFrame: it resolves once Svelte has flushed the DOM change, which
    // is all this needs. A frame callback additionally waits for the page to paint, and a tab in
    // the background never does -- the arrows would come back wrong the moment the workspace was
    // opened in a tab the user was not looking at.
    void tick().then(syncEnds)
  })

  // That effect only fires on a change this component can see. Resizing the window changes how
  // much of the strip fits without changing anything in it, and a ResizeObserver is what notices.
  $effect(() => {
    if (!scroller) return

    const observer = new ResizeObserver(syncEnds)

    observer.observe(scroller)

    return () => observer.disconnect()
  })

  // Lit up only for a card coming off the page: a tray card dropped back on the tray it came from
  // has nothing to do, so promising otherwise would be a lie.
  let overTray = $state(false)

  const acceptsDrop = $derived(binderPage.isDraggingFromSlot)

  function onDragOverStrip(event: DragEvent): void {
    if (!acceptsDrop) return

    // Every pass, or the browser refuses the drop.
    event.preventDefault()

    overTray = true
  }

  // Whether the place action has anywhere to put a card. placeInFirstFreeSlot only ever looks at
  // the open spread, so a full spread means the button does nothing -- better to say so than to
  // offer a press that goes nowhere.
  const hasFreePocket = $derived(binderPage.visibleSlots.some((slot) => slot.card === null))

  function tileTitle(card: CardSearchResult) {
    const name = [card.name, card.setName].filter(Boolean).join(' · ')

    return clickAdd.enabled
      ? `${name} — drag onto a pocket, or click to use the first free one`
      : `${name} — drag onto a pocket`
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

  /**
   * Takes focus off a hover button after it fires. See BinderPage.svelte for why -- same
   * group-focus-within trick, same cure.
   */
  function fire(action: () => void): (event: MouseEvent) => void {
    return (event) => {
      action()

      if (event.currentTarget instanceof HTMLElement) event.currentTarget.blur()
    }
  }
</script>

<!--
  The tray as it appears while placing cards: a strip laid over the top of the pages, opened and
  closed from the tab bar. It takes no room from the binder at all -- whoever renders it decides
  when it is there, so while it is mounted it is open, and it has no header or fold of its own.

  One tile per card, not per copy — a row of four identical Goldeens says nothing a chip reading 4
  does not say in a quarter of the space. No tilt either: these are pieces waiting to be placed, and
  a tile that swung under the pointer would fight the drag it is about to be part of.
-->
<!-- The drop highlight is a ring, not a tint: the strip lies over the pages, and a see-through
     background would put the cards under it behind the ones in it. Same ring as the tab bar's tray
     button, which is the other place a card can be dropped back. -->
<!-- svelte-ignore a11y_no_static_element_interactions -->
<section
  class="rounded-container border border-surface-200-800/50 bg-surface-50-950/90 shadow-xl backdrop-blur-md {overTray
    ? 'ring-2 ring-primary-500'
    : ''}"
  ondragenter={onDragOverStrip}
  ondragover={onDragOverStrip}
  ondragleave={(event) => {
    // dragleave fires crossing between the tiles inside the strip as well as leaving it, and
    // relatedTarget is what tells the two apart: only a pointer that has left the strip
    // altogether puts the highlight out.
    if (event.currentTarget.contains(event.relatedTarget as Node | null)) return

    overTray = false
  }}
  ondrop={(event) => {
    event.preventDefault()
    overTray = false
    binderPage.dropOnTray()
  }}
>
  <div class="flex items-center gap-1 p-2">
    {#if tray.isEmpty}
      <!-- It only opens on a click, so an empty one is someone looking for cards that are not
           there, and where to get them is the useful answer. -->
      <p class="w-full py-4 text-center text-sm opacity-60">
        The tray is empty. Add cards from Card Search, or drop one here to take it off the page.
      </p>
    {:else}
      <!-- Same xl as the buttons on a tile, for the same reason and to keep the one row of
           controls reading as one size. No size class on the chevrons: btn-icon sizes its own svg
           -- see the tile overlay. -->
      <button
        type="button"
        class="btn-icon btn-icon-xl shrink-0 hover:preset-tonal"
        aria-label="Scroll the tray left"
        disabled={atStart}
        onclick={() => scrollBy(-1)}
      >
        <ChevronLeftIcon />
      </button>

      <!-- scrollbar-none: the arrows are the control, and a scrollbar under the row is height the
           strip would be laying over the pages for nothing. -->
      <ul
        bind:this={scroller}
        onscroll={syncEnds}
        class="flex min-w-0 flex-1 gap-2 overflow-x-auto scroll-smooth p-1 [scrollbar-width:none]"
      >
        {#each tray.entries as entry (entry.card.id)}
          {@const card = entry.card}
          <!-- Sized off a pocket, not off the strip: 0.6 of one, so the strip covers a little over
               half of the top row and leaves the rest of each pocket there to drop on. --pocket is
               BinderPage's, inherited because the strip is rendered inside it. -->
          <li class="group relative w-[calc(var(--pocket)*0.6)] shrink-0">
            <!-- A button as well as a drag source: with click-add mode on, clicking drops the
                 card into the first free pocket, which is the same transfer without needing a
                 mouse to complete it. With the mode off a click does nothing, and the transfer
                 moves to the buttons below. A tile is never lifted into the spotlight: that is
                 Spotlight mode's, and only for placed cards -- tray tiles are not looked at,
                 they are placed. -->
            <button
              type="button"
              class="block w-full cursor-grab active:cursor-grabbing"
              title={tileTitle(card)}
              aria-label={clickAdd.enabled
                ? `Place ${card.name ?? 'card'} in the first free pocket`
                : (card.name ?? 'Card')}
              draggable="true"
              ondragstart={(event) => {
                // Firefox will not start a drag without something on the DataTransfer, even
                // though the payload this app reads is the module's own state.
                event.dataTransfer?.setData('text/plain', String(card.id))
                binderPage.startDrag({ kind: 'tray', card })
              }}
              ondragend={() => binderPage.endDrag()}
              onclick={() => {
                if (clickAdd.enabled) binderPage.placeInFirstFreeSlot(card)
              }}
            >
              <!-- The card's own ratio gives every tile its height before lazy art has loaded, so
                   the strip slides open at its full height rather than growing as the art lands. -->
              <img
                src={card.imageUrl ?? CARD_BACK_URL}
                alt={card.imageUrl ? (card.name ?? 'Card') : 'No image available'}
                loading="lazy"
                class="aspect-[719/1000] w-full rounded bg-surface-200-800/40 object-contain"
              />
            </button>

            {#if !clickAdd.enabled}
              <!-- What the click used to do, said out loud instead: the two transfers a tile has,
                   as buttons you have to aim at. Revealed on hover and on focus-within both --
                   the second is what keeps them off the mouse, since a button at opacity 0 is
                   still in the tab order and tabbing to one brings the pair into view.

                   pointer-events-none on the wrapper so the gaps around the buttons still belong
                   to the tile underneath, which is the drag handle.

                   Sized xl rather than sm: at 26px these were smaller than the art you aim past
                   to reach them. The pair takes 80px, and a tile follows its binder's pockets, so
                   on a binder with small pockets it can be narrower than that: the row is allowed
                   to wrap rather than squash -- flex would otherwise shave the width off two
                   buttons whose height is fixed and leave a pair of oblongs -- and wrapped, the two
                   stack onto the art instead.

                   No size class on these two glyphs, unlike every other icon here: btn-icon
                   already sizes its own svg from --btn-size, and this theme's spacing step is
                   4.48px, so the nearest size-* lands 2px off and sets the height fighting a
                   width the button has already set. Left alone it is square and follows the
                   button. -->
              <div
                class="pointer-events-none absolute inset-0 flex flex-wrap content-end items-end justify-center gap-1 rounded bg-surface-50-950/70 p-1 opacity-0 transition-opacity group-focus-within:opacity-100 group-hover:opacity-100 motion-reduce:transition-none"
              >
                <button
                  type="button"
                  class="btn-icon btn-icon-xl pointer-events-auto preset-filled-primary-500"
                  title={hasFreePocket
                    ? `Place ${card.name ?? 'card'} in the first free pocket`
                    : 'Every pocket on this spread is full'}
                  aria-label="Place {card.name ?? 'card'} in the first free pocket"
                  disabled={!hasFreePocket}
                  onclick={fire(() => binderPage.placeInFirstFreeSlot(card))}
                >
                  <BetweenHorizontalStartIcon />
                </button>
                <button
                  type="button"
                  class="btn-icon btn-icon-xl pointer-events-auto preset-filled-error-500"
                  title={removeLabel(entry)}
                  aria-label={removeLabel(entry)}
                  onclick={fire(() => tray.remove(card.id))}
                >
                  <Trash2Icon />
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
        class="btn-icon btn-icon-xl shrink-0 hover:preset-tonal"
        aria-label="Scroll the tray right"
        disabled={atEnd}
        onclick={() => scrollBy(1)}
      >
        <ChevronRightIcon />
      </button>
    {/if}
  </div>
</section>
