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

## Rules for code shared between slices

`PokeBinder.Features` is split into sections -- `Binder`, `CardAdmin`, `CardSearch` -- and each
section is a folder of vertical slices. **Code that two or more slices of a section need goes in
that section's `Shared` folder**, never in one of the slices that happen to use it:

```
PokeBinder.Features/Binder/Shared/Arrangement/   namespace PokeBinder.Features.Binder.Shared.Arrangement
```

`Binder/Shared/Arrangement` is the example: the sort and the pocket rewrite that `ReorderBinder` and
`UpdateBinder` both run, and the validation rules for a list of sort criteria.

- **A slice never reaches into another slice's folder.** When a second slice needs something that
  lives inside a slice, move it to `Shared` first, then use it from both. Left where it was, the
  first slice cannot be rewritten without breaking one that only borrowed from it.
- **`Shared` holds no slices.** No `Request`, no `Handler`, no endpoint of its own: only what slices
  call. Group it in a subfolder named for what it does (`Arrangement`), not one file per caller.
- **Keep it `internal`** unless something outside `PokeBinder.Features` needs it. A model that
  travels on the wire is the exception, and goes in `Shared` the same way.
- **One slice is not shared.** Code used by a single slice stays in that slice, however general it
  looks; it moves the day a second slice needs it.
- **A slice may split its own logic into a subfolder of its own**, as
  `CardSearch/GetSearchStarterFilters/Utils` does. That is the slice organising itself, not shared
  code: it stays inside the slice, no other slice uses it, and it does not move to `Shared`.
- **General code for every section goes in `PokeBinder.Features/Utils`**, not in a `Shared`
  folder. `Shared` is per logical unit -- the binder's slices, the search's -- and `Utils` is for
  code that is not about any one of them (`SqlLiteLikePatterns`), whichever slices happen to call
  it today.

## Rules for catalog data

**A migration is for the schema and for data the app cannot start without. Catalog data is
neither.** `PokeBinder.Migrations` runs under DbUp, once per database, journalled by script name
and never re-run -- which is the right shape for a table definition and the wrong shape for values
that get corrected. Which rarities a set has, what a rarity is worth, which cards a set contains:
all of it is important, none of it is essential in the sense that matters here, and all of it
changes without the schema changing at all.

So **catalog data goes in `Scripts/`**, as plain SQL run by hand:

| | Migration | Script in `Scripts/` |
| --- | --- | --- |
| Belongs there | tables, columns, indexes; the seeded rows of `filterCacheStamps`, without which the filters have no stamp to serve | rarity weights, corrections to loaded cards, anything derived from the card catalog |
| Runs | once, by DbUp, on every database | by hand, as often as it needs to |
| If it is wrong | a new migration to correct it | edit the file and run it again |

**Seed scripts must be re-runnable** Every statement is an upsert on `(setId, rarity)`, so running it twice
  changes nothing the first run did not, and it resolves sets by code rather than by id, because
  ids differ between databases. SeedRarityWeights.sql is an example of this.


**Deleting an applied migration is safe and leaves a stale row in `SchemaVersions`.** DbUp
journals by name and only ever asks whether a name has run, so a name it no longer has is simply
never considered. The eleven above were deleted after their values were captured; the journal in
the existing catalog still lists them and nothing reads it.

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

**Aspire is the only way to run the app.** `PokeBinder.AppHost` composes it with everything it
depends on at runtime, in the shape prod will have: one EC2 running nginx in front of the app, and
CloudFront in front of a public S3 bucket of card art. Never `dotnet run` `PokeBinder.csproj` on
its own: `CardImages:BaseUrl` comes from the AppHost, so the app refuses to start without it, and a
page reached around the edge is not the page prod serves.

It needs Docker Desktop running. Start it with the `pokebinder` configuration in
`.claude/launch.json`, the only one there, or by hand:

```bash
dotnet run --project PokeBinder.AppHost/PokeBinder.AppHost.csproj
```

| Resource | Stands in for | Where |
| --- | --- | --- |
| `pokebinder-proxy` | the EC2's nginx | **https://pokebinder.dev.localhost:8443** — the address to use |
| `pokebinder-app` | the app | http://localhost:5076, as a project, not a container |
| `cdn-nginx` | CloudFront | http://cdn.pokebinder.localhost:8081, `CardImages:BaseUrl` for the app |
| `minio` | S3 | bucket `card-images`; ports and console are in the dashboard |
| `seeder` | — | one-shot: copies `C:\ImagesTCG` into the bucket, then exits |

The Aspire dashboard is on http://localhost:15076; the console prints the login link.

- **The app runs as a project, so it runs in Development**: `Program.cs` calls
  `RunViteDevServer()`, which brings up Vite for the SPA with it. Vite picks whatever port is free
  (5173 upward) and the tag helpers resolve it, so the port it prints is not one to hard-code
  anywhere. Browse through `pokebinder.dev.localhost:8443`, not port 5076.
- **TLS ends at `pokebinder-proxy`**, as it will on the EC2, with the ASP.NET Core dev certificate
  that Aspire hands the container. That certificate covers `*.dev.localhost` and not
  `*.localhost`, which is why the host has the extra `dev` label. Port 8080 only answers with a
  redirect to 8443. The app behind the proxy stays on plain HTTP and learns the scheme from
  `X-Forwarded-Proto`. A browser that warns about the certificate means it is not trusted on this
  machine yet: `dotnet dev-certs https --trust`. The certificate APIs are experimental in Aspire
  13.6, hence the `#pragma` around the proxy in the AppHost.
- **The nginx configs are templates**: `aspire/proxy/` and `aspire/cdn/` hold
  `default.conf.template`, and the AppHost fills in the upstream addresses Aspire assigns. Edit the
  template, not the file nginx renders from it inside the container.
- **Card art shows broken until the seeder has finished.** The first run uploads the whole of
  `C:\ImagesTCG` (about 2 GB); after that, `aws s3 sync` sends only what is new. MinIO's data is
  in the `pokebinder-minio-data` volume, so the upload happens once, not on every start. The folder
  is `Seeder:CardImagesFolder` in the AppHost's `appsettings.json`.
- **The app never serves card art itself.** A card's `imageUrl` is its object key in the bucket
  (`Ruby & Sapphire/83475_in_1000x1000.jpg`), and `CardImages:BaseUrl`, which the AppHost sets to
  `cdn-nginx`, is the CDN it is appended to. The ETL still writes file paths:
  **re-run `Scripts/CardImagePathsToObjectKeys.sql` after every ETL load.**
- **MinIO's image is Chainguard's (`cgr.dev/chainguard/minio`)**, because MinIO no longer publishes
  images to Docker Hub or Quay. It only starts as root, hence the `--user 0`.
- **The MinIO password is generated on the first run** and kept in the AppHost's user secrets
  (`Parameters:minio-password`). The volume is formatted with it, so a cleared secret leaves a
  volume nothing can open: remove the volume along with it.

**Never touch `manifest.dev.json`.** The Vite dev server rewrites
`wwwroot/BinderBuilderSvelte/manifest.dev.json` every time it starts, with whatever port it got.
A changed port in that file is not a change anybody made, so do not edit it, revert it or report
it. It is generated, and `.gitignore` keeps it out of the repo.

**Stop it again before the turn is over.** Any prompt an agent started the AppHost for is a prompt
that ends by stopping it — no leaving it up for the next one. It holds ports 8443, 8080, 8081, 5076
and a Vite port, so one left behind is what makes the next start fail, and its Vite child keeps
rebuilding against a session nobody is watching. A preview server is stopped by the same tool that
launched it, and Ctrl+C takes the containers down with the AppHost. A killed AppHost process does
not: `pokebinder-proxy-*`, `cdn-nginx-*`, `minio-*` and Aspire's `tunnel-proxy-*` are left running,
and need a `docker rm -f`. The MinIO volume stays either way.

### Signing in

Every binder page is behind auth, and the sign-in is passwordless — there is no password to type,
and in Development there is no mail server either. The loop is:

1. Submit an email address on `https://pokebinder.dev.localhost:8443/Account/Login`. An unknown
   address registers an account on the spot, which is intended: dev accounts are meant to be cheap.
2. `LoginModel.GenerateTokenAndLink` writes the one-time link to **`PokeBinder/passwordless.txt`**
   instead of sending it. The file is gitignored and is overwritten on every attempt, so the link in
   it is always the most recent one, and it points at the proxy host the request came through.
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

### The image that ships

`PokeBinder/Dockerfile` is the image that ships, built from the repository root. It is for the
deploy, not a way to run the app locally:

```bash
docker build -f PokeBinder/Dockerfile -t pokebinder-app .
```

It builds the SPA bundle and the binder shell sheet itself. `site.tailwind.css` it takes as
committed, since the standalone Tailwind CLI is not in the image, so rebuild that sheet before
building the image. Run it with the host's `Databases/` folder mounted at `/Databases` and
`CardImages__BaseUrl` set to the CDN; without the latter the app refuses to start.

## Reporting back

**Keep the report on finished work short, and only as long as the change.** Say what changed and
anything the reader has to act on or decide -- a failed check, an open question, a judgement call
they may disagree with. Leave out the walk through how it was done, measurements that only confirm
the request was met, and restating the request. A one-line change gets a one-line report.

**No fluff.** Every sentence must carry information the reader does not already have. Cut
preamble ("I've gone ahead and…", "As requested…"), transitions ("Now let me…", "Next I will…"),
self-congratulation ("Perfect!", "Great, that worked"), restated conclusions, and sign-offs. Lists
over prose where a list fits. The report has two jobs: let the reader skim what happened and keep
the token count down -- both are undermined by narrative connective tissue.
