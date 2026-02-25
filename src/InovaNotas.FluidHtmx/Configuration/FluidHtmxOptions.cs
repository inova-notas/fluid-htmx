using Fluid;
using InovaNotas.FluidHtmx.Htmx;
using InovaNotas.FluidHtmx.Rendering;
using Microsoft.AspNetCore.Http;

namespace InovaNotas.FluidHtmx.Configuration;

public class FluidHtmxOptions
{
    public string TemplatesPath { get; set; } = "Templates";

    public bool EnableHotReload { get; set; }

    public TemplateOptions TemplateOptions { get; set; } = new();

    public HtmxFluidParser Parser { get; set; } = new();

    public Type? DefaultLayoutType { get; set; }

    public AssetOptions Assets { get; set; } = new();

    public Func<IViewRenderer, HttpContext, string, object?, Task<IResult>>? InvokeDefaultLayout { get; set; }
}
