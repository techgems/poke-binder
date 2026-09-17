<!--
  Lifts one card to the front of whatever it sits in, big enough to study and tilt, with a row of
  actions under it.

  Deliberately not a dialog. It covers its nearest positioned ancestor rather than the viewport, so
  the host decides how much of the page gets taken over — give that container `relative` and this
  fills exactly it. That keeps it usable inside a panel, a dialog, or a page without ever stacking a
  modal on a modal, which is the thing that makes a card preview inside a search unpleasant.

  Knows nothing about cards beyond a URL: the actions are a snippet, so what you can do with the
  lifted card belongs to the caller.
-->
<script lang="ts">
  import type { HoverTiltProps } from 'hover-tilt'
  import type { Snippet } from 'svelte'

  import TiltCard from './TiltCard.svelte'

  interface Props extends HoverTiltProps {
    /**
     * Whether the card is lifted. Controlled by the caller: this component never writes it, it only
     * asks to be closed through `onclose`. Making it bindable instead would let a caller pass a
     * derived expression — `open={selected !== null}` — while the component also assigned to it,
     * and the two sources then fight over which one is true.
     */
    open?: boolean
    /** Image to show on the card face. Ignored when `card` is given. */
    src?: string
    /** Alt text for that image. */
    alt?: string
    /** Accessible name for the overlay itself. */
    label?: string
    /** CSS aspect-ratio for the card box. */
    aspect?: string
    /**
     * Turns the tilt off while still lifting the card. A card that stays flat when every other one
     * moves reads as "there is nothing here to look at", which is worth saying out loud for a
     * placeholder face rather than quietly shining it up like real art.
     */
    tilt?: boolean
    /** Classes for the lifted card — size it here. */
    cardClass?: string
    /** Buttons shown under the card. */
    actions?: Snippet
    /** Anything worth reading beside the actions: name, set, price. */
    details?: Snippet
    /** Replaces the card face entirely. */
    card?: Snippet
    /**
     * Asked to close — by Escape, by the backdrop, or by an action. The caller decides what that
     * means, which is what keeps this component out of the business of owning the selection.
     */
    onclose?: () => void
  }

  let {
    open = false,
    src,
    alt = '',
    label = 'Card preview',
    aspect = '719 / 1000',
    tilt = true,
    cardClass = 'h-full max-h-full',
    actions,
    details,
    card,
    onclose,
    class: classes = '',
    // hover-tilt tuning, forwarded to the lifted card. Glare and shadow lean heavier than a grid
    // tile because this one is meant to be looked at — but the geometry stays modest. scaleFactor
    // is 1 because a lifted card already fills the space it is given, and tiltFactor is under 1
    // because rotation is what swings the corners down over the title: the card is several hundred
    // pixels tall here, so every degree costs far more travel at the edge than it does on a tile.
    tiltFactor = 0.6,
    scaleFactor = 1,
    glareIntensity = 1.5,
    shadow = true,
    ...tiltProps
  }: Props = $props()

  let panel = $state<HTMLElement | null>(null)

  // Where focus was before the card came up, so it can be handed back on the way out. Plain, not
  // $state: nothing renders it.
  let restoreTo: HTMLElement | null = null

  $effect(() => {
    if (!open) return

    restoreTo = document.activeElement as HTMLElement | null
    panel?.focus()

    return () => {
      restoreTo?.focus?.()
      restoreTo = null
    }
  })

  function close() {
    onclose?.()
  }

  const FOCUSABLE =
    'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'

  function handleKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape') {
      // Kept from reaching anything above: an enclosing dismissable layer would otherwise read this
      // as its own Escape and close out from under the card.
      event.stopPropagation()
      event.preventDefault()

      close()

      return
    }

    if (event.key !== 'Tab' || !panel) return

    // Focus stays inside while the card is up. Without this, tabbing walks off into the results
    // behind the scrim, which are visually covered but still perfectly focusable.
    const items = [...panel.querySelectorAll<HTMLElement>(FOCUSABLE)].filter(
      (item) => item.offsetParent !== null,
    )

    if (items.length === 0) {
      event.preventDefault()

      return
    }

    const first = items[0]
    const last = items[items.length - 1]
    const active = document.activeElement

    if (event.shiftKey && (active === first || active === panel)) {
      event.preventDefault()
      last.focus()
    } else if (!event.shiftKey && active === last) {
      event.preventDefault()
      first.focus()
    }
  }
</script>

<!--
  The entrance is a CSS transition off @starting-style, not svelte/transition. A `transition:fade`
  here played its outro but then left the element in the tree at opacity 0 — invisible, still
  absolutely positioned over the whole workspace, and still swallowing every click. Modal.svelte
  animates the same way for the same reason. The trade is that dismissal is instant rather than
  faded, which for a preview reads as responsive.
-->
{#if open}
  <!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
  <div
    bind:this={panel}
    role="dialog"
    aria-label={label}
    tabindex="-1"
    onkeydown={handleKeydown}
    class="absolute inset-0 z-30 opacity-100 transition-opacity duration-200 focus:outline-none motion-reduce:transition-none starting:opacity-0 {classes}"
  >
    <button
      type="button"
      aria-label="Close preview"
      class="absolute inset-0 cursor-default bg-surface-50-950/70 backdrop-blur-sm"
      onclick={close}
    ></button>

    <!-- Clicks in the empty space around the card fall through to the backdrop above; only the card
         and the actions take pointer events back. -->
    <div
      class="pointer-events-none relative flex h-full flex-col items-center justify-center gap-6 p-6"
    >
      <!-- The reserve that keeps the card's layout box clear of the rows below it. Tilt is a
           transform, so the card paints outside that box without ever pushing the details or the
           actions down — a card sized to fill this area edge to edge swings its corners straight
           over them. The rotation also carries a drop shadow past the corner it lifts, so the
           reserve has to cover more than the geometry alone suggests. -->
      <div class="flex min-h-0 w-full flex-1 items-center justify-center py-8">
        <div
          class="pointer-events-auto h-full scale-100 transition-transform duration-200 motion-reduce:transition-none starting:scale-95"
        >
          {#if card}
            {@render card()}
          {:else}
            <TiltCard
              {src}
              {alt}
              {aspect}
              {tilt}
              {tiltFactor}
              {scaleFactor}
              {glareIntensity}
              {shadow}
              {...tiltProps}
              loading="eager"
              class="rounded-container {cardClass}"
            />
          {/if}
        </div>
      </div>

      {#if details}
        <div class="pointer-events-auto shrink-0 text-center">
          {@render details()}
        </div>
      {/if}

      {#if actions}
        <div class="pointer-events-auto flex shrink-0 flex-wrap items-center justify-center gap-2">
          {@render actions()}
        </div>
      {/if}
    </div>
  </div>
{/if}
