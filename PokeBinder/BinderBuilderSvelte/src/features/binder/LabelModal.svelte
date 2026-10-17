<!--
  Edits a pocket's label: a short text in one of eight colours. Opens from the Label button in the
  pocket toolbox, or from pressing it on a card that already has one. Lives at the App level so it
  is positioned against the viewport rather than clipped to a pocket.

  **Substep 1: nothing wired to the server or the save flow.** The writes go to
  `stores/labels.svelte.ts`, which holds the state for the session only. Substep 2 moves the
  storage into the slot itself.
-->
<script lang="ts">
  import TagIcon from '@lucide/svelte/icons/tag'
  import Trash2Icon from '@lucide/svelte/icons/trash-2'

  import Modal from '../../components/Modal.svelte'
  import { binderPage } from '../../stores/binder-page.svelte'
  import {
    LABEL_COLORS,
    MAX_LABEL_LENGTH,
    pocketLabels,
    type LabelColor,
  } from '../../stores/labels.svelte'

  /**
   * Swatch background for each colour name, so the picker reads at a glance. Chose Tailwind-named
   * colours (red, orange, …) rather than the Skeleton theme presets so the eight swatches read as
   * distinct categories rather than semantic states (primary, success, …) that already mean
   * something else in the app.
   */
  const SWATCH_CLASS: Record<LabelColor, string> = {
    red: 'bg-red-600',
    orange: 'bg-orange-600',
    amber: 'bg-amber-500',
    green: 'bg-green-600',
    teal: 'bg-teal-600',
    blue: 'bg-blue-600',
    purple: 'bg-purple-600',
    pink: 'bg-pink-600',
  }

  /**
   * The modal mirrors the editor state. Modal.svelte binds `open`, so dismissing from inside (the
   * X in the header, Escape, backdrop click) writes false back here; the effect below closes the
   * editor so the same action does not re-open on the next tick.
   */
  let open = $state(false)

  $effect(() => {
    open = pocketLabels.editingIndex !== null
  })

  $effect(() => {
    if (!open && pocketLabels.editingIndex !== null) pocketLabels.closeEditor()
  })

  // Draft form state: text and colour before Save is pressed. Seeded from the stored label when
  // the editor opens, or defaults when the pocket has none yet. Captured in a $derived keyed on
  // the editing pocket so switching pockets without closing refreshes the draft.
  let text = $state('')
  let color = $state<LabelColor>('blue')

  $effect(() => {
    const index = pocketLabels.editingIndex

    if (index === null) return

    const existing = pocketLabels.get(index)

    text = existing?.text ?? ''
    color = existing?.color ?? 'blue'
  })

  function save(): void {
    const index = pocketLabels.editingIndex

    if (index === null) return

    pocketLabels.set(index, text, color)
    pocketLabels.closeEditor()
  }

  function remove(): void {
    const index = pocketLabels.editingIndex

    if (index === null) return

    pocketLabels.clear(index)
    pocketLabels.closeEditor()
  }

  const trimmedLength = $derived(text.trim().length)
  const canSave = $derived(trimmedLength > 0)
  const hasExisting = $derived(
    pocketLabels.editingIndex !== null &&
      pocketLabels.get(pocketLabels.editingIndex) !== undefined,
  )
  const cardName = $derived.by(() => {
    const idx = pocketLabels.editingIndex

    if (idx === null) return 'the card'

    return binderPage.slots[idx]?.name ?? 'the card'
  })
</script>

<Modal bind:open title="Label this card" width="max-w-md">
  <p class="text-sm opacity-75">
    A short tag for <strong>{cardName}</strong>, in one of eight colours. The chip shows in the
    pocket's bottom-left corner.
  </p>

  <label class="label">
    <span class="label-text">Tag</span>
    <input
      type="text"
      class="input"
      maxlength={MAX_LABEL_LENGTH}
      placeholder="Trade, PSA 10, …"
      bind:value={text}
      onkeydown={(event) => {
        if (event.key === 'Enter' && canSave) {
          event.preventDefault()
          save()
        }
      }}
    />
    <!-- A tiny counter in the label's footer. Clamping at MAX_LABEL_LENGTH is done in the store
         too, so paste-and-truncate behaves. -->
    <span class="label-text text-right text-xs opacity-60">
      {trimmedLength}/{MAX_LABEL_LENGTH}
    </span>
  </label>

  <fieldset class="space-y-2">
    <legend class="label-text">Colour</legend>
    <!-- A radio group styled as swatches. aria-checked on each button, aria-labelledby via the
         legend, and the border reveals which one is picked. Flex-wrap so the row reflows on narrow
         viewports. -->
    <div class="flex flex-wrap gap-2" role="radiogroup" aria-label="Label colour">
      {#each LABEL_COLORS as swatch (swatch)}
        <button
          type="button"
          role="radio"
          aria-checked={color === swatch}
          aria-label={swatch}
          title={swatch}
          onclick={() => (color = swatch)}
          class="size-8 rounded-full ring-offset-2 ring-offset-surface-50-950 transition-shadow {SWATCH_CLASS[
            swatch
          ]} {color === swatch ? 'ring-2 ring-primary-500' : ''}"
        ></button>
      {/each}
    </div>
  </fieldset>

  <footer class="flex items-center justify-end gap-2">
    {#if hasExisting}
      <!-- Mr-auto pushes the destructive action to the left end, away from the primary -- the
           same pattern the delete-binder dialog uses, so a stray click on "Save" does not land on
           "Remove". -->
      <button
        type="button"
        class="btn btn-sm preset-filled-error-500 mr-auto"
        onclick={remove}
      >
        <Trash2Icon class="size-4" />
        <span>Remove label</span>
      </button>
    {/if}
    <button
      type="button"
      class="btn btn-sm preset-filled-primary-500"
      disabled={!canSave}
      onclick={save}
    >
      <TagIcon class="size-4" />
      <span>Save label</span>
    </button>
  </footer>
</Modal>
