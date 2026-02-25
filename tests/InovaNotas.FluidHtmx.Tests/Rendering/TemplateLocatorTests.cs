using FluentAssertions;
using InovaNotas.FluidHtmx.Configuration;
using InovaNotas.FluidHtmx.Exceptions;
using Xunit;
using InovaNotas.FluidHtmx.Rendering;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace InovaNotas.FluidHtmx.Tests.Rendering;

public class TemplateLocatorTests
{
    private static TemplateLocator CreateLocator(string templatesPath)
    {
        var options = Options.Create(new FluidHtmxOptions { TemplatesPath = templatesPath });
        var testTemplatesDir = Path.Combine(AppContext.BaseDirectory, "TestTemplates");
        var physicalProvider = new PhysicalFileProvider(testTemplatesDir);
        return new TemplateLocator(options, physicalProvider);
    }

    [Fact]
    public async Task ReadTemplateAsync_FindsLocalTemplate()
    {
        var locator = CreateLocator("TestTemplates");

        var content = await locator.ReadTemplateAsync("pages/hello");

        content.Should().Contain("Hello");
    }

    [Fact]
    public async Task ReadTemplateAsync_FindsTemplateWithLiquidExtension()
    {
        var locator = CreateLocator("TestTemplates");

        var content = await locator.ReadTemplateAsync("pages/hello.liquid");

        content.Should().Contain("Hello");
    }

    [Fact]
    public async Task ReadTemplateAsync_ThrowsForMissingTemplate()
    {
        var locator = CreateLocator("TestTemplates");

        var act = () => locator.ReadTemplateAsync("pages/nonexistent");

        await act.Should().ThrowAsync<TemplateNotFoundException>()
            .WithMessage("*nonexistent*");
    }

    [Fact]
    public void FileProvider_IsNotNull()
    {
        var locator = CreateLocator("TestTemplates");

        locator.FileProvider.Should().NotBeNull();
    }
}
