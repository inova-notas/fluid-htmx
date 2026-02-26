using FluentAssertions;
using InovaNotas.FluidHtmx.Assets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Xunit;

namespace InovaNotas.FluidHtmx.Tests.Assets;

public class AssetManifestTests
{
    private static AssetManifest CreateManifest(bool isDevelopment, IFileProvider? fileProvider = null)
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns(isDevelopment ? Environments.Development : Environments.Production);
        env.WebRootFileProvider.Returns(fileProvider ?? Substitute.For<IFileProvider>());
        return new AssetManifest(env);
    }

    [Fact]
    public void Resolve_InDevelopment_ReturnsOriginalPath()
    {
        var manifest = CreateManifest(isDevelopment: true);

        var result = manifest.Resolve("/css/app.css");

        result.Should().Be("/css/app.css");
    }

    [Fact]
    public void Resolve_InProduction_AppendsHash()
    {
        var content = "body { color: red; }"u8.ToArray();
        var fileInfo = Substitute.For<IFileInfo>();
        fileInfo.Exists.Returns(true);
        fileInfo.PhysicalPath.Returns("/wwwroot/css/app.css");
        fileInfo.CreateReadStream().Returns(_ => new MemoryStream(content));

        var fileProvider = Substitute.For<IFileProvider>();
        fileProvider.GetFileInfo("css/app.css").Returns(fileInfo);

        var manifest = CreateManifest(isDevelopment: false, fileProvider);

        var result = manifest.Resolve("/css/app.css");

        result.Should().StartWith("/css/app.css?v=");
        result.Should().MatchRegex(@"\?v=[0-9a-f]{8}$");
    }

    [Fact]
    public void Resolve_FileNotFound_ReturnsOriginalPath()
    {
        var fileInfo = Substitute.For<IFileInfo>();
        fileInfo.Exists.Returns(false);

        var fileProvider = Substitute.For<IFileProvider>();
        fileProvider.GetFileInfo("css/missing.css").Returns(fileInfo);

        var manifest = CreateManifest(isDevelopment: false, fileProvider);

        var result = manifest.Resolve("/css/missing.css");

        result.Should().Be("/css/missing.css");
    }

    [Fact]
    public void Resolve_CachesResult()
    {
        var content = "body { color: blue; }"u8.ToArray();
        var callCount = 0;

        var fileInfo = Substitute.For<IFileInfo>();
        fileInfo.Exists.Returns(true);
        fileInfo.PhysicalPath.Returns("/wwwroot/css/app.css");
        fileInfo.CreateReadStream().Returns(_ =>
        {
            callCount++;
            return new MemoryStream(content);
        });

        var fileProvider = Substitute.For<IFileProvider>();
        fileProvider.GetFileInfo("css/app.css").Returns(fileInfo);

        var manifest = CreateManifest(isDevelopment: false, fileProvider);

        var result1 = manifest.Resolve("/css/app.css");
        var result2 = manifest.Resolve("/css/app.css");

        result1.Should().Be(result2);
        callCount.Should().Be(1);
    }
}
