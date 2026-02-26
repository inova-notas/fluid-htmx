using System.Diagnostics;
using InovaNotas.FluidHtmx.Extensions;
using InovaNotas.FluidHtmx.Rendering;
using InovaNotas.FluidHtmx.Sample.DaisyUI.Layouts;
using InovaNotas.FluidHtmx.Sample.DaisyUI.Providers;

var builder = WebApplication.CreateBuilder(args);

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

app.Use(async (context, next) =>
{
    context.Items["RequestStopwatch"] = Stopwatch.StartNew();
    await next();
});

app.UseStaticFiles();

app.UseHtmx();

app.MapGet("/", (IViewRenderer view, HttpContext ctx) =>
    view.RenderAsync(ctx, "pages/home/index"));

app.MapGet("/about", (IViewRenderer view, HttpContext ctx) =>
    view.RenderAsync(ctx, "pages/about/index"));

app.MapGet("/components", (IViewRenderer view, HttpContext ctx) =>
{
    var model = new Dictionary<string, object>
    {
        ["breadcrumb_items"] = new[]
        {
            new Dictionary<string, object> { ["label"] = "Home", ["url"] = "/" },
            new Dictionary<string, object> { ["label"] = "Components", ["url"] = "/components" },
            new Dictionary<string, object> { ["label"] = "Showcase" }
        },
        ["dropdown_items"] = new[]
        {
            new Dictionary<string, object> { ["label"] = "Edit", ["url"] = "#" },
            new Dictionary<string, object> { ["label"] = "Duplicate", ["url"] = "#" },
            new Dictionary<string, object> { ["label"] = "Archive", ["url"] = "#" }
        },
        ["table_headers"] = new[] { "Name", "Email", "Role", "Status" },
        ["table_rows"] = new object[]
        {
            new[] { "Alice Johnson", "alice@example.com", "Admin", "Active" },
            new[] { "Bob Smith", "bob@example.com", "Editor", "Active" },
            new[] { "Carol White", "carol@example.com", "Viewer", "Inactive" }
        }
    };

    return view.RenderAsync(ctx, "pages/components/index", model);
});

app.MapGet("/partials/features", (IViewRenderer view) =>
    view.RenderPartialAsync("partials/feature-cards"));

app.MapGet("/partials/toast-demo", (IViewRenderer view) =>
    view.RenderPartialAsync("partials/toast-demo"));

app.MapGet("/partials/toast-demo-2", (IViewRenderer view) =>
    view.RenderPartialAsync("partials/toast-demo-2"));

app.MapGet("/partials/modal-demo", (IViewRenderer view) =>
    view.RenderPartialAsync("partials/modal-demo"));

app.Run();
