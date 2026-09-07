<script lang="ts">
  import PlusIcon from '@lucide/svelte/icons/plus'

  import type { CardSearchResult } from '../clients/CardSearchClient'
  import QuantityStepper from './QuantityStepper.svelte'
  import { tray } from './tray.svelte'

  interface Props {
    card: CardSearchResult
  }

  let { card }: Props = $props()

  const quantity = $derived(tray.quantityOf(card.id))
  const inTray = $derived(quantity > 0)
  const name = $derived(card.name ?? 'this card')
</script>

<!-- Adds the card, then becomes the count for it. One click is the common case and gets a single
     button; once the card is in the tray the useful control is the count, so the same corner turns
     into a stepper. Stepping down to zero takes the card out and hands the plus back. -->
{#if inTray}
  <QuantityStepper
    {quantity}
    label={name}
    dense
    onchange={(next) => tray.setQuantity(card.id, next)}
  />
{:else}
  <button
    type="button"
    class="btn-icon btn-icon-sm shrink-0 preset-filled-primary-500"
    disabled={tray.isFull}
    title={tray.isFull ? 'The tray is full' : `Add ${name} to the tray`}
    aria-label="Add {name} to the tray"
    onclick={() => tray.add(card)}
  >
    <PlusIcon class="size-4" />
  </button>
{/if}
