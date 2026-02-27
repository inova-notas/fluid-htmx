using FluentAssertions;
using InovaNotas.FluidHtmx.Configuration;
using InovaNotas.FluidHtmx.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace InovaNotas.FluidHtmx.Tests.Rendering;

public class ModelRegistrationTests
{
    private static FluidViewRenderer CreateRenderer()
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
        return new FluidViewRenderer(options, cache, locator, sp, logger);
    }

    private static async Task<string> RenderAndRead(IResult result)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();

        var ctx = new DefaultHttpContext
        {
            RequestServices = sp,
            Response = { Body = new MemoryStream() }
        };
        await result.ExecuteAsync(ctx);
        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        return await new StreamReader(ctx.Response.Body).ReadToEndAsync();
    }

    // --- Test model types ---

    public class AuthorModel
    {
        public string Name { get; set; } = "";
    }

    public class SessionModel
    {
        public string Title { get; set; } = "";
    }

    public class ConferenceModel
    {
        public string Title { get; set; } = "";
        public AuthorModel? Author { get; set; }
        public List<SessionModel> Sessions { get; set; } = [];
        public int? Rating { get; set; }
        public string? Description { get; set; }
    }

    // --- Tests ---

    [Fact]
    public async Task RenderPartialAsync_WithObjectModel_AccessesProperties()
    {
        var renderer = CreateRenderer();
        var model = new ConferenceModel { Title = "DotNetConf" };

        var result = await renderer.RenderPartialAsync("pages/model-test", model);
        var html = await RenderAndRead(result);

        html.Should().Contain("DotNetConf");
    }

    [Fact]
    public async Task RenderPartialAsync_WithNestedObject_AccessesNestedProperties()
    {
        var renderer = CreateRenderer();
        var model = new ConferenceModel
        {
            Title = "DotNetConf",
            Author = new AuthorModel { Name = "Scott" }
        };

        var result = await renderer.RenderPartialAsync("pages/model-test", model);
        var html = await RenderAndRead(result);

        html.Should().Contain("Scott");
    }

    [Fact]
    public async Task RenderPartialAsync_WithCollection_IteratesAndAccessesProperties()
    {
        var renderer = CreateRenderer();
        var model = new ConferenceModel
        {
            Title = "DotNetConf",
            Sessions =
            [
                new SessionModel { Title = "Blazor" },
                new SessionModel { Title = "MAUI" }
            ]
        };

        var result = await renderer.RenderPartialAsync("pages/model-test", model);
        var html = await RenderAndRead(result);

        html.Should().Contain("Blazor");
        html.Should().Contain("MAUI");
    }

    [Fact]
    public async Task RenderPartialAsync_WithDictionary_RegistersValueTypes()
    {
        var renderer = CreateRenderer();
        var model = new Dictionary<string, object>
        {
            ["Title"] = "FromDict",
            ["Author"] = new AuthorModel { Name = "Jane" }
        };

        var result = await renderer.RenderPartialAsync("pages/model-test", model);
        var html = await RenderAndRead(result);

        html.Should().Contain("FromDict");
        html.Should().Contain("Jane");
    }

    [Fact]
    public async Task RenderPartialAsync_WithNullableProperties_Works()
    {
        var renderer = CreateRenderer();
        var model = new ConferenceModel
        {
            Title = "NullableTest",
            Rating = 5
        };

        var result = await renderer.RenderPartialAsync("pages/model-test", model);
        var html = await RenderAndRead(result);

        html.Should().Contain("NullableTest");
        html.Should().Contain("5");
    }
}
