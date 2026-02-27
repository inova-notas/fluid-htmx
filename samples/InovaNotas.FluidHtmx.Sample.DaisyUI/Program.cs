using InovaNotas.FluidHtmx.Extensions;
using InovaNotas.FluidHtmx.Rendering;
using InovaNotas.FluidHtmx.Sample.DaisyUI.Layouts;
using InovaNotas.FluidHtmx.Sample.DaisyUI.Providers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFluidHtmx(fluid =>
{
    fluid.TemplatesPath("Templates");
    fluid.EnableHotReload(builder.Environment.IsDevelopment());
    fluid.Assets(assets => assets.EnableTailwind("v4.2.0")
        .EnableDaisyUI("5.5.19", themes: "dark"));
    fluid.DefaultLayout<LandingLayout>();
    fluid.AddLayout<LandingLayout, LandingLayoutDataProvider>();
    fluid.EjectAllComponents();
});

var app = builder.Build();

app.UseStaticFiles();
app.UseHtmx();

app.MapGet("/", (IViewRenderer view, HttpContext ctx) =>
    view.RenderAsync(ctx, "pages/landing/index"));

app.MapGet("/docs", (IViewRenderer view, HttpContext ctx) =>
    view.RenderAsync(ctx, "pages/docs/index"));

app.Run();
