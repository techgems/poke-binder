<!--
  The binder as it falls open, with the tray along its foot. Cards move between the two by drag: out
  of the tray into a pocket, back out of a pocket into the tray, and between pockets. What is still
  missing is the saving: nothing here is written back yet.
-->
<script lang="ts">
  import BetweenHorizontalStartIcon from '@lucide/svelte/icons/between-horizontal-start'
  import CornerUpLeftIcon from '@lucide/svelte/icons/corner-up-left'
  import Trash2Icon from '@lucide/svelte/icons/trash-2'

  import BinderPage from './BinderPage.svelte'
  import BinderPageNav from './BinderPageNav.svelte'
  import BinderTrayStrip from './BinderTrayStrip.svelte'
  import { binderPage, type DragSource } from './binder-page.svelte'
  import CardSpotlight from './tilt/CardSpotlight.svelte'
  import { tray } from './tray.svelte'

  /** Stand-in art for cards the catalog has no image for. */
  const CARD_BACK_URL = '/images/TcgImages/card-back.png'

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

<!-- A column, not two: the page is what the eye is on here, so the tray gives up the side of the
     screen and takes a strip along the bottom that it can also fold away.

     relative because the spotlight below covers its nearest positioned ancestor rather than the
     viewport: this is the box it is meant to take over, page and tray strip together. -->
<div class="relative flex min-h-0 flex-1 flex-col gap-3">
  <!-- No frame and no header. A border, its padding and a "Page 1" caption cost three bands of
       height across the top of the panel, and every one of them came out of the page — which is
       the only thing in this view worth showing. The tray strip below keeps its border, so the two
       are still read apart. -->
  <section class="flex min-h-0 flex-1 flex-col gap-2 overflow-hidden">
    <div class="min-h-0 flex-1">
      <BinderPage onspotlight={(source) => (spotlit = source)} />
    </div>

    <BinderPageNav />
  </section>

  <BinderTrayStrip onspotlight={(source) => (spotlit = source)} />

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
