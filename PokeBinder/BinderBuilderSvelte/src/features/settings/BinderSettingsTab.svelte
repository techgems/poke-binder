<!--
  The binder's own properties -- name, description, grid and page count -- and deleting it.

  A save ends in a refresh and a delete in a redirect to /my-binders, so nothing in this tab is
  ever undone.
-->
<script lang="ts">
  import ArrowDownWideNarrowIcon from '@lucide/svelte/icons/arrow-down-wide-narrow'
  import EraserIcon from '@lucide/svelte/icons/eraser'
  import ListOrderedIcon from '@lucide/svelte/icons/list-ordered'
  import MinusIcon from '@lucide/svelte/icons/minus'
  import PlusIcon from '@lucide/svelte/icons/plus'
  import ShoppingBasketIcon from '@lucide/svelte/icons/shopping-basket'
  import Trash2Icon from '@lucide/svelte/icons/trash-2'
  import TriangleAlertIcon from '@lucide/svelte/icons/triangle-alert'
  import type { Component } from 'svelte'
  import { fade } from 'svelte/transition'

  import type { Rearrangement } from '../../clients/BinderClient'
  import Modal from '../../components/Modal.svelte'
  import { prefersReducedMotion } from '../../components/tilt/prefers-reduced-motion.svelte'
  import { toaster } from '../../components/toaster'
  import { preloadedBinder } from '../../preloads/preload'
  import { binderPage } from '../../stores/binder-page.svelte'
  import { MAX_CARDS, tray } from '../../stores/tray.svelte'
  import type { SortCriterion } from '../binder/sort/reorder-criteria'
  import SortCriteriaEditor from '../binder/sort/SortCriteriaEditor.svelte'
  import { sortSession } from '../binder/sort/sort-session'
  import { BINDER_SIZES, MAX_PAGES } from './binder-sizes'
  import { deleteBinder } from './delete-binder'
  import { saveBinderSettings } from './save-settings'
  import { resizeImpact, type ResizeChoice } from './resize-impact'

  const MAX_NAME_LENGTH = 100
  const MAX_DESCRIPTION_LENGTH = 500

  // What the page was opened with. Read once: a save refreshes the page, so the values the form is
  // compared against never change while it is open.
  const saved = preloadedBinder()?.binder ?? null

  let name = $state(saved?.name ?? '')
  let description = $state(saved?.description ?? '')
  let sizeId = $state(saved?.binderSizeId ?? 0)
  let pages = $state(saved?.pages ?? 1)

  // Keeping the order is the default: it is the one choice that changes nothing but where the
  // cards sit.
  let choice = $state<ResizeChoice | null>('keep')
  let criteria = $state<SortCriterion[]>(sortSession.criteria)

  let deleteOpen = $state(false)

  // Out to the server, and then -- once it has answered -- waiting on the refresh. Either way the
  // form is done with.
  let saving = $state(false)
  let deleting = $state(false)

  // The binder's own grid stands in when the list is missing, which only a page that failed to
  // embed it would leave: the tab still draws, with that one grid to offer.
  const size = $derived(
    BINDER_SIZES.find((s) => s.id === sizeId) ?? {
      id: sizeId,
      name: saved?.sizeName ?? '',
      description: saved?.sizeDescription ?? '',
      x: binderPage.columns,
      y: binderPage.rows,
      cardsPerPage: binderPage.cardsPerPage,
      defaultPages: saved?.pages ?? 1,
    },
  )

  const impact = $derived(
    resizeImpact(
      binderPage.slots,
      tray.entries,
      { columns: binderPage.columns, rows: binderPage.rows },
      { columns: size.x, rows: size.y, pages },
    ),
  )

  const nameError = $derived(name.trim() === '' ? 'Give the binder a name.' : null)

  const pagesValid = $derived(Number.isInteger(pages) && pages >= 1 && pages <= MAX_PAGES)

  const dirty = $derived(
    saved !== null &&
      (name.trim() !== saved.name ||
        (description.trim() || null) !== (saved.description ?? null) ||
        sizeId !== saved.binderSizeId ||
        pages !== saved.pages),
  )

  // The tray choice is only on offer while the cards fit in it, and dropping only while they do
  // not, so a choice the size change has taken off the table stops counting as made.
  const offered = $derived<ResizeChoice[]>(
    impact.trayFits ? ['keep', 'sort', 'tray'] : ['keep', 'sort', 'drop'],
  )

  const choiceMade = $derived(
    choice !== null &&
      offered.includes(choice) &&
      (choice === 'tray' || choice === 'drop' || impact.cardsFit),
  )

  const canSave = $derived(
    !saving && dirty && nameError === null && pagesValid && (!impact.affected || choiceMade),
  )

  const trayCount = $derived(tray.cardCount)

  function stepPages(by: number): void {
    pages = Math.min(Math.max((Number.isInteger(pages) ? pages : 1) + by, 1), MAX_PAGES)
  }

  function discard(): void {
    if (!saved) return

    name = saved.name
    description = saved.description ?? ''
    sizeId = saved.binderSizeId
    pages = saved.pages
    choice = 'keep'
    criteria = sortSession.criteria
  }

  const REARRANGEMENTS: Record<ResizeChoice, Rearrangement> = {
    keep: 'Keep',
    sort: 'Sort',
    tray: 'Tray',
    drop: 'Drop',
  }

  /**
   * Stores the edit and refreshes the page a moment after the toast. The choice only travels when
   * the change moves cards -- the server leaves every card in its pocket otherwise -- and the
   * criteria only with a sort.
   */
  async function save(): Promise<void> {
    if (!canSave) return

    const rearrange = impact.affected && choice !== null ? REARRANGEMENTS[choice] : null

    saving = true

    try {
      await saveBinderSettings(
        {
          name: name.trim(),
          description: description.trim() || null,
          binderSizeId: sizeId,
          pages,
          rearrange,
          criteria: rearrange === 'Sort' ? criteria.map((criterion) => ({ ...criterion })) : null,
        },
        () => {
          if (rearrange === 'Sort') sortSession.remember(criteria)

          toaster.success({
            title: 'Binder saved',
            description: 'Refreshing to show your changes…',
          })
        },
      )
    } catch {
      toaster.error({
        title: 'Not saved',
        description: 'The binder is as it was. Try again in a moment.',
      })

      saving = false
    }
  }

  /**
   * Stays open until the server answers. Success leaves the page for My Binders, so there is
   * nothing to close; a failure is reported here with the binder still there to try again on.
   */
  async function confirmDelete(): Promise<void> {
    if (deleting) return

    deleting = true

    try {
      await deleteBinder()
    } catch {
      toaster.error({
        title: 'Not deleted',
        description: 'The binder is still here. Try again in a moment.',
      })

      deleting = false
    }
  }

  function plural(count: number, one: string, many = `${one}s`): string {
    return `${count} ${count === 1 ? one : many}`
  }

  interface ChoiceOption {
    value: ResizeChoice
    icon: Component
    title: string
    detail: string
    disabledReason: string | null
    tone: 'primary' | 'error'
  }

  const doesNotFit = $derived(
    impact.cardsFit
      ? null
      : `${plural(impact.capacity, 'pocket')} for ${plural(impact.placed, 'card')}. Add pages or pick a bigger grid.`,
  )

  const choiceOptions = $derived<ChoiceOption[]>(
    offered.map((value): ChoiceOption => {
      switch (value) {
        case 'keep':
          return {
            value,
            icon: ListOrderedIcon,
            title: 'Keep the order',
            detail:
              'Same order as now, from the first pocket. Empty pockets and blank pages close up.',
            disabledReason: doesNotFit,
            tone: 'primary',
          }
        case 'sort':
          return {
            value,
            icon: ArrowDownWideNarrowIcon,
            title: 'Sort them',
            detail: 'In the order you pick, from the first pocket.',
            disabledReason: doesNotFit,
            tone: 'primary',
          }
        case 'tray':
          return {
            value,
            icon: ShoppingBasketIcon,
            title: 'Empty into the tray',
            detail: `Every card goes back to the tray${trayCount > 0 ? `, beside the ${plural(trayCount, 'card')} already there` : ''}. The binder starts empty.`,
            disabledReason: null,
            tone: 'primary',
          }
        case 'drop':
          return {
            value,
            icon: EraserIcon,
            title: 'Remove them',
            detail: `Too many for the tray, which holds ${MAX_CARDS} different cards. They are removed and the binder starts empty.`,
            disabledReason: null,
            tone: 'error',
          }
      }
    }),
  )

  // Sort is the one answer with more to say: picking it opens its criteria beside the choices,
  // which then stack in a column of their own. Every other answer leaves the choices in a row.
  const sortOpen = $derived(choice === 'sort' && impact.cardsFit)

  let choiceList = $state<HTMLElement | null>(null)

  // Where each choice sat before the layout flipped, so the cards can glide from the row into the
  // column (or back) instead of jumping. Taken in a pre-effect, which runs before the DOM is
  // updated for the new value of sortOpen.
  let morphFrom: Map<Element, DOMRect> | null = null

  $effect.pre(() => {
    void sortOpen

    if (!choiceList) return

    morphFrom = new Map([...choiceList.children].map((el) => [el, el.getBoundingClientRect()]))
  })

  // FLIP: each card starts where and how big it was, and animates to where the new layout put it.
  // Width and height go with the position, so a card does not snap to its new size and then slide.
  $effect(() => {
    void sortOpen

    const from = morphFrom

    morphFrom = null

    if (!from || !choiceList || prefersReducedMotion()) return

    for (const el of choiceList.children) {
      const was = from.get(el)

      if (!was) continue

      const now = el.getBoundingClientRect()

      if (was.left === now.left && was.top === now.top && was.width === now.width) continue

      el.animate(
        [
          {
            transform: `translate(${was.left - now.left}px, ${was.top - now.top}px)`,
            width: `${was.width}px`,
            height: `${was.height}px`,
          },
          { transform: 'none', width: `${now.width}px`, height: `${now.height}px` },
        ],
        { duration: 250, easing: 'cubic-bezier(0.2, 0, 0, 1)' },
      )
    }
  })

  // Why the question is being asked, in terms of what the user changed.
  const impactReason = $derived(
    impact.gridChanged
      ? `A ${size.name} page holds its cards in different places, so all ${plural(impact.placed, 'placed card')} would move.`
      : `${plural(impact.stranded, 'card sits', 'cards sit')} past the last pocket of a ${pages}-page binder.`,
  )
</script>

{#if saved === null}
  <p class="p-4 opacity-60">Open a binder to change its settings.</p>
{:else}
  <div class="flex min-h-0 flex-1 flex-col gap-3">
    <!-- Two columns, centred, sharing the panel's width two to three: the panel is far wider than
         it is tall, so Details and Size sit side by side rather than one under the other, and the
         pair is centred so a wide screen does not leave everything against the left edge. The
         resize question below uses the same split, so its halves line up under these. It still
         scrolls when it has to -- a sort's criteria can add more than the panel holds --
         and the bar with Save stays put below it either way.

         relative because every card here hides a radio with sr-only, which is absolutely
         positioned: without a positioned ancestor inside the scroller, those inputs are placed
         against the page, stretch the document past the viewport, and focusing one scrolls the
         whole workspace up under the app bar. -->
    <div class="relative min-h-0 flex-1 overflow-y-auto pr-1">
      <div class="mx-auto w-full max-w-[88rem] space-y-6">
        <div class="grid gap-x-20 gap-y-6 lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)]">
          <!-- Details. A column, so Description can take whatever height Size leaves beside it. -->
          <section class="flex flex-col gap-4">
            <h2 class="h5">Details</h2>

            <label class="label">
              <span class="label-text">Name</span>
              <input
                class="input"
                type="text"
                maxlength={MAX_NAME_LENGTH}
                bind:value={name}
                aria-invalid={nameError !== null}
              />
              {#if nameError}
                <span class="text-sm text-error-500">{nameError}</span>
              {/if}
            </label>

            <label class="label flex flex-1 flex-col">
              <span class="label-text flex justify-between">
                <span>Description</span>
                <span class="opacity-50">{description.length}/{MAX_DESCRIPTION_LENGTH}</span>
              </span>
              <textarea
                class="textarea min-h-28 flex-1 resize-none"
                maxlength={MAX_DESCRIPTION_LENGTH}
                placeholder="What this binder is for"
                bind:value={description}
              ></textarea>
            </label>
          </section>

          <!-- Size -->
          <section class="space-y-4">
            <h2 class="h5">Size</h2>

            <fieldset class="space-y-2">
              <legend class="label-text">Grid</legend>
              <div class="grid grid-cols-5 gap-3">
                {#each BINDER_SIZES as option (option.id)}
                  {@const selected = option.id === sizeId}
                  <label
                    class="card flex cursor-pointer flex-col items-center gap-1.5 border-2 px-2 py-3 transition-colors has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-primary-500 {selected
                      ? 'border-primary-500 preset-tonal-primary'
                      : 'border-transparent preset-tonal hover:border-surface-400-600'}"
                  >
                    <input
                      class="sr-only"
                      type="radio"
                      name="binder-size"
                      value={option.id}
                      bind:group={sizeId}
                    />
                    <!-- A page of the grid in miniature: one block per pocket. -->
                    <span
                      class="grid aspect-[3/4] w-12 gap-0.5 rounded-sm p-0.5 {selected
                        ? 'bg-primary-500/20'
                        : 'bg-surface-500/20'}"
                      style="grid-template-columns: repeat({option.x}, 1fr); grid-template-rows: repeat({option.y}, 1fr);"
                      aria-hidden="true"
                    >
                      {#each Array(option.x * option.y) as _, pocket (pocket)}
                        <span class="rounded-[2px] {selected ? 'bg-primary-500' : 'bg-surface-500/60'}"
                        ></span>
                      {/each}
                    </span>
                    <span class="font-semibold">{option.name}</span>
                    <span class="text-center text-xs opacity-60">{option.description}</span>
                    {#if option.id === saved.binderSizeId}
                      <span class="badge preset-tonal text-xs">Current</span>
                    {/if}
                  </label>
                {/each}
              </div>
            </fieldset>

            <!-- A div, not a label: a label wrapping the step buttons would name the first of them
                 "Pages" rather than the field. -->
            <div class="label">
              <span class="label-text">Pages</span>
              <div class="flex flex-wrap items-center gap-x-4 gap-y-2">
                <!-- The field itself carries the same input styling as Name, so it reads as
                     somewhere to type; the buttons either side only step it. -->
                <div class="flex items-center gap-2">
                  <button
                    type="button"
                    class="btn-icon preset-tonal"
                    aria-label="One page fewer"
                    disabled={pages <= 1}
                    onclick={() => stepPages(-1)}
                  >
                    <MinusIcon class="size-4" />
                  </button>
                  <input
                    class="input w-24 text-center"
                    type="number"
                    min="1"
                    max={MAX_PAGES}
                    aria-label="Pages"
                    bind:value={pages}
                    aria-invalid={!pagesValid}
                  />
                  <button
                    type="button"
                    class="btn-icon preset-tonal"
                    aria-label="One page more"
                    disabled={pages >= MAX_PAGES}
                    onclick={() => stepPages(1)}
                  >
                    <PlusIcon class="size-4" />
                  </button>
                </div>

                <p class="text-sm opacity-75">
                  {#if pagesValid}
                    {plural(pages, 'page')} of {size.x * size.y} =
                    <strong>{plural(impact.capacity, 'pocket')}</strong>,
                    {plural(impact.placed, 'card')} placed now.
                  {:else}
                    <span class="text-error-500">A binder has between 1 and {MAX_PAGES} pages.</span>
                  {/if}
                </p>
              </div>
            </div>
          </section>
        </div>

        <!-- Only when the change would move a card. Pages added at the end, or empty ones taken off
             it, save without a question. Two columns again: the question and its answers, then the
             sort's criteria beside them when Sort is the answer. -->
        {#if impact.affected && pagesValid}
          <section
            class="card grid items-start gap-x-20 gap-y-4 border border-warning-500/50 bg-warning-500/10 p-4 {sortOpen
              ? 'lg:grid-cols-[minmax(0,2fr)_minmax(min-content,3fr)]'
              : ''}"
            aria-labelledby="resize-heading"
          >
            <div class="space-y-3">
              <header class="flex items-start gap-3">
                <TriangleAlertIcon class="size-5 shrink-0 text-warning-500" />
                <div class="space-y-1">
                  <h3 id="resize-heading" class="font-semibold">
                    This change moves the cards in the binder
                  </h3>
                  <p class="text-sm opacity-75">{impactReason} Choose what happens to them.</p>
                </div>
              </header>

              <div
                bind:this={choiceList}
                class="grid gap-2 {sortOpen ? '' : 'md:grid-cols-3'}"
                role="radiogroup"
                aria-labelledby="resize-heading"
              >
                {#each choiceOptions as option (option.value)}
                  {@const selected = choice === option.value}
                  {@const Icon = option.icon}
                  <label
                    class="card flex gap-3 border-2 px-3 py-2 transition-colors has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-primary-500 {option.disabledReason
                      ? 'cursor-not-allowed border-transparent preset-tonal opacity-60'
                      : selected
                        ? option.tone === 'error'
                          ? 'cursor-pointer border-error-500 preset-tonal-error'
                          : 'cursor-pointer border-primary-500 preset-tonal-primary'
                        : 'cursor-pointer border-transparent preset-tonal hover:border-surface-400-600'}"
                  >
                    <input
                      class="sr-only"
                      type="radio"
                      name="resize-choice"
                      value={option.value}
                      disabled={option.disabledReason !== null}
                      bind:group={choice}
                    />
                    <Icon class="mt-0.5 size-4 shrink-0" />
                    <span>
                      <span class="block font-semibold">{option.title}</span>
                      <span class="block text-sm opacity-75">
                        {option.disabledReason ?? option.detail}
                      </span>
                    </span>
                  </label>
                {/each}
              </div>
            </div>

            {#if sortOpen}
              <div
                class="space-y-2"
                in:fade={{ duration: prefersReducedMotion() ? 0 : 200, delay: 100 }}
              >
                <h4 class="font-semibold">Sort by</h4>
                <p class="text-sm opacity-75">
                  The first criterion decides; the ones below it only break its ties.
                </p>
                <SortCriteriaEditor bind:criteria />
              </div>
            {/if}
          </section>
        {/if}
      </div>
    </div>

    <footer
      class="flex shrink-0 items-center justify-end gap-3 border-t border-surface-200-800/50 pt-3"
    >
      <!-- At the far end from Save, so the two are never mistaken for each other. -->
      <button
        type="button"
        class="btn preset-tonal-error"
        disabled={saving}
        onclick={() => (deleteOpen = true)}
      >
        <Trash2Icon class="size-4" />
        <span>Delete binder</span>
      </button>
      <p class="mr-auto text-sm opacity-60">
        {#if !dirty}
          No changes.
        {:else if impact.affected && !choiceMade}
          Choose what happens to the cards before saving.
        {:else}
          Unsaved changes. Saving refreshes the page.
        {/if}
      </p>
      <button type="button" class="btn preset-tonal" disabled={!dirty || saving} onclick={discard}>
        Discard changes
      </button>
      <button
        type="button"
        class="btn preset-filled-primary-500"
        disabled={!canSave}
        onclick={save}
      >
        {saving ? 'Saving…' : 'Save'}
      </button>
    </footer>
  </div>

  <Modal bind:open={deleteOpen} title="Delete “{saved.name}”?" width="max-w-lg">
    <div class="card flex gap-3 border border-error-500/50 bg-error-500/10 p-3">
      <TriangleAlertIcon class="size-5 shrink-0 text-error-500" />
      <p class="text-sm">
        <strong>This cannot be undone.</strong> The binder, the {plural(impact.placed, 'card')}
        placed in it and the {plural(trayCount, 'card')} in its tray are deleted for good.
      </p>
    </div>

    <footer class="flex justify-end gap-2">
      <button
        type="button"
        class="btn preset-tonal"
        disabled={deleting}
        onclick={() => (deleteOpen = false)}
      >
        Cancel
      </button>
      <button
        type="button"
        class="btn preset-filled-error-500"
        disabled={deleting}
        onclick={confirmDelete}
      >
        <Trash2Icon class="size-4" />
        <span>{deleting ? 'Deleting…' : 'Delete binder'}</span>
      </button>
    </footer>
  </Modal>
{/if}
