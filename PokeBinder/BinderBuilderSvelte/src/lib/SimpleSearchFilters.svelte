<script lang="ts" module>
  /** The current state of every simple search field, keyed by field. */
  export interface SimpleSearchTerms {
    cardName: string
    cardIdentifier: string
  }

  export function emptyTerms(): SimpleSearchTerms {
    return { cardName: '', cardIdentifier: '' }
  }
</script>

<script lang="ts">
  interface Props {
    /** The current search terms. Bindable. */
    terms?: SimpleSearchTerms
  }

  let { terms = $bindable(emptyTerms()) }: Props = $props()

  function resetTerms() {
    terms = emptyTerms()
  }
</script>

<aside class="min-h-0 space-y-5 overflow-y-auto pr-2">
  <header class="flex items-center justify-between">
    <h3 class="font-semibold">Search</h3>
    <button type="button" class="btn btn-sm hover:preset-tonal opacity-75" onclick={resetTerms}>
      Reset
    </button>
  </header>

  <label class="label">
    <span class="label-text">Card Name</span>
    <input class="input" type="text" placeholder="Type a card name" bind:value={terms.cardName} />
  </label>

  <label class="label">
    <span class="label-text">Card Identifier</span>
    <input
      class="input"
      type="text"
      placeholder="Type a card identifier"
      bind:value={terms.cardIdentifier}
    />
  </label>
</aside>
