# PokeBinder (web)

The ASP.NET Core host for PokeBinder — Razor Pages for the marketing and account
surface, plus a Svelte SPA mounted inside the binder builder page.

Targets `net10.0`. Referenced projects: `PokeBinder.Features`,
`PokeBinder.Binder.DbContext`, `PokeBinder.Binder.Users`.

## Running

```bash
dotnet run
```

Launch profiles live in `Properties/launchSettings.json`: `http` serves on
`http://localhost:5076`, `https` on `https://localhost:7018` (HTTP redirects to
HTTPS). In Development the app also maps OpenAPI and Swagger UI at
`/openapi/v1.json` and `/swagger`, and starts the Vite dev server automatically
via `app.RunViteDevServer()`.

Data comes from two SQLite files resolved relative to the project directory —
`../Databases/PokeBinder.db` (application/identity) and
`../Databases/TcgCatalog.db` (card catalog). Both connection strings are
required at startup; the app throws if either is missing.

## Styling — Tailwind CSS

Razor Pages are styled with Tailwind v4, compiled ahead of time with the
**standalone Tailwind CLI**. There is no `package.json` or Node toolchain for
this part of the project — the CLI is a self-contained executable.

Source is `wwwroot/input.css`; the generated bundle is
`wwwroot/css/site.tailwind.css`, which every layout links and which **is
committed to the repo**. Regenerate it after adding or changing any class in a
`.cshtml` file:

```bash
tailwindcss -i wwwroot/input.css -o wwwroot/css/site.tailwind.css
```

Run it from this directory (the paths are relative to the project root). To
rebuild continuously while working:

```bash
tailwindcss -i wwwroot/input.css -o wwwroot/css/site.tailwind.css --watch
```

The bundle was last generated with Tailwind v4.1.18. On this machine the
executable lives at `C:\Tailwind\tailwindcss.exe`; if `tailwindcss` is not on
your `PATH`, grab the standalone binary for your platform from the
[Tailwind releases](https://github.com/tailwindlabs/tailwindcss/releases) and
invoke it by full path.

`input.css` starts with `@import "tailwindcss";` followed by `@source`
directives pointing at `../Pages/**/*.cshtml` and `../Components/**/*.cshtml`.
Those two globs cover every Razor file in the app — if you add `.cshtml` files
outside `Pages/` or `Components/`, add a matching `@source` or their classes
will be silently dropped from the bundle.

Non-utility rules (`.code-cell`, `.validation-summary-valid`) also live in
`input.css`. Put shared CSS there rather than in `site.tailwind.css`, which is
overwritten on every build.

## Styling — Svelte SPA

`BinderBuilderSvelte/` is a separate Vite + Svelte 5 app with its own Tailwind
setup (`@tailwindcss/vite`, entry `src/app.css`). It builds into
`wwwroot/BinderBuilderSvelte/` and is wired to .NET through the `vite-dotnet`
plugin and `TechGems.ViteDotNet`; `appsettings.json` names the app via the
`ViteDotNet` key. It has its own `package.json` and does need Node:

```bash
npm install
npm run build
```

The two Tailwind builds are independent — regenerating `site.tailwind.css` does
not touch the SPA bundle, and vice versa.

## Layout of the project

| Path | Contents |
| --- | --- |
| `Pages/` | Razor Pages — landing, card search, binder, admin, `Account/` |
| `Pages/Shared/` | `_Layout`, `_LandingLayout`, `_AdminLayout` |
| `Components/` | Razor tag-helper components (hero rows, feature cards, header/footer, CTA button) |
| `wwwroot/input.css` | Tailwind source |
| `wwwroot/css/site.tailwind.css` | Generated Tailwind bundle (committed) |
| `BinderBuilderSvelte/` | Svelte SPA source |

UI components come from `TechGems.PinesUI` and `TechGems.StaticComponents`, both
registered as tag helpers in `Pages/_ViewImports.cshtml`.

## Authentication

Sign-in is passwordless: `/Account/Login` takes an email, creates the user if
they are new, and issues a one-time token consumed at `/Account/AuthCallback`.

Email delivery is not implemented yet — `Login.cshtml.cs` writes the login link
to `passwordless.txt` in the working directory instead. Open that file to
complete a sign-in locally.

Validation is server-side only. The jQuery Validation scripts that ship with the
default Razor Pages template have been removed; `asp-validation-for` and the
validation summary render on POST round-trips.
