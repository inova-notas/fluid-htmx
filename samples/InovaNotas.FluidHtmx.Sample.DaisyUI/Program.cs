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

app.MapGet("/partials/features", (IViewRenderer view) =>
    view.RenderPartialAsync("partials/feature-cards"));

app.Run();
