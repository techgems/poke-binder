<!--
  The binder as it falls open, with the tray laid over the top of the pages when it is open. Cards
  move between the two by drag: out of the tray into a pocket, back out of a pocket into the tray,
  and between pockets. What is still missing is the saving: nothing here is written back yet.
-->
<script lang="ts">

  import { cubicInOut } from 'svelte/easing'
  import { slide } from 'svelte/transition'

  import { CARD_BACK_URL } from '../../card-art'
  import BinderPage from './BinderPage.svelte'
  import BinderPageNav from './BinderPageNav.svelte'
  import BinderTrayStrip from './BinderTrayStrip.svelte'
  import { binderPage, type DragSource } from '../../stores/binder-page.svelte'
  import CardSpotlight from '../../components/tilt/CardSpotlight.svelte'
  import { prefersReducedMotion } from '../../components/tilt/prefers-reduced-motion.svelte'

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

  // The placed card being looked at. Only ever set in Spotlight mode -- a click on a placed card
  // there lifts it instead of acting on it; nothing else opens the spotlight.
  let spotlit = $state<DragSource | null>(null)

  // Whether the lifted pocket is flagged missing, read live from the store so the dim and the chip
  // update if the slot changes under the spotlight (e.g. an undo).
  const spotlitMissing = $derived(
    spotlit?.kind === 'slot' && binderPage.isMissing(spotlit.index),
  )
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
              <BinderTrayStrip />
            </div>
          {/if}
        {/snippet}
      </BinderPage>
    </div>

    <BinderPageNav />
  </section>

  <!-- What a click on a placed card does in Spotlight mode, and the only place a card is lifted
       at all. It is for looking, so it moves nothing: Close is its one action. The hover toolbox
       is how a card is acted on, which is off in this mode by design. -->
  <CardSpotlight
    open={spotlit !== null}
    src={spotlit?.card.imageUrl ?? CARD_BACK_URL}
    alt={spotlit?.card.imageUrl ? (spotlit.card.name ?? 'Card') : 'No image available'}
    label={spotlit?.card.name ? `${spotlit.card.name} preview` : 'Card preview'}
    tilt={spotlit?.card.imageUrl != null && !spotlitMissing}
    cardClass="h-full max-h-full {spotlitMissing ? 'opacity-40 grayscale' : ''}"
    onclose={() => (spotlit = null)}
  >
    {#snippet details()}
      {#if spotlitMissing}
        <span
          title="This pocket is held for a card the collector does not own yet"
          class="badge preset-filled-warning-500 mb-2 text-xs font-semibold shadow"
        >
          Missing
        </span>
      {/if}
      <p class="font-semibold">{spotlit?.card.name}</p>
      <p class="text-sm opacity-60">
        {[spotlit?.card.setName, spotlit?.card.rarity, spotlit?.card.cardNumber]
          .filter(Boolean)
          .join(' · ')}
      </p>
    {/snippet}

    {#snippet actions()}
      <button type="button" class="btn btn-lg preset-tonal" onclick={() => (spotlit = null)}>
        Close
      </button>
    {/snippet}
  </CardSpotlight>
</div>
