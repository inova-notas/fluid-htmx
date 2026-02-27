using FluentAssertions;
using InovaNotas.FluidHtmx.Configuration;
using InovaNotas.FluidHtmx.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace InovaNotas.FluidHtmx.Tests.Configuration;

public class EjectComponentTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _originalDir;

    public EjectComponentTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"fluid-eject-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _originalDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_tempDir);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_originalDir);
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static FluidHtmxBuilder CreateBuilder()
    {
        var services = new ServiceCollection();
        return new FluidHtmxBuilder(services);
    }

    [Fact]
    public void EjectComponent_CopiesFileToLocal()
    {
        var builder = CreateBuilder();

        builder.EjectComponent("toast");

        var destPath = Path.Combine(_tempDir, "Templates", "components", "toast.liquid");
        File.Exists(destPath).Should().BeTrue();
        var content = File.ReadAllText(destPath);
        content.Should().Contain("toast");
    }

    [Fact]
    public void EjectComponent_DoesNotOverwriteExisting()
    {
        var builder = CreateBuilder();
        var destDir = Path.Combine(_tempDir, "Templates", "components");
        Directory.CreateDirectory(destDir);
        var destPath = Path.Combine(destDir, "toast.liquid");
        File.WriteAllText(destPath, "custom content");

        builder.EjectComponent("toast");

        File.ReadAllText(destPath).Should().Be("custom content");
    }

    [Fact]
    public void EjectComponent_ThrowsForUnknownComponent()
    {
        var builder = CreateBuilder();

        var act = () => builder.EjectComponent("nonexistent");

        act.Should().Throw<FluidHtmxConfigException>()
            .WithMessage("*nonexistent*");
    }

    [Fact]
    public void EjectAllComponents_CopiesAllFiles()
    {
        var builder = CreateBuilder();

        builder.EjectAllComponents();

        var destDir = Path.Combine(_tempDir, "Templates", "components");
        Directory.Exists(destDir).Should().BeTrue();

        var files = Directory.GetFiles(destDir, "*.liquid")
            .Select(Path.GetFileNameWithoutExtension)
            .ToList();

        files.Should().Contain("toast");
        files.Should().Contain("confirm-dialog");
        files.Should().Contain("modal");
        files.Should().HaveCount(3);
    }

    [Fact]
    public void EjectComponent_WithCustomTemplatesPath()
    {
        var builder = CreateBuilder();
        builder.TemplatesPath("Views");

        builder.EjectComponent("modal");

        var destPath = Path.Combine(_tempDir, "Views", "components", "modal.liquid");
        File.Exists(destPath).Should().BeTrue();
    }

    [Fact]
    public void EjectComponent_ReturnsSelfForChaining()
    {
        var builder = CreateBuilder();

        var result = builder.EjectComponent("toast");

        result.Should().BeSameAs(builder);
    }
}
