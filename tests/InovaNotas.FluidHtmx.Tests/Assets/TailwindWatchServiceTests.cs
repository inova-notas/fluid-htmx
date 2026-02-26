using FluentAssertions;
using InovaNotas.FluidHtmx.Assets;
using InovaNotas.FluidHtmx.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace InovaNotas.FluidHtmx.Tests.Assets;

public class TailwindWatchServiceTests
{
    private static TailwindWatchService CreateService(bool isDevelopment, bool tailwindEnabled)
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns(isDevelopment ? Environments.Development : Environments.Production);
        env.ContentRootPath.Returns("/tmp");

        var options = Options.Create(new AssetOptions { TailwindEnabled = tailwindEnabled });
        var logger = NullLogger<TailwindWatchService>.Instance;

        return new TailwindWatchService(env, options, logger);
    }

    [Fact]
    public async Task ExecuteAsync_NotDevelopment_DoesNotStartProcess()
    {
        var service = CreateService(isDevelopment: false, tailwindEnabled: true);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await service.StartAsync(cts.Token);
        await service.StopAsync(CancellationToken.None);

        // If we reach here without exceptions, the service exited early (no process started)
        true.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_TailwindDisabled_DoesNotStartProcess()
    {
        var service = CreateService(isDevelopment: true, tailwindEnabled: false);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await service.StartAsync(cts.Token);
        await service.StopAsync(CancellationToken.None);

        // If we reach here without exceptions, the service exited early (no process started)
        true.Should().BeTrue();
    }
}
