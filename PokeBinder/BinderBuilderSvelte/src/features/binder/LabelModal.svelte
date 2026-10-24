<!--
  Assigns labels to a pocket. Opens from the Tag button in the toolbox and lives at the App level
  so it is positioned against the viewport rather than clipped to a pocket.

  Two tabs:
  - **Assign** (the default): two lists side by side -- the labels on this card at the top, and
    the labels the binder owns at the bottom. Drag from the binder onto the card to apply, or
    click a row in the binder list to apply it. Each chip on the card has an X that returns it to
    the binder list; Clear all empties the card side in one press. Three labels is the cap; the
    binder list disables once the cap is reached.
  - **Create label**: the inline create form. Save creates the label on the binder and, as long as
    the card has room, drops it into the draft in the same action, then switches back to Assign.

  **Everything in the modal is a draft until Apply.** Toggles, chip-X, Clear all and Create all
  work on a local copy of the pocket's label ids and on `pocketLabels.pending` for new rows -- the
  slot itself is untouched. Pressing **Apply** commits any new labels in the draft to the server
  (swapping negative ids for real ones), writes the real ids to the slot in one history entry,
  then closes. Pressing **Cancel** -- or the X, Escape, or a backdrop click -- throws the draft
  away and the close effect calls `commitPending` with no draft, which silently drops every
  pending label whose id no slot references.
-->
<script lang="ts">
  import CheckIcon from '@lucide/svelte/icons/check'
  import PlusIcon from '@lucide/svelte/icons/plus'
  import TagIcon from '@lucide/svelte/icons/tag'
  import XIcon from '@lucide/svelte/icons/x'

  import Modal from '../../components/Modal.svelte'
  import { binderPage } from '../../stores/binder-page.svelte'
  import {
    LABEL_COLORS,
    MAX_LABEL_LENGTH,
    MAX_LABELS_PER_POCKET,
    pocketLabels,
    type Label,
    type LabelColor,
  } from '../../stores/labels.svelte'

  /**
   * Swatch background per colour, so the picker reads at a glance. Tailwind-named colours rather
   * than Skeleton theme presets so the eight swatches read as categories rather than semantic
   * states (primary, success, …) that already mean something else in the app.
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

  const CHIP_CLASS: Record<LabelColor, string> = {
    red: 'bg-red-600 text-white',
    orange: 'bg-orange-600 text-white',
    amber: 'bg-amber-500 text-black',
    green: 'bg-green-600 text-white',
    teal: 'bg-teal-600 text-white',
    blue: 'bg-blue-600 text-white',
    purple: 'bg-purple-600 text-white',
    pink: 'bg-pink-600 text-white',
  }

  /**
   * Modal.svelte binds `open`. Apply commits any new labels, writes the real ids to the slot,
   * then sets `open = false`; the X, Escape, a backdrop click and the Cancel button all set
   * `open = false` without the write, so the draft is thrown away. The close effect calls
   * `closeEditor` and -- for the dismiss paths -- `commitPending` with no keep list, which drops
   * every pending label whose id no slot references. On the Apply path pending is already drained
   * by then, so that call is a no-op.
   *
   * No `save.hold` is needed: by the time `setPocketLabels` arms the debounced save, every id in
   * the draft is a real server id. The save fires with real ids on its own schedule.
   */
  let open = $state(false)
  let applying = $state(false)

  $effect(() => {
    open = pocketLabels.editingIndex !== null
  })

  $effect(() => {
    if (!open && pocketLabels.editingIndex !== null) {
      const binderId = binderPage.binderId

      pocketLabels.closeEditor()

      // Cancel / X / Escape / backdrop paths come through here with the draft never written to a
      // slot, so no pending id is in use and `commitPending` drops them all silently. On the Apply
      // path, pending is already empty.
      if (binderId !== null) {
        void pocketLabels.commitPending(binderId).catch((error: unknown) => {
          console.error('Failed to discard pending labels', error)
        })
      }
    }
  })

  /** Which tab is active. Reset to assign on every open, as the step calls for. */
  let activeTab = $state<'assign' | 'create'>('assign')

  /** Highlight the drop zone while a label is being dragged over it. */
  let dragOverCard = $state(false)

  // Draft form state for Create label. Seeded fresh every time the modal opens.
  let draftName = $state('')
  let draftColor = $state<LabelColor>('blue')

  /**
   * The pocket's label ids as the modal has been editing them. Seeded from the slot on open and
   * mutated by every toggle, chip-X, Clear all and Create-and-add; the slot itself is only
   * rewritten on Apply, so a Cancel (or any other dismissal) leaves the pocket as it was.
   */
  let draftLabelIds = $state<number[]>([])

  /**
   * The pocket's label ids as the modal was opened on, for the dirty check that lights up Apply.
   * Captured alongside the draft so a round-trip (add then remove the same chip) does not count.
   */
  let originalLabelIds: readonly number[] = []

  $effect(() => {
    const index = pocketLabels.editingIndex

    if (index === null) return

    const slot = binderPage.pocketAt(index)
    const seed = slot ? [...slot.labelIds] : []

    originalLabelIds = seed
    draftLabelIds = [...seed]
    activeTab = 'assign'
    draftName = ''
    draftColor = 'blue'
    dragOverCard = false
  })

  const cardName = $derived.by(() => {
    const idx = pocketLabels.editingIndex

    if (idx === null) return 'the card'

    return binderPage.slots[idx]?.name ?? 'the card'
  })

  /**
   * The draft, resolved to full `Label` objects in chip-strip order. Ids that no longer point at
   * a live label (would only happen if a Settings-side delete reached in while the modal was open)
   * are skipped.
   */
  const pocketAssignments = $derived.by(() => {
    const out: Label[] = []

    for (const id of draftLabelIds) {
      const label = pocketLabels.get(id)

      if (label) out.push(label)
    }

    return out
  })

  const pocketCount = $derived(draftLabelIds.length)
  const atCap = $derived(pocketCount >= MAX_LABELS_PER_POCKET)
  const hasAny = $derived(pocketCount > 0)

  const labelsList = $derived(pocketLabels.all)

  /**
   * Labels the binder owns that are not currently in the draft. The Assign tab's binder list only
   * shows these -- dragging from the on-card chips back to the binder list is modelled through
   * the chip's X, not through drag.
   */
  const availableLabels = $derived.by(() => {
    const onCard = new Set(draftLabelIds)

    return labelsList.filter((label) => !onCard.has(label.id))
  })

  /** Whether the draft differs from the slot. Apply is only live while something has changed. */
  const dirty = $derived.by(() => {
    if (draftLabelIds.length !== originalLabelIds.length) return true

    for (let i = 0; i < draftLabelIds.length; i += 1) {
      if (draftLabelIds[i] !== originalLabelIds[i]) return true
    }

    return false
  })

  function tryAdd(labelId: number): void {
    if (atCap || draftLabelIds.includes(labelId)) return
    if (!pocketLabels.get(labelId)) return

    draftLabelIds = [...draftLabelIds, labelId]
  }

  function removeFromCard(labelId: number): void {
    if (!draftLabelIds.includes(labelId)) return

    draftLabelIds = draftLabelIds.filter((id) => id !== labelId)
  }

  function clearAll(): void {
    if (draftLabelIds.length === 0) return

    draftLabelIds = []
  }

  function onDragStart(event: DragEvent, labelId: number): void {
    if (!event.dataTransfer) return

    event.dataTransfer.setData('text/plain', String(labelId))
    event.dataTransfer.effectAllowed = 'copyMove'
  }

  function onDragOverCard(event: DragEvent): void {
    // Must preventDefault on every pass or the browser refuses the drop; the capacity check only
    // governs whether the zone lights up. If the drop fires at cap, the drop handler no-ops.
    event.preventDefault()
    dragOverCard = !atCap
  }

  function onDragLeaveCard(event: DragEvent): void {
    // Crossing between the chips inside the zone fires dragleave too; relatedTarget tells the two
    // apart, so only a pointer that has actually left the zone drops the highlight.
    if (event.currentTarget instanceof Node && event.relatedTarget instanceof Node) {
      if ((event.currentTarget as Node).contains(event.relatedTarget as Node)) return
    }

    dragOverCard = false
  }

  function onDropCard(event: DragEvent): void {
    event.preventDefault()
    dragOverCard = false

    if (atCap) return

    const payload = event.dataTransfer?.getData('text/plain') ?? ''
    const id = Number(payload)

    if (!Number.isFinite(id)) return

    tryAdd(id)
  }

  // Create tab: trimmed name, duplicate check, Save path.
  const trimmedDraftLength = $derived(draftName.trim().length)
  const nameClashes = $derived(trimmedDraftLength > 0 && pocketLabels.nameTaken(draftName))
  const canCreate = $derived(trimmedDraftLength > 0 && !nameClashes)

  /**
   * Creates a label on the binder (as a pending row) and drops it into the pocket draft when there
   * is room. On Cancel, `commitPending` discards any pending label with no slot pointing at it, so
   * a label created here is kept only if the user then presses Apply.
   */
  function createAndApply(): void {
    if (!canCreate) return

    const newId = pocketLabels.createLabel({ name: draftName, color: draftColor })

    if (!atCap) draftLabelIds = [...draftLabelIds, newId]

    draftName = ''
    draftColor = 'blue'
    activeTab = 'assign'
  }

  /**
   * Commits any new labels in the draft to the server, translates the draft's negative ids to the
   * real ones that come back, writes the whole list to the slot in one history entry, and closes.
   * On a server failure the modal stays open with the draft intact so the user can retry.
   */
  async function apply(): Promise<void> {
    const idx = pocketLabels.editingIndex

    if (idx === null || applying) return

    if (!dirty) {
      open = false
      return
    }

    const binderId = binderPage.binderId
    applying = true

    try {
      let finalIds: readonly number[] = draftLabelIds

      if (binderId !== null) {
        const mapping = await pocketLabels.commitPending(binderId, draftLabelIds)

        if (mapping.size > 0) {
          finalIds = draftLabelIds.map((id) => mapping.get(id) ?? id)
        }
      }

      binderPage.setPocketLabels(idx, finalIds)
      open = false
    } catch (error) {
      // Keep the modal open so the collector can retry; the toast system isn't wired yet in this
      // step, so for now a console error is what surfaces it.
      console.error('Failed to commit labels', error)
    } finally {
      applying = false
    }
  }

  /** Throws the draft away. The close effect then clears any unused pending labels. */
  function cancel(): void {
    if (applying) return

    open = false
  }
</script>

<Modal bind:open title="Labels on this card" width="max-w-lg">
  <p class="text-sm opacity-75">
    Up to three labels for <strong>{cardName}</strong>. Drag labels from the binder onto this
    card, or click them to add. Manage the binder's full list in Settings.
  </p>

  <!-- Tabs. Low-key, no border-radius: this is a modal, not a panel, so the tabs read as
       section switchers rather than cards of their own. -->
  <div
    role="tablist"
    aria-label="Label mode"
    class="flex gap-1 border-b border-surface-200-800/50"
  >
    <button
      type="button"
      role="tab"
      aria-selected={activeTab === 'assign'}
      class="btn btn-sm -mb-px rounded-b-none border-b-2 {activeTab === 'assign'
        ? 'border-primary-500 preset-tonal-primary'
        : 'border-transparent hover:preset-tonal'}"
      onclick={() => (activeTab = 'assign')}
    >
      <TagIcon class="size-4" />
      <span>Assign</span>
    </button>
    <button
      type="button"
      role="tab"
      aria-selected={activeTab === 'create'}
      class="btn btn-sm -mb-px rounded-b-none border-b-2 {activeTab === 'create'
        ? 'border-primary-500 preset-tonal-primary'
        : 'border-transparent hover:preset-tonal'}"
      onclick={() => (activeTab = 'create')}
    >
      <PlusIcon class="size-4" />
      <span>Create label</span>
    </button>
  </div>

  {#if activeTab === 'assign'}
    <!-- On this card: the drop zone, with removable chips. -->
    <section class="space-y-2" aria-labelledby="assign-on-card">
      <header class="flex items-center justify-between">
        <h3 id="assign-on-card" class="label-text">On this card ({pocketCount}/3)</h3>
        {#if hasAny}
          <button type="button" class="btn btn-sm preset-tonal" onclick={clearAll}>
            <XIcon class="size-3" />
            <span>Clear all</span>
          </button>
        {/if}
      </header>

      <!-- svelte-ignore a11y_no_static_element_interactions -->
      <div
        class="flex min-h-16 flex-wrap items-start gap-2 rounded-container border-2 border-dashed p-3 transition-colors {dragOverCard
          ? 'border-primary-500 bg-primary-500/10'
          : atCap
            ? 'border-surface-200-800/40'
            : 'border-surface-200-800/60'}"
        aria-label="Labels on this card"
        ondragenter={onDragOverCard}
        ondragover={onDragOverCard}
        ondragleave={onDragLeaveCard}
        ondrop={onDropCard}
      >
        {#if pocketAssignments.length === 0}
          <p class="text-sm opacity-60">
            {atCap
              ? 'Three labels is the cap.'
              : 'Drag a label here from below, or click one to add it.'}
          </p>
        {:else}
          {#each pocketAssignments as chip (chip.id)}
            <!-- The chip itself is not draggable: on-card chips are managed through the X, which
                 the step calls for. -->
            <span
              class="badge inline-flex items-center gap-1.5 pr-1 text-xs font-semibold shadow {CHIP_CLASS[
                chip.color
              ]}"
            >
              <span class="pl-1">{chip.name}</span>
              <button
                type="button"
                class="rounded-full p-0.5 transition-colors hover:bg-black/20"
                aria-label="Remove {chip.name} from this card"
                title="Remove {chip.name}"
                onclick={() => removeFromCard(chip.id)}
              >
                <XIcon class="size-3" />
              </button>
            </span>
          {/each}
        {/if}
      </div>
    </section>

    <!-- Available in this binder: drag source, also clickable to add. Hidden when every label is
         on the card already (the hint belongs with the drop zone above, not down here). -->
    <section class="space-y-2" aria-labelledby="assign-available">
      <h3 id="assign-available" class="label-text">Available in this binder</h3>
      {#if labelsList.length === 0}
        <p class="rounded-container border border-surface-200-800/50 p-3 text-sm opacity-75">
          No labels yet. Switch to <strong>Create label</strong> to add the first one.
        </p>
      {:else if availableLabels.length === 0}
        <p class="rounded-container border border-surface-200-800/50 p-3 text-sm opacity-75">
          Every label in this binder is on this card already.
        </p>
      {:else}
        <ul class="max-h-56 space-y-1 overflow-y-auto">
          {#each availableLabels as label (label.id)}
            <li>
              <!-- A button so the keyboard path works (click to add); draggable so the pointer
                   path works too. active:cursor-grabbing is the grab-pointer feedback. -->
              <button
                type="button"
                draggable="true"
                ondragstart={(event) => onDragStart(event, label.id)}
                onclick={() => tryAdd(label.id)}
                disabled={atCap}
                title={atCap
                  ? 'Three labels is the cap -- remove one before adding another'
                  : `Add '${label.name}' to this card`}
                aria-label={atCap
                  ? `${label.name} (cap reached)`
                  : `Add ${label.name} to this card`}
                class="flex w-full cursor-grab items-center gap-3 rounded-container p-2 text-left transition-colors hover:preset-tonal active:cursor-grabbing disabled:cursor-not-allowed disabled:opacity-50"
              >
                <span class="size-5 shrink-0 rounded-full {SWATCH_CLASS[label.color]}"></span>
                <span class="flex-1 truncate text-sm font-medium">{label.name}</span>
              </button>
            </li>
          {/each}
        </ul>
      {/if}
    </section>
  {:else}
    <!-- Create tab: same inline form, now reachable only through the tab. Save creates the
         label on the binder and, as long as the card has room, applies it to this pocket, then
         switches back to Assign so the collector sees the new chip in place. -->
    <label class="label">
      <span class="label-text">Label name</span>
      <input
        type="text"
        class="input"
        maxlength={MAX_LABEL_LENGTH}
        placeholder="Trade, PSA 10, …"
        autofocus
        bind:value={draftName}
        onkeydown={(event) => {
          if (event.key === 'Enter' && canCreate) {
            event.preventDefault()
            createAndApply()
          }
        }}
      />
      <span class="label-text text-right text-xs opacity-60">
        {trimmedDraftLength}/{MAX_LABEL_LENGTH}
      </span>
      {#if nameClashes}
        <!-- Unique-per-binder rule, called out while typing rather than refused on press.
             Substep 3's validator enforces the same thing server-side. -->
        <span class="text-xs text-error-500">That name is already used in this binder.</span>
      {/if}
    </label>

    <fieldset class="space-y-2">
      <legend class="label-text">Colour</legend>
      <div class="flex flex-wrap gap-2" role="radiogroup" aria-label="Label colour">
        {#each LABEL_COLORS as swatch (swatch)}
          <button
            type="button"
            role="radio"
            aria-checked={draftColor === swatch}
            aria-label={swatch}
            title={swatch}
            onclick={() => (draftColor = swatch)}
            class="size-8 rounded-full ring-offset-2 ring-offset-surface-50-950 transition-shadow {SWATCH_CLASS[
              swatch
            ]} {draftColor === swatch ? 'ring-2 ring-primary-500' : ''}"
          ></button>
        {/each}
      </div>
    </fieldset>

    <footer class="flex items-center justify-end gap-2">
      <button
        type="button"
        class="btn btn-sm preset-tonal"
        disabled={!canCreate}
        onclick={createAndApply}
      >
        <TagIcon class="size-4" />
        <span>{atCap ? 'Create label' : 'Create and add'}</span>
      </button>
    </footer>
  {/if}

  <!-- Apply writes the whole draft to the slot in one history entry; Cancel throws it away. The X,
       Escape and a backdrop click all land through Modal's `open` binding and so share the same
       Cancel path. Shown in both tabs so the collector can save or back out from wherever they
       are. -->
  <footer
    class="flex items-center justify-end gap-2 border-t border-surface-200-800/50 pt-3"
  >
    <p class="mr-auto text-sm opacity-60">
      {#if dirty}
        Unsaved changes.
      {:else}
        No changes.
      {/if}
    </p>
    <button type="button" class="btn btn-sm preset-tonal" disabled={applying} onclick={cancel}>
      <XIcon class="size-4" />
      <span>Cancel</span>
    </button>
    <button
      type="button"
      class="btn btn-sm preset-filled-primary-500"
      disabled={!dirty || applying}
      onclick={apply}
    >
      <CheckIcon class="size-4" />
      <span>{applying ? 'Applying…' : 'Apply'}</span>
    </button>
  </footer>
</Modal>
