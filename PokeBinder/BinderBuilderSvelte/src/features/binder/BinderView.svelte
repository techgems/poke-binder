<!--
  The binder as it falls open, with the tray laid over the top of the pages when it is open. Cards
  move between the two by drag: out of the tray into a pocket, back out of a pocket into the tray,
  and between pockets. What is still missing is the saving: nothing here is written back yet.
-->
<script lang="ts">
  import BetweenHorizontalStartIcon from '@lucide/svelte/icons/between-horizontal-start'
  import CornerUpLeftIcon from '@lucide/svelte/icons/corner-up-left'
  import Trash2Icon from '@lucide/svelte/icons/trash-2'

  import { cubicInOut } from 'svelte/easing'
  import { slide } from 'svelte/transition'

  import { CARD_BACK_URL } from '../../card-art'
  import BinderPage from './BinderPage.svelte'
  import BinderPageNav from './BinderPageNav.svelte'
  import BinderTrayStrip from './BinderTrayStrip.svelte'
  import { binderPage, type DragSource } from '../../stores/binder-page.svelte'
  import CardSpotlight from '../../components/tilt/CardSpotlight.svelte'
  import { prefersReducedMotion } from '../../components/tilt/prefers-reduced-motion.svelte'
  import { tray } from '../../stores/tray.svelte'

  interface Props {
    /**
     * Whether the tray is showing. The button that opens it is up in the tab bar rather than
     * anywhere in this view.
     */
    trayOpen?: boolean
    /** A click landed outside the open tray, which closes it. */
    onclosetray?: () => void
  }

  let { trayOpen = false, onclosetray }: Props = $props()

  let overlayTray = $state<HTMLElement | null>(null)

  /**
   * The tray is only ever open on purpose, so it shuts again on the first click anywhere else.
   *
   * A click rather than a pointerdown, because a drag never fires one: dragging a card off a pocket
   * onto the open tray starts outside it, and must not close the target on the way. Captured, so it
   * runs before whatever the click is for -- by the time a spotlight button had handled it, the
   * spotlight would already be shut and the click would look like any other.
   *
   * Three clicks are not "outside": one on the tray, one on the tab bar's toggle (which would close
   * it here and open it again there), and any while a card is lifted -- the spotlight hides the
   * tray rather than replacing it, so closing the lifted card should bring the tray back, not end
   * it.
   */
  function closeTrayOnOutsideClick(event: MouseEvent) {
    if (!trayOpen || spotlit) return

    const target = event.target

    if (!(target instanceof Element)) return
    if (overlayTray?.contains(target) || target.closest('[data-tray-toggle]')) return

    onclosetray?.()
  }

  const overlaySlide = $derived({ duration: prefersReducedMotion() ? 0 : 200, easing: cubicInOut })

  // The card being looked at, and which end of the transfer it sits at. A card alone would not be
  // enough: "return this to the tray" and "put this on a page" are the same button in two places,
  // and only the source says which one is on offer.
  let spotlit = $state<DragSource | null>(null)

  const hasFreePocket = $derived(binderPage.visibleSlots.some((slot) => slot.card === null))

  // Every action here moves the card somewhere else, which leaves the lifted copy showing a card
  // that is no longer where the overlay says it is. Closing is the honest end to all of them.
  function returnToTray() {
    if (spotlit?.kind === 'slot') binderPage.returnToTray(spotlit.index)

    spotlit = null
  }

  function placeInFirstFreePocket() {
    if (spotlit) binderPage.placeInFirstFreeSlot(spotlit.card)

    spotlit = null
  }

  function removeFromTray() {
    if (spotlit) tray.remove(spotlit.card.id)

    spotlit = null
  }
</script>

<svelte:document onclickcapture={closeTrayOnOutsideClick} />

<!-- The page is what the eye is on here, so the tray takes no edge of it: it lies over the pages
     while it is open, and the binder keeps the whole tab.

     relative because the spotlight below covers its nearest positioned ancestor rather than the
     viewport: this is the box it is meant to take over. -->
<div class="relative flex min-h-0 flex-1 flex-col">
  <!-- No frame and no header. A border, its padding and a "Page 1" caption cost three bands of
       height across the top of the panel, and every one of them came out of the page — which is
       the only thing in this view worth showing.

       No clip here: BinderPage clips its own pages, and leaves the overlay free to reach past
       this box. -->
  <section class="flex min-h-0 min-w-0 flex-1 flex-col gap-2">
    <div class="min-h-0 flex-1">
      <BinderPage onspotlight={(source) => (spotlit = source)}>
        {#snippet overlay()}
          <!-- The full width of the tab, hung straight off the tab bar's underline: -top-8 climbs
               back over the tab list's mb-4 and the panel's gap-4 below it (Skeleton's tabs.css
               and WorkspacePanel), which would otherwise be a band of nothing between the two.

               Under the spotlight (z-30), but the spotlight only covers this view, and the 8 that
               climbs out of it would show above the lifted card -- so the strip is hidden while a
               card is lifted. invisible rather than unmounted, so it comes back where it was
               scrolled to. -->
          {#if trayOpen}
            <div
              bind:this={overlayTray}
              class="absolute inset-x-0 -top-8 z-10 {spotlit ? 'invisible' : ''}"
              transition:slide={overlaySlide}
            >
              <BinderTrayStrip onspotlight={(source) => (spotlit = source)} />
            </div>
          {/if}
        {/snippet}
      </BinderPage>
    </div>

    <BinderPageNav />
  </section>

  <!-- What a click does with click-add mode off. The moves it replaces are all here as buttons, so
       nothing the mode takes away goes missing — it just stops happening by accident, and stays
       reachable from the keyboard and from a touch screen, neither of which has a hover overlay. -->
  <CardSpotlight
    open={spotlit !== null}
    src={spotlit?.card.imageUrl ?? CARD_BACK_URL}
    alt={spotlit?.card.imageUrl ? (spotlit.card.name ?? 'Card') : 'No image available'}
    label={spotlit?.card.name ? `${spotlit.card.name} preview` : 'Card preview'}
    tilt={spotlit?.card.imageUrl != null}
    onclose={() => (spotlit = null)}
  >
    {#snippet details()}
      <p class="font-semibold">{spotlit?.card.name}</p>
      <p class="text-sm opacity-60">
        {[spotlit?.card.setName, spotlit?.card.rarity, spotlit?.card.cardNumber]
          .filter(Boolean)
          .join(' · ')}
      </p>
    {/snippet}

    {#snippet actions()}
      {#if spotlit?.kind === 'slot'}
        <button
          type="button"
          class="btn btn-lg preset-filled-primary-500 min-w-64 shadow-lg shadow-primary-500/30"
          onclick={returnToTray}
        >
          <CornerUpLeftIcon class="size-5" />
          <span>Send back to the tray</span>
        </button>
      {:else if spotlit}
        <button
          type="button"
          class="btn btn-lg preset-filled-primary-500 min-w-64 shadow-lg shadow-primary-500/30"
          disabled={!hasFreePocket}
          onclick={placeInFirstFreePocket}
        >
          <BetweenHorizontalStartIcon class="size-5" />
          <span>
            {hasFreePocket
              ? 'Place in the first free pocket'
              : 'Every pocket on this spread is full'}
          </span>
        </button>
        <button type="button" class="btn btn-lg preset-tonal-error" onclick={removeFromTray}>
          <Trash2Icon class="size-5" />
          <span>
            {spotlit && tray.quantityOf(spotlit.card.id) > 1
              ? `Remove all ${tray.quantityOf(spotlit.card.id)} from the tray`
              : 'Remove from the tray'}
          </span>
        </button>
      {/if}

      <button type="button" class="btn btn-lg preset-tonal" onclick={() => (spotlit = null)}>
        Close
      </button>
    {/snippet}
  </CardSpotlight>
</div>
