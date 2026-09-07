<script lang="ts">
  import ShoppingBasketIcon from '@lucide/svelte/icons/shopping-basket'

  import QuantityStepper from './QuantityStepper.svelte'
  import { tray } from './tray.svelte'

  /** Stand-in art for cards the catalog has no image for. */
  const CARD_BACK_URL = '/images/TcgImages/card-back.png'

  interface Props {
    /** Additional classes for the panel. */
    class?: string
  }

  let { class: classes = '' }: Props = $props()
</script>

<!-- Deliberately plain: no tilt, no glare, no lift. This is a list of what the user has picked, the
     way a shopping basket is, and art that moved under the pointer would invite inspecting cards
     here rather than in the results grid where inspecting them belongs. -->
<aside class="flex min-h-0 flex-col rounded-container border border-surface-200-800/50 {classes}">
  <header
    class="flex items-center justify-between gap-2 border-b border-surface-200-800/50 px-3 py-2"
  >
    <h3 class="font-semibold">
      Cards in Tray
      <!-- Copies, not rows: three of one card is three cards to place. -->
      <span class="opacity-60">({tray.totalQuantity})</span>
    </h3>

    <button
      type="button"
      class="btn btn-sm hover:preset-tonal"
      disabled={tray.isEmpty}
      onclick={() => tray.clear()}
    >
      Clear
    </button>
  </header>

  {#if tray.isEmpty}
    <div class="m-auto max-w-48 space-y-2 p-4 text-center">
      <ShoppingBasketIcon class="mx-auto size-8 opacity-40" />
      <p class="text-sm opacity-60">Cards you add show up here, ready to place in the binder.</p>
    </div>
  {:else}
    <ul class="min-h-0 flex-1 divide-y divide-surface-200-800/50 overflow-y-auto">
      {#each tray.entries as entry (entry.card.id)}
        {@const card = entry.card}
        <li class="flex items-center gap-3 p-3">
          <img
            src={card.imageUrl ?? CARD_BACK_URL}
            alt={card.imageUrl ? (card.name ?? 'Card') : 'No image available'}
            loading="lazy"
            class="w-12 shrink-0 rounded"
          />

          <!-- min-w-0 so a long card name ellipses instead of pushing the stepper off the panel. -->
          <div class="min-w-0 flex-1">
            <p class="truncate text-sm font-semibold">{card.name}</p>
            <p class="truncate text-xs opacity-60">
              {[card.setName, card.cardNumber].filter(Boolean).join(' · ')}
            </p>
          </div>

          <QuantityStepper
            quantity={entry.quantity}
            label={card.name ?? 'this card'}
            onchange={(next) => tray.setQuantity(card.id, next)}
          />
        </li>
      {/each}
    </ul>

    <footer class="border-t border-surface-200-800/50 px-3 py-2 text-xs opacity-60">
      {tray.cardCount}
      {tray.cardCount === 1 ? 'card' : 'cards'} · {tray.totalQuantity}
      {tray.totalQuantity === 1 ? 'copy' : 'copies'}
    </footer>
  {/if}
</aside>
