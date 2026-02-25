using InovaNotas.FluidHtmx.Layouts;
using Microsoft.AspNetCore.Http;

namespace InovaNotas.FluidHtmx.Rendering;

public interface IViewRenderer
{
    Task<IResult> RenderAsync(HttpContext httpContext, string templateName, object? model = null);

    Task<IResult> RenderAsync<TLayout>(HttpContext httpContext, string templateName, object? model = null)
        where TLayout : LayoutDefinition, new();

    Task<IResult> RenderPartialAsync(string templateName, object? model = null);
}
