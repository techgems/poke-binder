<script lang="ts">
  import MinusIcon from '@lucide/svelte/icons/minus'
  import PlusIcon from '@lucide/svelte/icons/plus'

  import { MAX_QUANTITY } from './tray.svelte'

  interface Props {
    /** Copies currently held. */
    quantity: number
    /** Called with the new count. Reaching zero is the caller's cue to drop the card. */
    onchange: (quantity: number) => void
    /** Names the card the stepper belongs to, so the buttons read as more than "minus". */
    label: string
    /**
     * Sized for a result tile, which is about a hundred pixels wide with a card name already in
     * it: bare icon buttons instead of Skeleton's, because a btn-icon pair plus the number is
     * wider than the whole tile. The tray uses the roomier default.
     */
    dense?: boolean
  }

  let { quantity, onchange, label, dense = false }: Props = $props()

  const buttonClasses = $derived(
    dense
      ? 'rounded p-0.5 text-primary-500 hover:preset-tonal-primary'
      : 'btn-icon hover:preset-tonal',
  )
</script>

<div
  class="flex shrink-0 items-center {dense ? 'gap-0.5' : 'gap-1'}"
  role="group"
  aria-label="Quantity for {label}"
>
  <button
    type="button"
    class={buttonClasses}
    aria-label="One fewer {label}"
    onclick={() => onchange(quantity - 1)}
  >
    <MinusIcon class={dense ? 'size-3' : 'size-4'} />
  </button>

  <!-- Fixed width so the buttons do not shuffle sideways as the count changes. -->
  <span
    class="text-center font-semibold tabular-nums {dense ? 'min-w-3 text-xs' : 'min-w-6 text-sm'}"
    aria-live="polite"
  >
    {quantity}
  </span>

  <button
    type="button"
    class={buttonClasses}
    aria-label="One more {label}"
    disabled={quantity >= MAX_QUANTITY}
    onclick={() => onchange(quantity + 1)}
  >
    <PlusIcon class={dense ? 'size-3' : 'size-4'} />
  </button>
</div>
