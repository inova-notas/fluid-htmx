using FluentAssertions;
using InovaNotas.FluidHtmx.Configuration;
using InovaNotas.FluidHtmx.Htmx;
using Xunit;
using InovaNotas.FluidHtmx.Layouts;
using InovaNotas.FluidHtmx.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InovaNotas.FluidHtmx.Tests.Rendering;

public class TestLayout : LayoutDefinition
{
    public override string TemplateName => "test";
}

public class FluidViewRendererTests
{
    private static (FluidViewRenderer renderer, IServiceProvider sp) CreateRenderer()
    {
        var testTemplatesDir = Path.Combine(AppContext.BaseDirectory, "TestTemplates");
        var physicalProvider = new PhysicalFileProvider(testTemplatesDir);

        var options = Options.Create(new FluidHtmxOptions());
        var cache = new TemplateCache();
        var locator = new TemplateLocator(options, physicalProvider);

        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();

        var logger = new NullLogger<FluidViewRenderer>();
        var renderer = new FluidViewRenderer(options, cache, locator, sp, logger);

        return (renderer, sp);
    }

    private static DefaultHttpContext CreateHttpContext(IServiceProvider sp)
    {
        var ctx = new DefaultHttpContext
        {
            RequestServices = sp,
            Response = { Body = new MemoryStream() }
        };
        return ctx;
    }

    private static async Task<string> ReadResponseBody(HttpContext ctx)
    {
        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        return await new StreamReader(ctx.Response.Body).ReadToEndAsync();
    }

    [Fact]
    public async Task RenderPartialAsync_RendersTemplate()
    {
        var (renderer, sp) = CreateRenderer();

        var result = await renderer.RenderPartialAsync("pages/hello");

        var httpContext = CreateHttpContext(sp);
        await result.ExecuteAsync(httpContext);
        var html = await ReadResponseBody(httpContext);

        html.Should().Contain("Hello");
        html.Should().Contain("World");
    }

    [Fact]
    public async Task RenderPartialAsync_RendersWithModel()
    {
        var (renderer, sp) = CreateRenderer();
        var model = new Dictionary<string, object> { ["name"] = "FluidHtmx" };

        var result = await renderer.RenderPartialAsync("pages/hello", model);

        var httpContext = CreateHttpContext(sp);
        await result.ExecuteAsync(httpContext);
        var html = await ReadResponseBody(httpContext);

        html.Should().Contain("FluidHtmx");
    }

    [Fact]
    public async Task RenderAsync_WithLayout_WrapsContent()
    {
        var (renderer, sp) = CreateRenderer();

        var httpContext = CreateHttpContext(sp);
        httpContext.Request.Path = "/test";

        var result = await renderer.RenderAsync<TestLayout>(httpContext, "pages/hello");

        await result.ExecuteAsync(httpContext);
        var html = await ReadResponseBody(httpContext);

        html.Should().Contain("<!DOCTYPE html>");
        html.Should().Contain("Test Layout");
        html.Should().Contain("Hello");
    }

    [Fact]
    public async Task RenderAsync_WithHtmxHeader_ReturnsPartial()
    {
        var (renderer, sp) = CreateRenderer();

        var httpContext = CreateHttpContext(sp);
        httpContext.Request.Headers[HtmxHeaders.Request] = "true";
        httpContext.Request.Path = "/test";

        var result = await renderer.RenderAsync<TestLayout>(httpContext, "pages/hello");

        await result.ExecuteAsync(httpContext);
        var html = await ReadResponseBody(httpContext);

        html.Should().Contain("Hello");
        html.Should().NotContain("<!DOCTYPE html>");
    }
}
