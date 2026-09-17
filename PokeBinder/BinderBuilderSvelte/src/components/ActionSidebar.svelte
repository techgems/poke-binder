<script lang="ts">
  import type { Snippet } from 'svelte'

  interface Props {
    /** The action buttons to render inside the sidebar. */
    children?: Snippet
    /** Destructive actions, rendered below a divider at the bottom of the sidebar. */
    danger?: Snippet
    /** Additional classes for the sidebar element. */
    class?: string
  }

  let { children, danger, class: classes = '' }: Props = $props()
</script>

<!-- Full-height sticky rail so the sidebar stays vertically centered in the viewport. -->
<div class="sticky top-0 self-start h-dvh flex items-center">
  <aside
    class="flex flex-col items-center gap-3 p-3 rounded-container preset-glass-surface border border-surface-200-800/50 shadow-xl {classes}"
  >
    {@render children?.()}

    {#if danger}
      <hr class="hr w-full" />
      {@render danger()}
    {/if}
  </aside>
</div>
