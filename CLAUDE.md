# PokeBinder — agent instructions

Two things so far: the styling pipeline, which is load-bearing for the app's visual identity — get
it wrong and pages render unstyled rather than failing loudly — and how to get the app running and
signed in locally, which is the only way to look at the binder workspace at all.

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


## Running the app locally

`.claude/launch.json` holds the one configuration, `pokebinder`: `dotnet run` on
**http://localhost:5076** with the Development environment. Start it from there rather than by hand
— `Program.cs` calls `RunViteDevServer()` in Development, so the same command also brings up Vite
for the SPA and the two stay in step. Vite picks whatever port is free (5173 upward) and the tag
helpers resolve it, so the port it prints is not one to hard-code anywhere.

**Stop it again before the turn is over.** Any prompt an agent started the server for is a prompt
that ends by killing it — no leaving it up for the next one. It holds port 5076 and a Vite port, so
a server left behind is what makes the next `dotnet run` bind somewhere else or fail outright, and
its Vite child keeps rebuilding against a session nobody is watching. The Ctrl+C equivalent depends
on how it was started; a preview server is stopped by the same tool that launched it.

### Signing in

Every binder page is behind auth, and the sign-in is passwordless — there is no password to type,
and in Development there is no mail server either. The loop is:

1. Submit an email address on `/Account/Login`. An unknown address registers an account on the spot,
   which is intended: dev accounts are meant to be cheap.
2. `LoginModel.GenerateTokenAndLink` writes the one-time link to **`PokeBinder/passwordless.txt`**
   instead of sending it. The file is gitignored and is overwritten on every attempt, so the link in
   it is always the most recent one.
3. Open that URL. It lands on `/Account/AuthCallback`, which sets the cookie and drops you on the
   site signed in.

The token is single-use and short-lived; a stale `passwordless.txt` from an earlier session will not
work, so re-submit the email rather than reusing what is already on disk.

### Finding a binder

`/Binder` with no id renders the 404 page — the route needs a specific binder. `/my-binders` lists
the signed-in account's binders and links to each; go through it rather than guessing an id.

### Looking at the workspace

The workspace is the SPA: the Card Tray and Binder tabs both live inside it, and the tray strip is
on the Binder tab. Panels there open and close on a 200ms `slide` transition, so a screenshot taken
the instant after a click can still show the previous state. Give it a beat, or read the state out
of the DOM instead of the pixels — the latter is the more reliable check anyway.

One caveat when driving the page with browser automation: synthesised `Enter`/`Space` keypresses
arrive with an empty `event.key` and so never produce the activation click a real key press would.
A keyboard path that looks broken under automation is worth confirming with `element.click()`, which
is exactly what the browser does on activation, before believing it.
