<!--
  A card-shaped image that tilts and glares under the pointer.

  Built to stand in for a plain `<img>`: give it the same src/alt and the same classes you would
  have put on the image, and the tilt is the only thing that changes. It knows nothing about card
  search, binders, or any particular payload — pass it a URL, or a snippet if the face needs to be
  something richer than one image.

  Radius is inherited rather than set: put `rounded-*` on this component's `class` and the tilt
  layer, the glare and the image all pick it up, so there is one place to change it.
-->
<script lang="ts">
  import type { HoverTiltProps } from 'hover-tilt'
  import { HoverTilt } from 'hover-tilt'
  import type { Snippet } from 'svelte'

  import { prefersReducedMotion } from './prefers-reduced-motion.svelte'

  interface Props extends HoverTiltProps {
    /** Image to show on the card face. Ignored when `children` is given. */
    src?: string
    /** Alt text for that image. Empty marks it decorative, which is right if a caption repeats it. */
    alt?: string
    /** Matches the `<img>` attribute; `lazy` is worth keeping in long grids. */
    loading?: 'eager' | 'lazy'
    /**
     * CSS aspect-ratio for the card box, as an `aspect-ratio` value. Defaults to the proportions of
     * a real trading card, which is what stops a grid of these jumping around while art loads.
     */
    aspect?: string
    /**
     * Turns the effect off while keeping the same markup, so nothing reflows. Use it to opt a grid
     * out wholesale, or to hold the effect back until something is ready.
     */
    tilt?: boolean
    /** Makes the card activatable — it becomes a real button, so it is keyboard reachable. */
    onclick?: (event: MouseEvent) => void
    /** Accessible name for that button. Falls back to the alt text. */
    label?: string
    /** Native title, used for the hover tooltip. */
    title?: string
    /** Replaces the image entirely; use for faces that are more than one picture. */
    children?: Snippet
  }

  let {
    src,
    alt = '',
    loading = 'lazy',
    aspect = '719 / 1000',
    tilt = true,
    onclick,
    label,
    title,
    children,
    class: classes = '',
    style = '',
    // Everything left over is hover-tilt's own tuning, forwarded untouched so callers get the whole
    // API — glare masks, springs, blend modes — without this component having to restate any of it.
    ...tiltProps
  }: Props = $props()

  // hover-tilt has no reduced-motion handling of its own, so neutralise the effect here instead of
  // unmounting it: same DOM, same box, just nothing that moves.
  const still = $derived(!tilt || prefersReducedMotion())

  const effects = $derived(
    still
      ? { tiltFactor: 0, tiltFactorY: 0, scaleFactor: 1, glareIntensity: 0, shadow: false }
      : tiltProps,
  )

  const interactive = $derived(Boolean(onclick))
</script>

<HoverTilt
  {...effects}
  class="hover-tilt-card {classes}"
  style="aspect-ratio: {aspect}; {style}"
>
  {#if interactive}
    <!-- A button rather than a click handler on the image: this is the only thing that makes the
         card reachable by keyboard, and it gives focus styling somewhere to land. -->
    <button
      type="button"
      {title}
      aria-label={label ?? alt ?? undefined}
      class="block size-full cursor-pointer [border-radius:inherit] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-500"
      {onclick}
    >
      {@render face()}
    </button>
  {:else}
    <div {title} class="size-full [border-radius:inherit]">
      {@render face()}
    </div>
  {/if}
</HoverTilt>

{#snippet face()}
  {#if children}
    {@render children()}
  {:else}
    <img
      {src}
      {alt}
      {loading}
      class="size-full bg-surface-200-800/40 object-cover [border-radius:inherit]"
    />
  {/if}
{/snippet}

<style>
  /*
    Scaling on hover makes a card overlap its neighbours; without this it would be painted under
    whichever tile comes later in the grid. hover-tilt sets data-is-active itself, so this follows
    the real animation rather than :hover, and survives the exit delay on the way back down.
  */
  :global(.hover-tilt-card[data-is-active='true']) {
    z-index: 1;
  }

  /*
    hover-tilt's inner layer sizes to its content, so without this the card's height comes from the
    image's intrinsic proportions and the aspect-ratio set on the container never governs anything.
    That looks fine right up until the art is missing or still loading — then the layer collapses to
    nothing and takes the card's box with it. Pinning it to the container is what makes the reserved
    box real.
  */
  :global(.hover-tilt-card > .hover-tilt) {
    height: 100%;
  }
</style>
