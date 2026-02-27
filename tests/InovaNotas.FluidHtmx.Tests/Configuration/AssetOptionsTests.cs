using FluentAssertions;
using InovaNotas.FluidHtmx.Configuration;
using Xunit;

namespace InovaNotas.FluidHtmx.Tests.Configuration;

public class AssetOptionsTests
{
    [Fact]
    public void EnableDaisyUI_SetsDaisyUIEnabled()
    {
        var options = new AssetOptions();

        options.EnableDaisyUI();

        options.DaisyUIEnabled.Should().BeTrue();
    }

    [Fact]
    public void EnableDaisyUI_ImplicitlyEnablesTailwind()
    {
        var options = new AssetOptions();

        options.EnableDaisyUI();

        options.TailwindEnabled.Should().BeTrue();
    }

    [Fact]
    public void EnableDaisyUI_WithVersion_SetsDaisyUIVersion()
    {
        var options = new AssetOptions();

        options.EnableDaisyUI("v5.5.19");

        options.DaisyUIVersion.Should().Be("v5.5.19");
    }

    [Fact]
    public void EnableDaisyUI_WithoutVersion_UsesDefault()
    {
        var options = new AssetOptions();

        options.EnableDaisyUI();

        options.DaisyUIVersion.Should().Be(AssetOptions.DefaultDaisyUIVersion);
    }

    [Fact]
    public void EnableDaisyUI_WithThemes_SetsThemes()
    {
        var options = new AssetOptions();

        options.EnableDaisyUI(themes: ["dark", "light"]);

        options.DaisyUIThemes.Should().BeEquivalentTo(["dark", "light"]);
    }

    [Fact]
    public void EnableDaisyUI_WithoutThemes_LeavesThemesEmpty()
    {
        var options = new AssetOptions();

        options.EnableDaisyUI();

        options.DaisyUIThemes.Should().BeEmpty();
    }

    [Fact]
    public void EnableDaisyUI_Chainable()
    {
        var options = new AssetOptions();

        var result = options.EnableTailwind("v4.2.0").EnableDaisyUI("v5.5.19", "dark");

        result.Should().BeSameAs(options);
        options.TailwindEnabled.Should().BeTrue();
        options.TailwindVersion.Should().Be("v4.2.0");
        options.DaisyUIEnabled.Should().BeTrue();
        options.DaisyUIVersion.Should().Be("v5.5.19");
        options.DaisyUIThemes.Should().BeEquivalentTo(["dark"]);
    }
}
