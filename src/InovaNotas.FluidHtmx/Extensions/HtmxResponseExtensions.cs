using System.Text.Json;
using InovaNotas.FluidHtmx.Htmx;
using Microsoft.AspNetCore.Http;

namespace InovaNotas.FluidHtmx.Extensions;

public static class HtmxResponseExtensions
{
    public static void HxLocation(this HttpResponse response, string url)
        => response.Headers.Append(HtmxHeaders.Location, url);

    public static void HxPushUrl(this HttpResponse response, string url)
        => response.Headers.Append(HtmxHeaders.PushUrl, url);

    public static void HxRedirect(this HttpResponse response, string url)
        => response.Headers.Append(HtmxHeaders.Redirect, url);

    public static void HxRefresh(this HttpResponse response)
        => response.Headers.Append(HtmxHeaders.Refresh, "true");

    public static void HxReplaceUrl(this HttpResponse response, string url)
        => response.Headers.Append(HtmxHeaders.ReplaceUrl, url);

    public static void HxReswap(this HttpResponse response, string strategy)
        => response.Headers.Append(HtmxHeaders.Reswap, strategy);

    public static void HxRetarget(this HttpResponse response, string selector)
        => response.Headers.Append(HtmxHeaders.Retarget, selector);

    public static void HxReselect(this HttpResponse response, string selector)
        => response.Headers.Append(HtmxHeaders.Reselect, selector);

    public static void HxTrigger(this HttpResponse response, string eventName)
        => response.Headers.Append(HtmxHeaders.Trigger, eventName);

    public static void HxTrigger(this HttpResponse response, string eventName, object data)
    {
        var json = JsonSerializer.Serialize(
            new Dictionary<string, object> { [eventName] = data });
        response.Headers.Append(HtmxHeaders.Trigger, json);
    }

    public static void HxTriggerAfterSettle(this HttpResponse response, string eventName)
        => response.Headers.Append(HtmxHeaders.TriggerAfterSettle, eventName);

    public static void HxTriggerAfterSwap(this HttpResponse response, string eventName)
        => response.Headers.Append(HtmxHeaders.TriggerAfterSwap, eventName);
}
