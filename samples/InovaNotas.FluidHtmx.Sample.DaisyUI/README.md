# FluidHtmx DaisyUI Sample

A sample ASP.NET Core application showcasing **Tailwind CSS + DaisyUI + HTMX** with build-time CSS compilation — no Node.js required.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- `curl` (pre-installed on most systems)

That's it. No Node.js, no npm, no npx.

## How it works

This sample uses two key pieces to get Tailwind CSS + DaisyUI running without a Node.js toolchain:

| Tool | What it does |
|------|-------------|
| [AustinS.TailwindCssTool](https://www.nuget.org/packages/AustinS.TailwindCssTool) | .NET local tool that downloads and runs the **standalone Tailwind CSS CLI** binary |
| [DaisyUI standalone bundles](https://daisyui.com/docs/install/standalone/) | `daisyui.mjs` + `daisyui-theme.mjs` files that plug directly into the standalone CLI via `@plugin` syntax |

The `.csproj` has two MSBuild targets that run automatically on every build:

1. **`DownloadDaisyUI`** — downloads `daisyui.mjs` and `daisyui-theme.mjs` from GitHub releases (only if they don't already exist)
2. **`TailwindBuild`** — restores the .NET tool and runs `tailwindcss build` to compile `Styles/app.css` into `wwwroot/css/app.css`

```
dotnet build  →  downloads daisyUI (if needed)  →  compiles Tailwind CSS  →  builds the project
```

## Quick start

```bash
# From the repository root
dotnet build
dotnet run --project samples/InovaNotas.FluidHtmx.Sample.DaisyUI
```

Browse to `https://localhost:5001` (or the URL shown in the console).

## Development with watch mode

For live CSS recompilation during development, run two terminals side by side:

**Terminal 1 — Tailwind watch:**
```bash
cd samples/InovaNotas.FluidHtmx.Sample.DaisyUI
dotnet tool restore
dotnet tool run tailwindcss build -t v4.1.8 -i Styles/app.css -o wwwroot/css/app.css --watch
```

**Terminal 2 — App with hot reload:**
```bash
dotnet watch run --project samples/InovaNotas.FluidHtmx.Sample.DaisyUI
```

Tailwind recompiles CSS whenever you change templates or `Styles/app.css`. The app server picks up the new file automatically.

## Overriding the Tailwind CSS version

The Tailwind CLI version is controlled by the `TailwindVersion` MSBuild property in the `.csproj`:

```xml
<PropertyGroup>
    <TailwindVersion>v4.1.8</TailwindVersion>
</PropertyGroup>
```

You can override it in three ways:

### 1. Edit the `.csproj`

Change the value directly in the `<TailwindVersion>` property.

### 2. Override from the command line

```bash
dotnet build -p:TailwindVersion=v4.2.0
```

### 3. Override via `Directory.Build.props`

Create a `Directory.Build.props` file in the project or repo root to set it across all projects:

```xml
<Project>
    <PropertyGroup>
        <TailwindVersion>v4.2.0</TailwindVersion>
    </PropertyGroup>
</Project>
```

The tool automatically downloads the requested version if it's not already cached.

## Updating DaisyUI

The `DownloadDaisyUI` MSBuild target only runs when `Styles/daisyui.mjs` doesn't exist. To update to a newer version:

```bash
# Delete the cached files so the next build re-downloads them
rm Styles/daisyui.mjs Styles/daisyui-theme.mjs

# Rebuild — fetches the latest release
dotnet build
```

To pin a specific DaisyUI version instead of `latest`, edit the download URLs in the `.csproj`:

```xml
<Exec Command="curl -sLo Styles/daisyui.mjs https://github.com/saadeghi/daisyui/releases/download/v5.5.19/daisyui.mjs" />
<Exec Command="curl -sLo Styles/daisyui-theme.mjs https://github.com/saadeghi/daisyui/releases/download/v5.5.19/daisyui-theme.mjs" />
```

## Project structure

```
InovaNotas.FluidHtmx.Sample.DaisyUI/
├── Layouts/
│   └── DaisyLayout.cs              # Layout definition (maps to "daisy" template)
├── Providers/
│   └── DaisyLayoutDataProvider.cs   # Injects nav_items, page_title, render time, etc.
├── Styles/
│   ├── app.css                      # Tailwind input CSS with DaisyUI plugins
│   ├── daisyui.mjs                  # (downloaded at build, git-ignored)
│   └── daisyui-theme.mjs            # (downloaded at build, git-ignored)
├── Templates/
│   ├── layouts/
│   │   └── daisy.liquid             # HTML5 layout with navbar, footer, HTMX script
│   ├── pages/
│   │   ├── home/index.liquid        # Hero section + "Load Features" HTMX button
│   │   └── about/index.liquid       # Stats, steps, and card components
│   └── partials/
│       └── feature-cards.liquid     # Cards loaded on-demand via HTMX
├── wwwroot/
│   └── css/
│       └── app.css                  # (generated output, git-ignored)
├── Program.cs                       # Routes, middleware, FluidHtmx config
├── .gitignore                       # Ignores build artifacts
└── InovaNotas.FluidHtmx.Sample.DaisyUI.csproj
```

## Key design points

### Custom theme

The `Styles/app.css` defines a custom `inovanotas` theme using `@plugin "./daisyui-theme.mjs"`. Colors are specified in `oklch()` for perceptual uniformity. Edit the color variables in `app.css` to customize the theme.

### HTMX navigation

Navbar links use `hx-get`, `hx-target="#main-content"`, `hx-swap="innerHTML"`, and `hx-push-url="true"` for SPA-like navigation without JavaScript. Full page loads still work when HTMX is not available.

### Partial rendering

The `/partials/features` endpoint uses `RenderPartialAsync` to return just the HTML fragment — no layout wrapper. This is what the "Load Features" button targets via HTMX to load cards on demand.

### HTMX detection

When a request includes the `HX-Request` header (sent automatically by HTMX), `FluidViewRenderer` returns only the page content without the layout. This means clicking a navbar link swaps just the `<main>` content, not the full page.

### Render time

A middleware starts a `Stopwatch` per request. The `DaisyLayoutDataProvider` reads the elapsed time and exposes it as `render_time_ms` in the footer.

### Build artifacts are git-ignored

`daisyui.mjs`, `daisyui-theme.mjs`, and `wwwroot/css/app.css` are all generated at build time and listed in `.gitignore`. Only the source files (`Styles/app.css`, templates, C# code) are committed.
