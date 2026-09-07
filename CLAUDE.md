# PokeBinder — agent instructions

Scoped to the styling pipeline for now. Everything below is load-bearing for the app's visual
identity; get it wrong and pages render unstyled rather than failing loudly.

## Two stylesheets, two design systems

The app runs two separate Tailwind builds. They are not interchangeable and must not be merged.

| Stylesheet | Built from | Vocabulary | Used by |
| --- | --- | --- | --- |
| `PokeBinder/wwwroot/css/site.tailwind.css` | `PokeBinder/wwwroot/input.css` | plain Tailwind (`bg-neutral-900`, `text-white`) | `_Layout`, `_AdminLayout`, `_LandingLayout` — the landing and admin pages, alongside Pines UI |
| `PokeBinder/wwwroot/css/binder-shell.css` | `PokeBinder/BinderBuilderSvelte/src/app.css` | Tailwind + Skeleton 5 + the `PokeBinder` theme (`preset-tonal-primary`, `bg-surface-100-900`, `card`, `input`) | `_BinderLayout` — the binder workspace and every Razor page under it |

**Write markup in the vocabulary of the layout the page uses.** A Skeleton preset on a landing page
and a `bg-neutral-900` in the binder area both produce an element with no styling at all, because
the class does not exist in the stylesheet that page loads.

## Rebuilding the binder shell stylesheet

```bash
npm run build:shell-css --prefix PokeBinder/BinderBuilderSvelte
```

Run it **whenever a class is added or changed in a `.cshtml` that `_BinderLayout` serves**, and
before any deploy. `watch:shell-css` does the same on a watcher for a working session.

The failure mode is silent: Tailwind only emits utilities it has seen in a scanned source file, so
a class that exists in no scanned file simply is not in the stylesheet. The page renders, unstyled,
with nothing in the console.

`src/app.css` carries an `@source` line per Razor tree for exactly this reason — Vite's own scan
stops at the SPA folder, so without them no Razor class would ever be generated:

```css
@source '../../Pages/**/*.cshtml';       /* pages, layouts, partials */
@source '../../Components/**/*.cshtml';  /* Static Components */
```

**Add a matching `@source` before putting binder markup in any new directory.** Missing one is how
`max-w-lg` on the new-binder modal silently never got generated, leaving the dialog full-width with
no cap: the class was written, scanned by nothing, and emitted nowhere.

## Why the shell sheet exists at all

`_BinderLayout`'s chrome — the app bar, the background blobs, the theme — is Skeleton markup. That
CSS ships inside the Svelte bundle, and the bundle only reaches pages that boot the SPA. A Razor
page on this layout that does not render `<dev-vite-scripts>` / `<prod-vite-scripts>` would
therefore have no stylesheet at all. `binder-shell.css` is the same sheet compiled to a plain file
so those pages are styled without booting the SPA. `/Binder` ends up with both copies; that costs a
download and changes nothing else.

## Rules for the binder area

- **No Pines UI** in `_BinderLayout` or anything under it — no `<pines-*>` tag helpers, no
  `<pines-css />`, no `<pines-scripts />`. Pines belongs to the landing and admin layouts.
- **The theme and dark mode belong on `<html>`** (`class="dark" data-theme="PokeBinder"`). Skeleton
  paints the page from `html { background-color: light-dark(--color-root-bg-light,
  --color-root-bg-dark) }` and reads both properties off the `[data-theme]` element, so putting the
  theme on any inner element leaves that rule with nothing to resolve and the page unpainted.
- **Do not add a third Tailwind build** for the binder area. If a new Razor section needs Skeleton,
  put it on `_BinderLayout` and rebuild the shell sheet.
- Production also needs `npm run build` for the SPA bundle. That build and the shell stylesheet are
  separate artifacts — building one does not refresh the other.
