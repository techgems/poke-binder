# PokeBinder (web)

The ASP.NET Core host for PokeBinder — Razor Pages for the marketing and account
surface, plus a Svelte SPA mounted inside the binder builder page.

Targets `net10.0`. Referenced projects: `PokeBinder.Features`,
`PokeBinder.Binder.DbContext`, `PokeBinder.Binder.Users`.


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

The bundle was last generated with Tailwind v4.1.18 with a local installation of Tailwind CLI.

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
