# FluidHtmx

A server-side rendering UI library for ASP.NET Core that combines [Fluid](https://github.com/sebastienros/fluid) (Liquid-compatible) templates with [HTMX](https://htmx.org/) for interactivity — no JavaScript framework required.

## Installation

```bash
dotnet add package InovaNotas.FluidHtmx
```

## Features

- **Fluid templates** — Liquid-compatible syntax rendered server-side
- **HTMX integration** — Custom tags, filters, middleware, and response helpers
- **Layout system** — Composable layouts with data providers
- **Asset pipeline** — Build-time Tailwind CSS with optional DaisyUI, cache busting in production
- **Behavioral components** — Toast, confirm dialog, and modal with HTMX interactions built-in
- **Developer experience** — Hot reload, error pages with source context, template caching

## Quick Start

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFluidHtmx(fluid =>
{
    fluid.TemplatesPath("Templates");
    fluid.DefaultLayout<MainLayout>();
});

var app = builder.Build();

app.UseStaticFiles();

app.MapGet("/", (IViewRenderer view, HttpContext ctx) =>
    view.RenderAsync(ctx, "pages/home/index"));

app.Run();
```

Define a layout:

```csharp
// Layouts/MainLayout.cs
public class MainLayout : LayoutDefinition
{
    public override string TemplateName => "main";
}
```

Create templates:

```liquid
<!-- Templates/layouts/main.liquid -->
<!DOCTYPE html>
<html lang="en">
<head>
    <title>My App</title>
    {% asset_css "/css/app.css" %}
</head>
<body>
    <main id="main-content">
        {{ content }}
    </main>
    {% hx_script %}
</body>
</html>
```

```liquid
<!-- Templates/pages/home/index.liquid -->
<h1>Welcome</h1>
<p>Hello, {{ name | default: "World" }}!</p>
```

## Configuration

### Full setup with Tailwind + DaisyUI

```csharp
builder.Services.AddFluidHtmx(fluid =>
{
    fluid.TemplatesPath("Templates");
    fluid.EnableHotReload(builder.Environment.IsDevelopment());
    fluid.Assets(assets => assets.EnableTailwind("v4.2.0"));
    fluid.DefaultLayout<DaisyLayout>();
    fluid.AddLayout<DaisyLayout, DaisyLayoutDataProvider>();
    fluid.EjectAllComponents();
});

var app = builder.Build();

app.UseStaticFiles();
app.UseHtmx();
```

### Multiple layouts

You can register multiple layouts, each with its own template and optional data provider. Set one as the default and use the others explicitly per route:

```csharp
builder.Services.AddFluidHtmx(fluid =>
{
    fluid.TemplatesPath("Templates");

    // Default layout — used by RenderAsync without a generic parameter
    fluid.DefaultLayout<MainLayout>();
    fluid.AddLayout<MainLayout, MainLayoutDataProvider>();

    // Additional layouts
    fluid.AddLayout<AdminLayout, AdminLayoutDataProvider>();
    fluid.AddLayout<MinimalLayout>();  // no data provider needed
});
```

Each layout is a class that extends `LayoutDefinition` and points to its own `.liquid` template:

```csharp
public class MainLayout : LayoutDefinition
{
    public override string TemplateName => "main";       // Templates/layouts/main.liquid
}

public class AdminLayout : LayoutDefinition
{
    public override string TemplateName => "admin";      // Templates/layouts/admin.liquid
}

public class MinimalLayout : LayoutDefinition
{
    public override string TemplateName => "minimal";    // Templates/layouts/minimal.liquid
}
```

### Layout data providers

Inject dynamic data into every layout render by implementing `ILayoutDataProvider<TLayout>`:

```csharp
public class MainLayoutDataProvider : ILayoutDataProvider<MainLayout>
{
    public Task<Dictionary<string, object>> GetDataAsync(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value ?? "/";

        return Task.FromResult(new Dictionary<string, object>
        {
            ["app_name"] = "My App",
            ["nav_items"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["label"] = "Home",
                    ["url"] = "/",
                    ["active"] = path == "/"
                }
            },
            ["current_year"] = DateTime.UtcNow.Year
        });
    }
}
```

Each layout can have its own data provider. The data is merged into the template context before rendering, so all variables are available in the layout's `.liquid` file.

### Rendering

```csharp
// Full page with default layout
app.MapGet("/", (IViewRenderer view, HttpContext ctx) =>
    view.RenderAsync(ctx, "pages/home/index"));

// Full page with a specific layout
app.MapGet("/admin", (IViewRenderer view, HttpContext ctx) =>
    view.RenderAsync<AdminLayout>(ctx, "pages/admin/dashboard"));

// Full page with model
app.MapGet("/users", (IViewRenderer view, HttpContext ctx) =>
    view.RenderAsync(ctx, "pages/users/index", new { users = userList }));

// Partial (no layout) — for HTMX fragments
app.MapGet("/partials/user-list", (IViewRenderer view) =>
    view.RenderPartialAsync("partials/user-list", new { users = userList }));
```

`RenderAsync` (without a generic parameter) uses the default layout. Use `RenderAsync<TLayout>` to render with a specific layout. When a request includes the `HX-Request` header, both variants automatically return only the page content (no layout), enabling seamless HTMX navigation.

## HTMX Tags

Custom Liquid tags for generating HTMX-enabled HTML:

| Tag | Description | Example |
|-----|-------------|---------|
| `hx_script` | Injects HTMX `<script>` tag | `{% hx_script %}` |
| `hx_link` | HTMX-enabled anchor | `{% hx_link "/about", target: "#main-content" %}About{% endhx_link %}` |
| `hx_button` | HTMX-enabled button | `{% hx_button "/api/save", method: "post" %}Save{% endhx_button %}` |
| `hx_form` | HTMX-enabled form | `{% hx_form "/api/users", method: "post" %}...{% endhx_form %}` |
| `hx_lazy` | Lazy-loaded content | `{% hx_lazy "/partials/stats" %}` |
| `hx_swap_oob` | Out-of-band swap | `{% hx_swap_oob "#notification" %}Updated!{% endhx_swap_oob %}` |
| `csrf_token` | Anti-forgery hidden input | `{% csrf_token %}` |
| `hx_indicator` | Loading spinner | `{% hx_indicator %}` |
| `asset_css` | CSS link with cache busting | `{% asset_css "/css/app.css" %}` |
| `asset_js` | Script with cache busting | `{% asset_js "/js/app.js" %}` |

### Tag parameters

```liquid
{% hx_link "/users", target: "#content", swap: "outerHTML", push_url: "true" %}
  View Users
{% endhx_link %}

{% hx_button "/users/1", method: "delete", confirm: "Are you sure?", class: "btn btn-error" %}
  Delete
{% endhx_button %}

{% hx_form "/users", method: "post", target: "#user-list", swap: "beforeend" %}
  <input name="name" />
  <button type="submit">Add</button>
{% endhx_form %}
```

## Filters

| Filter | Description | Example |
|--------|-------------|---------|
| `to_json` | Serialize to JSON | `{{ model \| to_json }}` |
| `hx_vals` | JSON for `hx-vals` | `{{ data \| hx_vals }}` |
| `active_class` | Active nav class | `{{ "/about" \| active_class: "btn-active" }}` |
| `pluralize` | Singular/plural | `{{ count \| pluralize: "item", "items" }}` |
| `append_query` | Add query param | `{{ "/users" \| append_query: "page", "2" }}` |
| `escape_attr` | HTML-encode for attributes | `{{ value \| escape_attr }}` |

## HTMX Middleware and Response Helpers

Enable the middleware to parse HTMX request headers:

```csharp
app.UseHtmx();
```

### HTMX request context

Access parsed HTMX headers in your endpoints:

```csharp
app.MapGet("/dashboard", (HttpContext ctx) =>
{
    var htmx = ctx.GetHtmxContext();

    if (htmx.IsHtmx)
    {
        // Partial update — HTMX request
    }

    // htmx.IsBoosted, htmx.Target, htmx.CurrentUrl, htmx.TriggerName, etc.
});
```

### Response helpers

Use response extensions in your endpoints:

```csharp
app.MapPost("/users", (HttpContext ctx) =>
{
    // ... create user
    ctx.Response.HxTrigger("user-created");
    ctx.Response.HxRetarget("#user-list");
    return Results.Ok();
});

app.MapDelete("/users/{id}", (HttpContext ctx, int id) =>
{
    // ... delete user
    ctx.Response.HxRedirect("/users");
    return Results.Ok();
});
```

Available response helpers: `HxLocation`, `HxPushUrl`, `HxRedirect`, `HxRefresh`, `HxReplaceUrl`, `HxReswap`, `HxRetarget`, `HxReselect`, `HxTrigger`, `HxTriggerAfterSettle`, `HxTriggerAfterSwap`.

## Behavioral Components

FluidHtmx ships only components that encapsulate actual behavior — HTMX interactions, auto-dismiss timers, dialog management. For presentational elements (alerts, badges, tables, etc.), use your CSS framework classes directly in your templates. DaisyUI already provides those as simple class names.

### Toast

Auto-dismissing notification returned from server endpoints. Uses `hx-on::load` to self-remove after the specified duration.

```liquid
{% render 'components/toast', message: 'Saved!', type: 'success', duration: 3000 %}
```

Parameters: `message`, `type` (info/success/warning/error), `dismissible` (bool, default: true), `duration` (ms, default: 3000)

Typical usage — return a toast partial from a POST endpoint:

```csharp
// Endpoint returns a toast as an HTMX response
app.MapPost("/users", async (IViewRenderer view, HttpContext ctx) =>
{
    // ... create user
    return view.RenderPartialAsync("partials/user-created");
});
```

```liquid
<!-- Templates/partials/user-created.liquid -->
<tr>...</tr>
{% render 'components/toast', message: 'User created!', type: 'success' %}
```

### Confirm Dialog

Native `<dialog>` with HTMX confirm action — no server round-trip to show the dialog.

```liquid
<button class="btn btn-error" onclick="document.getElementById('delete-user').showModal()">
  Delete
</button>
{% render 'components/confirm-dialog',
    id: 'delete-user',
    title: 'Delete user?',
    message: 'This cannot be undone.',
    confirm_url: '/users/1',
    confirm_method: 'delete' %}
```

Parameters: `id`, `title`, `message`, `confirm_url`, `confirm_method` (default: delete), `confirm_label` (default: Confirm), `cancel_label` (default: Cancel), `confirm_target` (hx-target)

### Modal

HTMX-loaded modal — fetches content from the server when opened via `hx-get` with `intersect` trigger.

```liquid
{% render 'components/modal',
    id: 'edit-user',
    title: 'Edit User',
    url: '/users/1/edit',
    trigger_label: 'Edit',
    trigger_class: 'btn btn-primary',
    size: 'lg' %}
```

Parameters: `id`, `title`, `url` (hx-get), `trigger_label`, `trigger_class` (default: btn), `size` (sm/md/lg)

### Ejecting components

Components are embedded in the library DLL. To customize them, eject to your project:

```csharp
builder.Services.AddFluidHtmx(fluid =>
{
    // Eject all components to Templates/components/
    fluid.EjectAllComponents();

    // Or eject individually
    fluid.EjectComponent("toast");
    fluid.EjectComponent("modal");
});
```

Ejected files are copied to `{TemplatesPath}/components/`. Local files take priority over embedded ones — edit freely. Existing files are never overwritten.

Ejecting also ensures Tailwind CSS can scan the component templates for class extraction via your `@source` directive.

## Asset Pipeline

### Tailwind CSS

FluidHtmx uses [AustinS.TailwindCssTool](https://github.com/AustinS/TailwindCssTool) — no Node.js required.

```csharp
fluid.Assets(assets => assets.EnableTailwind("v4.2.0"));
```

In development, a background service runs `tailwindcss watch` to rebuild CSS on changes. In production, assets get SHA256 cache-busted URLs automatically.

### DaisyUI

Add DaisyUI by downloading its bundles and configuring your `Styles/app.css`:

```css
@import "tailwindcss";

@source "../Templates/**/*.liquid";

@plugin "./daisyui.mjs" {
    themes: dark --default;
}
@plugin "./daisyui-theme.mjs";
```

See the DaisyUI sample for a complete setup with MSBuild targets that download DaisyUI automatically.

## Template Resolution

Templates are resolved in this order:

1. **Physical files** in your `TemplatesPath` directory (e.g. `Templates/`)
2. **Embedded resources** bundled in the FluidHtmx assembly

This means local files always take priority — you can override any built-in template by placing a file with the same path in your project.

## Hot Reload

Enable hot reload to automatically invalidate cached templates when `.liquid` files change:

```csharp
fluid.EnableHotReload(builder.Environment.IsDevelopment());
```

A background file watcher monitors your `TemplatesPath` directory and clears the template cache on create, update, rename, or delete events.

## Error Pages

In development, FluidHtmx renders detailed error pages for template issues:

- **Parse errors** — shows the template source with the error line highlighted
- **Not found errors** — lists the search locations that were checked

These pages are only active when `IsDevelopment()` is true and are handled by the `UseHtmx()` middleware.

## Project Structure

```
your-project/
├── Layouts/
│   └── MainLayout.cs              # Layout definition
├── Providers/
│   └── MainLayoutDataProvider.cs   # Layout data provider
├── Styles/
│   └── app.css                     # Tailwind input CSS
├── Templates/
│   ├── layouts/
│   │   └── main.liquid             # Layout template
│   ├── pages/
│   │   └── home/
│   │       └── index.liquid        # Page template
│   ├── partials/
│   │   └── user-list.liquid        # HTMX partial
│   └── components/                 # Ejected components (optional)
│       ├── toast.liquid
│       ├── confirm-dialog.liquid
│       └── modal.liquid
├── wwwroot/
│   └── css/
│       └── app.css                 # Generated CSS output
└── Program.cs
```

## Samples

The repository includes two sample applications:

- **Basic** (`samples/InovaNotas.FluidHtmx.Sample`) — Minimal setup with layouts and pages
- **DaisyUI** (`samples/InovaNotas.FluidHtmx.Sample.DaisyUI`) — Full-featured with Tailwind, DaisyUI, HTMX navigation, component showcase, and asset pipeline

Run a sample:

```bash
cd samples/InovaNotas.FluidHtmx.Sample.DaisyUI
dotnet run
```

## Requirements

- .NET 10.0+
- [Fluid.Core](https://www.nuget.org/packages/Fluid.Core) 2.31.0

## License

MIT
