using FluentAssertions;
using InovaNotas.FluidHtmx.Extensions;
using InovaNotas.FluidHtmx.Htmx;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace InovaNotas.FluidHtmx.Tests.Htmx;

public class HtmxMiddlewareTests
{
    private static HtmxMiddleware CreateMiddleware(RequestDelegate? next = null)
        => new(next ?? (_ => Task.CompletedTask));

    [Fact]
    public async Task InvokeAsync_SetsHtmxRequestContext_WhenHtmxHeaderPresent()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Headers[HtmxHeaders.Request] = "true";

        await middleware.InvokeAsync(context);

        var htmx = context.GetHtmxContext();
        htmx.IsHtmx.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_SetsIsHtmxFalse_WhenNoHeader()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        var htmx = context.GetHtmxContext();
        htmx.IsHtmx.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_ParsesAllHeaders()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Headers[HtmxHeaders.Request] = "true";
        context.Request.Headers[HtmxHeaders.Boosted] = "true";
        context.Request.Headers[HtmxHeaders.CurrentUrl] = "https://example.com/page";
        context.Request.Headers[HtmxHeaders.HistoryRestoreRequest] = "true";
        context.Request.Headers[HtmxHeaders.Prompt] = "Are you sure?";
        context.Request.Headers[HtmxHeaders.Target] = "#content";
        context.Request.Headers[HtmxHeaders.TriggerName] = "btn-save";

        await middleware.InvokeAsync(context);

        var htmx = context.GetHtmxContext();
        htmx.IsHtmx.Should().BeTrue();
        htmx.IsBoosted.Should().BeTrue();
        htmx.CurrentUrl.Should().Be("https://example.com/page");
        htmx.IsHistoryRestoreRequest.Should().BeTrue();
        htmx.Prompt.Should().Be("Are you sure?");
        htmx.Target.Should().Be("#content");
        htmx.TriggerName.Should().Be("btn-save");
    }

    [Fact]
    public async Task InvokeAsync_CallsNext()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task GetHtmxContext_ReturnsContext_WhenMiddlewareRan()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Headers[HtmxHeaders.Request] = "true";
        context.Request.Headers[HtmxHeaders.Target] = "#main";

        await middleware.InvokeAsync(context);

        var htmx = context.GetHtmxContext();
        htmx.Should().NotBeNull();
        htmx.IsHtmx.Should().BeTrue();
        htmx.Target.Should().Be("#main");
    }

    [Fact]
    public async Task GetHtmxContext_ReturnsDefault_WhenMiddlewareNotRegistered()
    {
        var context = new DefaultHttpContext();

        var htmx = context.GetHtmxContext();

        htmx.Should().NotBeNull();
        htmx.IsHtmx.Should().BeFalse();
        htmx.Target.Should().BeNull();
    }
}
