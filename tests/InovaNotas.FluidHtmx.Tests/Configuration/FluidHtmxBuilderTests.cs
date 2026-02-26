using FluentAssertions;
using InovaNotas.FluidHtmx.Configuration;
using InovaNotas.FluidHtmx.Exceptions;
using InovaNotas.FluidHtmx.Layouts;
using InovaNotas.FluidHtmx.Rendering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using NSubstitute;
using Xunit;

namespace InovaNotas.FluidHtmx.Tests.Configuration;

public class BuilderTestLayout : LayoutDefinition
{
    public override string TemplateName => "test";
}

public class BuilderTestDataProvider : ILayoutDataProvider<BuilderTestLayout>
{
    public Task<Dictionary<string, object>> GetDataAsync(HttpContext httpContext)
    {
        return Task.FromResult(new Dictionary<string, object> { ["title"] = "Test" });
    }
}

public class FluidHtmxBuilderTests
{
    [Fact]
    public void Validate_ThrowsWhenNoLayoutConfigured()
    {
        var services = new ServiceCollection();
        var builder = new FluidHtmxBuilder(services);

        var act = () => builder.Validate();

        act.Should().Throw<FluidHtmxConfigException>()
            .WithMessage("*layout*");
    }

    [Fact]
    public void Validate_PassesWithDefaultLayout()
    {
        var services = new ServiceCollection();
        var builder = new FluidHtmxBuilder(services);
        builder.DefaultLayout<BuilderTestLayout>();

        var act = () => builder.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_PassesWithAddLayout()
    {
        var services = new ServiceCollection();
        var builder = new FluidHtmxBuilder(services);
        builder.AddLayout<BuilderTestLayout>();

        var act = () => builder.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Build_RegistersServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var env = Substitute.For<IWebHostEnvironment>();
        env.WebRootFileProvider.Returns(Substitute.For<IFileProvider>());
        services.AddSingleton(env);
        var builder = new FluidHtmxBuilder(services);
        builder.DefaultLayout<BuilderTestLayout>();
        builder.Validate();
        builder.Build();

        var sp = services.BuildServiceProvider();

        sp.GetService<TemplateCache>().Should().NotBeNull();
        sp.GetService<TemplateLocator>().Should().NotBeNull();
        sp.GetService<IViewRenderer>().Should().NotBeNull();
    }

    [Fact]
    public void Build_RegistersLayoutDataProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = new FluidHtmxBuilder(services);
        builder.DefaultLayout<BuilderTestLayout>();
        builder.AddLayout<BuilderTestLayout, BuilderTestDataProvider>();
        builder.Validate();
        builder.Build();

        var sp = services.BuildServiceProvider();

        sp.GetService<ILayoutDataProvider<BuilderTestLayout>>().Should().NotBeNull();
        sp.GetService<ILayoutDataProvider<BuilderTestLayout>>().Should().BeOfType<BuilderTestDataProvider>();
    }

    [Fact]
    public void Build_CompilesDefaultLayoutDelegate()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = new FluidHtmxBuilder(services);
        builder.DefaultLayout<BuilderTestLayout>();
        builder.Validate();
        builder.Build();

        builder.Options.InvokeDefaultLayout.Should().NotBeNull();
    }
}
