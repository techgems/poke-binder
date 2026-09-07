# poke-binder

## Styling

The app runs **two separate Tailwind builds**, one per design system. They are not interchangeable:
a class only exists in the sheet whose sources were scanned for it, so using the wrong vocabulary
renders an unstyled element rather than an error.

| Stylesheet | Built from | Vocabulary | Used by |
| --- | --- | --- | --- |
| `PokeBinder/wwwroot/css/site.tailwind.css` | `PokeBinder/wwwroot/input.css` | plain Tailwind | `_Layout`, `_AdminLayout`, `_LandingLayout` (landing + admin, with Pines UI) |
| `PokeBinder/wwwroot/css/binder-shell.css` | `PokeBinder/BinderBuilderSvelte/src/app.css` | Tailwind + Skeleton 5 + the `PokeBinder` theme | `_BinderLayout` (the binder workspace and its Razor pages) |

### Rebuilding the binder shell stylesheet

```bash
npm run build:shell-css --prefix PokeBinder/BinderBuilderSvelte
```

Run this after adding or changing a class in any `.cshtml` served by `_BinderLayout`, and before
deploying. Use `watch:shell-css` while working. Skipping it fails silently — the page renders with
the new markup unstyled and nothing in the console.

### Why it is a separate file

`_BinderLayout`'s chrome (app bar, background, theme) is Skeleton markup, and that CSS normally
ships inside the Svelte bundle — which only loads on pages that boot the SPA. `binder-shell.css` is
the same stylesheet compiled to a plain file so Razor-only pages on that layout are styled without
starting the SPA. `src/app.css` declares one `@source` per Razor tree — `../../Pages/**/*.cshtml` and
`../../Components/**/*.cshtml` — so Tailwind scans the Razor markup; Vite's own scan stops at the
SPA folder. Add another before putting binder markup in a new directory, or its classes will not be
generated.

The theme and dark mode are set on `<html>` in `_BinderLayout` (`class="dark"
data-theme="PokeBinder"`), because Skeleton resolves the page background from the `[data-theme]`
element — on any inner element, the root background rule has nothing to resolve.

Production also needs `npm run build` for the SPA bundle. It and the shell stylesheet are separate
artifacts; building one does not refresh the other.
