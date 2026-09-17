<script lang="ts">
  import XIcon from '@lucide/svelte/icons/x'
  import { Dialog, Portal } from '@skeletonlabs/skeleton-svelte'
  import type { Snippet } from 'svelte'

  interface Props {
    /** Whether the modal is open. Bindable. */
    open?: boolean
    /** Heading shown at the top of the modal. */
    title?: string
    /** Max width utility for the modal. */
    width?: string
    /** Additional classes for the modal panel. */
    class?: string
    /** Modal body content. */
    children?: Snippet
  }

  let {
    open = $bindable(false),
    title = '',
    width = 'max-w-3xl',
    class: classes = '',
    children,
  }: Props = $props()

  const animation =
    'transition transition-discrete opacity-0 -translate-y-4 starting:data-[state=open]:opacity-0 starting:data-[state=open]:-translate-y-4 data-[state=open]:opacity-100 data-[state=open]:translate-y-0'
</script>

<Dialog {open} onOpenChange={(e) => (open = e.open)}>
  <Portal>
    <Dialog.Backdrop class="fixed inset-0 z-50 bg-surface-50-950/60 backdrop-blur-md" />
    <Dialog.Positioner class="fixed inset-0 z-50 flex items-center justify-center p-4">
      <Dialog.Content
        class="card preset-glass-surface border border-surface-200-800/50 w-full {width} p-4 space-y-4 shadow-xl {classes} {animation}"
      >
        <header class="flex items-center justify-between">
          <Dialog.Title class="text-lg font-bold">{title}</Dialog.Title>
          <Dialog.CloseTrigger class="btn-icon hover:preset-tonal" aria-label="Close">
            <XIcon class="size-4" />
          </Dialog.CloseTrigger>
        </header>

        {@render children?.()}
      </Dialog.Content>
    </Dialog.Positioner>
  </Portal>
</Dialog>
