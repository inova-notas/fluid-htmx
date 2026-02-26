using System.Text.Json;
using FluentAssertions;
using InovaNotas.FluidHtmx.Extensions;
using InovaNotas.FluidHtmx.Htmx;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace InovaNotas.FluidHtmx.Tests.Htmx;

public class HtmxResponseExtensionsTests
{
    private static HttpResponse CreateResponse()
        => new DefaultHttpContext().Response;

    [Fact]
    public void HxRedirect_SetsHeader()
    {
        var response = CreateResponse();

        response.HxRedirect("/login");

        response.Headers[HtmxHeaders.Redirect].ToString().Should().Be("/login");
    }

    [Fact]
    public void HxLocation_SetsHeader()
    {
        var response = CreateResponse();

        response.HxLocation("/dashboard");

        response.Headers[HtmxHeaders.Location].ToString().Should().Be("/dashboard");
    }

    [Fact]
    public void HxRefresh_SetsTrue()
    {
        var response = CreateResponse();

        response.HxRefresh();

        response.Headers[HtmxHeaders.Refresh].ToString().Should().Be("true");
    }

    [Fact]
    public void HxPushUrl_SetsHeader()
    {
        var response = CreateResponse();

        response.HxPushUrl("/new-url");

        response.Headers[HtmxHeaders.PushUrl].ToString().Should().Be("/new-url");
    }

    [Fact]
    public void HxReplaceUrl_SetsHeader()
    {
        var response = CreateResponse();

        response.HxReplaceUrl("/replaced");

        response.Headers[HtmxHeaders.ReplaceUrl].ToString().Should().Be("/replaced");
    }

    [Fact]
    public void HxReswap_SetsHeader()
    {
        var response = CreateResponse();

        response.HxReswap("outerHTML");

        response.Headers[HtmxHeaders.Reswap].ToString().Should().Be("outerHTML");
    }

    [Fact]
    public void HxRetarget_SetsHeader()
    {
        var response = CreateResponse();

        response.HxRetarget("#content");

        response.Headers[HtmxHeaders.Retarget].ToString().Should().Be("#content");
    }

    [Fact]
    public void HxReselect_SetsHeader()
    {
        var response = CreateResponse();

        response.HxReselect(".main-content");

        response.Headers[HtmxHeaders.Reselect].ToString().Should().Be(".main-content");
    }

    [Fact]
    public void HxTrigger_SetsSimpleEvent()
    {
        var response = CreateResponse();

        response.HxTrigger("myEvent");

        response.Headers[HtmxHeaders.Trigger].ToString().Should().Be("myEvent");
    }

    [Fact]
    public void HxTrigger_SetsJsonWithData()
    {
        var response = CreateResponse();

        response.HxTrigger("showMessage", new { level = "info", message = "Saved!" });

        var header = response.Headers[HtmxHeaders.Trigger].ToString();
        var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(header);
        json.Should().ContainKey("showMessage");
        var data = json!["showMessage"];
        data.GetProperty("level").GetString().Should().Be("info");
        data.GetProperty("message").GetString().Should().Be("Saved!");
    }

    [Fact]
    public void HxTriggerAfterSettle_SetsHeader()
    {
        var response = CreateResponse();

        response.HxTriggerAfterSettle("settleEvent");

        response.Headers[HtmxHeaders.TriggerAfterSettle].ToString().Should().Be("settleEvent");
    }

    [Fact]
    public void HxTriggerAfterSwap_SetsHeader()
    {
        var response = CreateResponse();

        response.HxTriggerAfterSwap("swapEvent");

        response.Headers[HtmxHeaders.TriggerAfterSwap].ToString().Should().Be("swapEvent");
    }
}
