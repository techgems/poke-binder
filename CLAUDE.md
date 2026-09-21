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

## Rebuilding the site stylesheet

**This sheet is built by the standalone Tailwind CLI installed on the machine, not by npm.** It is
on `PATH` as `tailwindcss` (here: `C:\Tailwind\tailwindcss.exe`), and it is a self-contained
binary: it carries its own copy of Tailwind, so nothing next to `input.css` has to be installed and
the version it reports is the version that built the sheet.

```bash
tailwindcss --cwd PokeBinder -i wwwroot/input.css -o wwwroot/css/site.tailwind.css
```

Run it **whenever a class is added or changed in a file `_Layout`, `_AdminLayout` or
`_LandingLayout` serves**. `-w` adds a watcher. Same silent failure as the shell sheet below, same
cure.

Three things about that command are load-bearing:

- **`--cwd PokeBinder`, not the repo root.** Tailwind scans the working directory on top of the
  `@source` globs, and what that sweeps in changes the output. From the app project it reaches
  `Components/Binder/BinderIcon.cshtml.cs` — icon sizes like `h-5` live in a `.cs` file, which the
  `*.cshtml` globs do not match and which nothing else would generate. From the repo root it also
  sweeps `.claude/skills/**/*.md`, which pads the sheet with several hundred utilities out of
  library documentation. Both builds succeed and neither says anything.
- **`@import "tailwindcss";` has to stay in `input.css`.** The `@apply` rules at the foot of that
  file cannot resolve a utility without it, so a build missing the line fails outright. It was
  dropped once already, in `0e6e0c8`, which left the sheet unbuildable by any route until it came
  back.
- **Do not add a `package.json` to `PokeBinder/`** to get an npm CLI for this. There is no npm
  project for the app and this sheet does not want one; an npm-installed CLI is also a different
  Tailwind version from the standalone binary, which rewrites the whole artifact.

The binder shell sheet below is the other way round -- it *is* an npm script, because it needs the
Skeleton packages from the SPA's `node_modules`, which a standalone binary cannot resolve. Two
sheets, two build tools, and neither command works for the other file.

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


## Rules for the admin pages

**The admin pages are never used on a phone, so do not design them mobile first.** Anything on
`_AdminLayout` is a desktop tool for the person running the catalog. Lay it out for a wide screen
and stop there: a table may be as wide as it needs to be without a scrolling container, a row of
controls need not wrap, and a narrow-screen variant of an admin screen is work nobody will ever
see. Responsive classes are not banned -- they are simply not a requirement, and a layout that
only reads well above `lg` is finished.

What is already in the layout is not an argument against this. `<pines-sidebar>` ships a drawer
and a toggle for small screens, and the bar keeps that toggle, because removing it would mean
rewriting Pines' sidebar rather than saving anybody anything.

## Rules for controllers

**A slice's `Request` is also the body its controller receives.** Bind it straight off the wire and
hand it to the validator:

```csharp
public async Task<ActionResult<SaveBinderCards.Response>> SaveCards(
    int binderId,
    [FromBody] SaveBinderCards.Request body,
    CancellationToken ct)
{
    var request = body with { BinderId = binderId };
```

Do not define a per-endpoint body type beside it. One shape means the endpoint, the slice, the
validator and the tests are all talking about the same object, and a field added to the contract
cannot reach the slice while the controller's own copy of the shape quietly ignores it.

- **The route still owns any id in it.** The `Request` carries `BinderId` and the URL carries it
  too, so the URL wins: `body with { BinderId = binderId }`, before anything reads the request.
  What the caller put in that field is not part of what the endpoint asks for, and saying so in the
  `<param>` doc is worth the line.
- **Normalise nothing else.** No `?? []` on the lists. The request that reaches the validator is
  exactly what was deserialised, which is the point of binding it directly, and a coalesce turns
  "the client sent nonsense" into a save of an empty page.
- **Which puts the null on the validator**, and it is not free: FluentValidation's default cascade
  is `Continue`, so `.NotNull()` records its failure and the next rule in the chain dereferences
  the null anyway. A non-nullable `IReadOnlyList<T>` with an `= []` default does not save you --
  `System.Text.Json` will write a null over it. So **every list rule gets
  `.Cascade(CascadeMode.Stop)` before its `NotNull`**, and **every `CustomAsync` that reads a list
  returns early when it is null**, leaving the message to the rule that owns it. Without both, a
  body carrying `"cards": null` is a 500 instead of the 400 the validator had already decided on.
  There is a test per list for this in `SaveBinderChangesTests`; copy it for a new slice.

## Rules for tests

`PokeBinder.Features.Tests` (xUnit) covers the slices. Run it with:

```bash
dotnet test PokeBinder.Features.Tests/PokeBinder.Features.Tests.csproj
```

**A test never touches a real database.** Build the context on the EF Core in-memory provider, with
a database name unique to the fixture:

```csharp
var options = new DbContextOptionsBuilder<TcgCatalogDbContext>()
    .UseInMemoryDatabase($"catalog-{Guid.NewGuid()}")
    .Options;
```

**Not Sqlite, and that includes `Data Source=:memory:`.** The point is not only where the bytes end
up — it is that a test with a connection string in it is one careless edit away from naming
`Databases/TcgCatalog.db`, and a test suite that can write to the real catalog is one that will,
eventually, on the run nobody was watching. The in-memory provider has no connection string to get
wrong.

Two things follow from it:

- **Query translation is not covered.** The in-memory provider is not relational, so a LINQ query
  that no provider could turn into SQL still passes here. Anything SQL-shaped -- a new `EF.Functions`
  call, a group-by, a projection that leans on the database -- has to be confirmed by running the
  app, not by a green test run.
- **Give every fixture its own database name and its own `IMemoryCache`.** Shared state between
  tests is what makes a suite order-dependent, and a cached filter group leaking into the next test
  is exactly the kind of failure that only appears in CI.

Tests state behaviour, not implementation: go through the slice's `Handler` with a `Request` and
assert on the `Response`. That is what lets a slice be rewritten -- and one is due, see
NEXT-STEPS.md -- without the tests having to be rewritten with it.

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
