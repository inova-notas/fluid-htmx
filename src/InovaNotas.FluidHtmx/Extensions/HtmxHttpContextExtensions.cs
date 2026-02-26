using InovaNotas.FluidHtmx.Htmx;
using Microsoft.AspNetCore.Http;

namespace InovaNotas.FluidHtmx.Extensions;

public static class HtmxHttpContextExtensions
{
    public static HtmxRequestContext GetHtmxContext(this HttpContext context)
        => context.Items[HtmxRequestContext.ItemsKey] as HtmxRequestContext
           ?? new HtmxRequestContext();
}
