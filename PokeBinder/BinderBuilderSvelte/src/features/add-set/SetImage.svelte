<!--
  A set's image in a wide frame -- set logos are wide -- or a placeholder in the same frame when the
  set has none, or its image fails to load. Decorative: the set's name is always beside it.
-->
<script lang="ts">
  import ImageIcon from '@lucide/svelte/icons/image'

  interface Props {
    url: string | null | undefined
    /** Sizing for the frame. */
    class?: string
  }

  let { url, class: classes = '' }: Props = $props()

  // The url that failed, rather than a flag, so a different set's image gets its own try.
  let failed = $state<string | null>(null)
</script>

<span
  class="flex aspect-[2/1] items-center justify-center overflow-hidden rounded-base bg-surface-500/15 {classes}"
  aria-hidden="true"
>
  {#if url && failed !== url}
    <img
      src={url}
      alt=""
      loading="lazy"
      class="size-full object-contain p-1"
      onerror={() => (failed = url ?? null)}
    />
  {:else}
    <ImageIcon class="size-6 opacity-30" />
  {/if}
</span>
