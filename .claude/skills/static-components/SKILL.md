---
name: static-components
description: >
  Use this skill whenever the user asks to create, build, or work with UI components in an ASP.NET Core project using the TechGems.StaticComponents library (also known as Static Components). Trigger this skill for any request involving: creating a new Static Component, adding slots or child content to a component, writing a component's Razor view (.cshtml), wiring up scripts with static-script, integrating AlpineJS or HTMX with a component, or following the conventions of the HATS stack (HTMX + AlpineJS + TailwindCSS + Static Components). If the user mentions StaticComponent, RenderSlot, static-script, teleport-script, or JavascriptConvert, always use this skill.
---

# Static Components

Static Components (`TechGems.StaticComponents`) is a minimalistic ASP.NET Core library that lets you write reusable UI components backed by Razor views. It extends ASP.NET Core Tag Helpers and is designed to synergize with AlpineJS, HTMX, and TailwindCSS (the **HATS stack**).

- **NuGet**: `TechGems.StaticComponents` (v1.1.0+)
- **Requires**: .NET 8 or above
- **Docs**: https://static-components.techgems.net/

---

## Installation

```bash
dotnet add package TechGems.StaticComponents --version 1.1.0
```

In `_ViewImports.cshtml`, register tag helpers:

```cshtml
@addTagHelper *, YourRazorPagesProject.Web
@addTagHelper *, TechGems.StaticComponents
```

---

## Core Convention: Two-File Structure

Every component consists of **two co-located files** inside `Pages/` or `Views/`:

| File | Purpose |
|---|---|
| `HelloWorldComponent.cshtml.cs` | C# class inheriting `StaticComponent` |
| `HelloWorldComponent.cshtml` | Razor view using the class as `@model` |

### Naming & Tag Inference

By default (no `[HtmlTargetElement]`), the library **infers** the tag name and view path from the class name:

- Class `HelloWorldComponent` → tag `<hello-world-component>`
- View auto-discovered at `~/Pages/Components/HelloWorldComponent.cshtml` (must be next to the code-behind)
- Properties become kebab-case HTML attributes: `GreetMessage` → `greet-message`

```csharp
// ~/Pages/Components/HelloWorldComponent.cshtml.cs
using TechGems.StaticComponents;

namespace Sample.Pages.Components;

public class HelloWorldComponent : StaticComponent
{
    public HelloWorldComponent() { }

    public string GreetMessage { get; set; }
}
```

```cshtml
@* ~/Pages/Components/HelloWorldComponent.cshtml *@
@using Sample.Pages.Components
@model HelloWorldComponent

<div>Hello world! @Model.GreetMessage</div>
```

Usage:

```cshtml
<hello-world-component greet-message="Hello there!"></hello-world-component>
```

---

## Overriding Tag Name, View Route, or Attribute Names

Use `[HtmlTargetElement]` to set a custom tag name and pass a custom path to `base(...)`:

```csharp
using Microsoft.AspNetCore.Razor.TagHelpers;
using TechGems.StaticComponents;

namespace Sample.Views;

[HtmlTargetElement("hello-world")]
public class HelloWorldComponent : StaticComponent
{
    public HelloWorldComponent() : base("~/Views/HelloWorld.cshtml") { }

    [HtmlAttributeName("message")]
    public string GreetMessage { get; set; }
}
```

Usage:

```cshtml
<hello-world message="Hello there!"></hello-world>
```

---

## Child Content

Any HTML placed between a component's opening and closing tags becomes `@Model.ChildContent`:

```csharp
public class OutlinedButton : StaticComponent
{
    public OutlinedButton() { }
}
```

```cshtml
@model YourAssembly.Views.OutlinedButton

<button type="button" class="...">
    @Model.ChildContent
</button>
```

```cshtml
<outlined-button>
    <strong>Click Me!</strong>
</outlined-button>
```

- Check `Model.IsChildContentNullOrEmpty` to conditionally render fallback content.

---

## Slots

Slots are **named regions** of HTML within a component — separate from `ChildContent`. They are declared at call-site with `<slot name="...">` and rendered in the view via `@Model.RenderSlot("slotName")`.

### Component class (best practice: use `static readonly` constants for slot names)

```csharp
using TechGems.StaticComponents;

namespace YourAssembly.Views.Components;

public class SidebarComponent : StaticComponent
{
    public static readonly string MobileSlot = "mobileLinks";
    public static readonly string DesktopSlot = "desktopLinks";

    public SidebarComponent() { }
}
```

### Razor view

```cshtml
@using YourAssembly.Views.Components
@model SidebarComponent

<nav>
    <div id="mobile">
        @if (Model.IsSlotContentNullOrEmpty(SidebarComponent.MobileSlot))
        {
            <a href="#">Home (Fallback)</a>
        }
        else
        {
            @Model.RenderSlot(SidebarComponent.MobileSlot)
        }
    </div>
    <div id="desktop">
        @Model.RenderSlot(SidebarComponent.DesktopSlot)
    </div>
</nav>
```

### Usage

```cshtml
<sidebar-component>
    <slot name="@SidebarComponent.MobileSlot">
        <a href="#">Home</a>
    </slot>
    <slot name="@SidebarComponent.DesktopSlot">
        <a href="#">Home</a>
        <a href="#">About</a>
    </slot>
</sidebar-component>
```

> **Key rule**: Slots are popped out of `ChildContent` — they will NOT render unless `RenderSlot` is explicitly called. Both slots and `ChildContent` can coexist in the same component.

---

## Static Scripts

Static Components includes the `static-script` tag helper to manage JavaScript inside components. It solves two problems that ordinary Razor partials cannot: **deduplication** and **teleportation** of scripts.

Use `static-script` as an attribute on a `<script>` tag **inside a component's `.cshtml` view**. It does nothing when used outside a Static Component's execution context.

### Attributes

| Attribute | Purpose |
|---|---|
| `static-script` | (Required marker) Marks the script for Static Components processing |
| `render-once="Model"` | Renders the script only once, regardless of how many times the component is used on the page. Pass the view model (`Model`) so the type can be extracted. |
| `teleport-script` | Moves the script to the bottom of the page (near `<render-static-scripts />`), instead of rendering inline |
| `disable-render="true/false"` | Prevents the script from rendering at all (useful for HTMX partial requests) |

### Example: deduplicated + teleported script

```cshtml
@model MaskedInputComponent

<input asp-for="Model.InputExpression" class="autocomplete" data-mask />

<script static-script type="text/javascript" render-once="Model" teleport-script>
    var selector = document.querySelector("[data-mask]");
    var im = new Inputmask("(000)-000-0000");
    im.mask(selector);
</script>
```

### Add `<render-static-scripts />` to your layout

To output all teleported scripts at the bottom of the page:

```cshtml
@* ~/Pages/Shared/_Layout.cshtml *@
...
@await RenderSectionAsync("Scripts", required: false)
<render-static-scripts />
</body>
```

### Injecting server-side values into scripts: `JavascriptConvert`

Use `JavascriptConvert.SerializeObject()` to embed C# objects as JS Object Literals (not JSON strings, no `JSON.parse()` needed):

```cshtml
<script static-script type="text/javascript" teleport-script>
    Alpine.data('gallery', () => ({
        imageGallery: @(JavascriptConvert.SerializeObject(Model.ImageList)),
        // ...
    }))
</script>
```

> Uses Newtonsoft.Json internally. Apply `Newtonsoft.Json` attributes (not `System.Text.Json`) for serialization control. Always encode untrusted/user-supplied values to avoid XSS.

---

## AlpineJS Integration

Pass server-side values as AlpineJS `x-data` to make components interactive without writing imperative JS:

```csharp
public class CollapsibleSectionComponent : StaticComponent
{
    public CollapsibleSectionComponent() { }

    [HtmlAttributeName("is-open")]
    public bool IsOpen { get; set; } = false;
}
```

```cshtml
@using Sample.Views
@model CollapsibleSectionComponent
@{
    var isOpenJs = Model.IsOpen.ToString().ToLower();
}

<div x-data="{ open: @isOpenJs }">
    <div x-show="open">
        @Model.ChildContent
    </div>
    <button @@click="open = !open">Toggle</button>
</div>
```

```cshtml
<collapsible-section-component is-open="true">
    This content can be toggled on and off.
</collapsible-section-component>
```

> Use `@@` in Razor views to escape the `@` symbol when writing AlpineJS event handlers like `@@click`.

---

## Overriding `ProcessAsync` (Advanced)

Override `ProcessAsync` for custom pre-render logic (e.g., data fetching, conditional view selection). Call `base.RenderPartialView(output)` to trigger the default Razor rendering:

```csharp
[HtmlTargetElement("hello-world")]
public class HelloWorldComponent : StaticComponent
{
    public HelloWorldComponent() : base("~/Views/HelloWorld.cshtml") { }

    [HtmlAttributeName("render-alternate")]
    public bool RenderAlternateView { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (!RenderAlternateView)
        {
            await base.RenderPartialView(output);
        }
        else
        {
            var model = new ModelType() { Name = "Test" };
            await base.RenderPartialView("~/Views/DifferentView.cshtml", output, model);
        }
    }
}
```

- `output.Content` is set to the rendered partial output.
- `output.TagName` is set to `null` (no wrapping tag).

---

## Quick Reference Checklist

When creating a Static Component, always verify:

- [ ] Class inherits `StaticComponent` and lives in `Pages/` or `Views/`
- [ ] A corresponding `.cshtml` file exists next to the code-behind
- [ ] The `.cshtml` has `@model YourComponentClass` at the top
- [ ] `_ViewImports.cshtml` includes both `@addTagHelper` lines
- [ ] Slot names use `static readonly string` constants (not magic strings)
- [ ] `<render-static-scripts />` is in `_Layout.cshtml` when using `teleport-script`
- [ ] `@@` is used instead of `@` for AlpineJS event bindings in Razor views
