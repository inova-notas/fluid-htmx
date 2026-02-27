using FluentAssertions;
using InovaNotas.FluidHtmx.Assets;
using InovaNotas.FluidHtmx.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace InovaNotas.FluidHtmx.Tests.Assets;

public class DaisyUISetupServiceTests : IDisposable
{
    private readonly string _tempDir;

    public DaisyUISetupServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"daisyui-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private DaisyUISetupService CreateService(
        bool daisyUIEnabled = true,
        string? daisyUIVersion = null,
        List<string>? themes = null,
        string templatesPath = "Templates",
        string inputCss = "Styles/app.css",
        IHttpClientFactory? httpClientFactory = null)
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.ContentRootPath.Returns(_tempDir);

        var assetOptions = Options.Create(new AssetOptions
        {
            DaisyUIEnabled = daisyUIEnabled,
            DaisyUIVersion = daisyUIVersion ?? AssetOptions.DefaultDaisyUIVersion,
            DaisyUIThemes = themes ?? [],
            InputCss = inputCss,
            TailwindEnabled = true
        });

        var fluidOptions = Options.Create(new FluidHtmxOptions
        {
            TemplatesPath = templatesPath
        });

        httpClientFactory ??= Substitute.For<IHttpClientFactory>();
        var logger = NullLogger<DaisyUISetupService>.Instance;

        return new DaisyUISetupService(env, assetOptions, fluidOptions, httpClientFactory, logger);
    }

    [Fact]
    public async Task StartAsync_DaisyUIDisabled_DoesNothing()
    {
        var service = CreateService(daisyUIEnabled: false);

        await service.StartAsync(CancellationToken.None);

        var stylesDir = Path.Combine(_tempDir, "Styles");
        Directory.Exists(stylesDir).Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_FilesExist_SkipsDownload()
    {
        var stylesDir = Path.Combine(_tempDir, "Styles");
        Directory.CreateDirectory(stylesDir);
        File.WriteAllText(Path.Combine(stylesDir, "daisyui.mjs"), "existing");
        File.WriteAllText(Path.Combine(stylesDir, "daisyui-theme.mjs"), "existing");
        File.WriteAllText(Path.Combine(stylesDir, "app.css"), "existing");

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var service = CreateService(httpClientFactory: httpClientFactory);

        await service.StartAsync(CancellationToken.None);

        httpClientFactory.DidNotReceive().CreateClient(Arg.Any<string>());
        File.ReadAllText(Path.Combine(stylesDir, "app.css")).Should().Be("existing");
    }

    [Fact]
    public async Task StartAsync_CssExists_DoesNotOverwrite()
    {
        var stylesDir = Path.Combine(_tempDir, "Styles");
        Directory.CreateDirectory(stylesDir);
        File.WriteAllText(Path.Combine(stylesDir, "daisyui.mjs"), "existing");
        File.WriteAllText(Path.Combine(stylesDir, "daisyui-theme.mjs"), "existing");
        File.WriteAllText(Path.Combine(stylesDir, "app.css"), "my custom css");

        var service = CreateService();

        await service.StartAsync(CancellationToken.None);

        File.ReadAllText(Path.Combine(stylesDir, "app.css")).Should().Be("my custom css");
    }

    [Fact]
    public void GenerateCssContent_NoThemes_SimplePlugin()
    {
        var stylesDir = Path.Combine(_tempDir, "Styles");
        Directory.CreateDirectory(stylesDir);

        var service = CreateService(themes: []);
        var content = service.GenerateCssContent(stylesDir);

        content.Should().Contain("@import \"tailwindcss\";");
        content.Should().Contain("@plugin \"./daisyui.mjs\";");
        content.Should().Contain("@plugin \"./daisyui-theme.mjs\";");
        content.Should().NotContain("themes:");
    }

    [Fact]
    public void GenerateCssContent_SingleTheme_SetsDefault()
    {
        var stylesDir = Path.Combine(_tempDir, "Styles");
        Directory.CreateDirectory(stylesDir);

        var service = CreateService(themes: ["dark"]);
        var content = service.GenerateCssContent(stylesDir);

        content.Should().Contain("themes: dark --default;");
    }

    [Fact]
    public void GenerateCssContent_MultipleThemes_FirstIsDefault()
    {
        var stylesDir = Path.Combine(_tempDir, "Styles");
        Directory.CreateDirectory(stylesDir);

        var service = CreateService(themes: ["dark", "light"]);
        var content = service.GenerateCssContent(stylesDir);

        content.Should().Contain("themes: dark --default light;");
    }

    [Fact]
    public void GenerateCssContent_UsesTemplatesPath()
    {
        var stylesDir = Path.Combine(_tempDir, "Styles");
        Directory.CreateDirectory(stylesDir);

        var service = CreateService(templatesPath: "Views");
        var content = service.GenerateCssContent(stylesDir);

        content.Should().Contain("../Views/**/*.liquid");
    }

    [Fact]
    public void GenerateCssContent_IncludesSourceExclusion()
    {
        var stylesDir = Path.Combine(_tempDir, "Styles");
        Directory.CreateDirectory(stylesDir);

        var service = CreateService();
        var content = service.GenerateCssContent(stylesDir);

        content.Should().Contain("@source not \"./daisyui{,*}.mjs\";");
    }

    [Theory]
    [InlineData("v5.5.19", "v5.5.19")]
    [InlineData("5.5.19", "v5.5.19")]
    [InlineData("v6.0.0", "v6.0.0")]
    [InlineData("6.0.0", "v6.0.0")]
    public void NormalizeVersion_EnsuresVPrefix(string input, string expected)
    {
        DaisyUISetupService.NormalizeVersion(input).Should().Be(expected);
    }
}
