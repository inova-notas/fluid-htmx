using Microsoft.AspNetCore.Http;

namespace InovaNotas.FluidHtmx.Htmx;

public class HtmxMiddleware
{
    private readonly RequestDelegate _next;

    public HtmxMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Request.Headers;

        context.Items[HtmxRequestContext.ItemsKey] = new HtmxRequestContext
        {
            IsHtmx = headers.ContainsKey(HtmxHeaders.Request),
            IsBoosted = headers.ContainsKey(HtmxHeaders.Boosted),
            CurrentUrl = headers[HtmxHeaders.CurrentUrl].FirstOrDefault(),
            IsHistoryRestoreRequest = headers.ContainsKey(HtmxHeaders.HistoryRestoreRequest),
            Prompt = headers[HtmxHeaders.Prompt].FirstOrDefault(),
            Target = headers[HtmxHeaders.Target].FirstOrDefault(),
            TriggerName = headers[HtmxHeaders.TriggerName].FirstOrDefault(),
        };

        return _next(context);
    }
}
