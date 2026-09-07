<!--
  The binder as it falls open, with the tray along its foot. Cards move between the two by drag: out
  of the tray into a pocket, back out of a pocket into the tray, and between pockets. What is still
  missing is the saving: nothing here is written back yet.
-->
<script lang="ts">
  import BinderPage from './BinderPage.svelte'
  import BinderPageNav from './BinderPageNav.svelte'
  import BinderTrayStrip from './BinderTrayStrip.svelte'
</script>

<!-- A column, not two: the page is what the eye is on here, so the tray gives up the side of the
     screen and takes a strip along the bottom that it can also fold away. -->
<div class="flex min-h-0 flex-1 flex-col gap-3">
  <!-- No frame and no header. A border, its padding and a "Page 1" caption cost three bands of
       height across the top of the panel, and every one of them came out of the page — which is
       the only thing in this view worth showing. The tray strip below keeps its border, so the two
       are still read apart. -->
  <section class="flex min-h-0 flex-1 flex-col gap-2 overflow-hidden">
    <div class="min-h-0 flex-1">
      <BinderPage />
    </div>

    <BinderPageNav />
  </section>

  <BinderTrayStrip />
</div>
