using InovaNotas.FluidHtmx.Extensions;
using InovaNotas.FluidHtmx.Rendering;
using InovaNotas.FluidHtmx.Sample.Layouts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFluidHtmx(fluid =>
{
    fluid.TemplatesPath("Templates");
    fluid.DefaultLayout<MainLayout>();
});

var app = builder.Build();

app.MapGet("/", (IViewRenderer view, HttpContext ctx) =>
    view.RenderAsync(ctx, "pages/home/index"));

app.MapGet("/about", (IViewRenderer view, HttpContext ctx) =>
    view.RenderAsync(ctx, "pages/home/index", new Dictionary<string, object>
    {
        ["title"] = "About"
    }));

app.Run();
