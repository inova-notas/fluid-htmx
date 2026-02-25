using Microsoft.AspNetCore.Http;

namespace InovaNotas.FluidHtmx.Layouts;

public interface ILayoutDataProvider<TLayout> where TLayout : LayoutDefinition
{
    Task<Dictionary<string, object>> GetDataAsync(HttpContext httpContext);
}
