using Fluid;
using FluentAssertions;
using InovaNotas.FluidHtmx.Configuration;
using InovaNotas.FluidHtmx.Rendering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace InovaNotas.FluidHtmx.Tests.Rendering;

public class TemplateFileWatcherTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _templatesDir;

    public TemplateFileWatcherTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"fluidhtmx_test_{Guid.NewGuid():N}");
        _templatesDir = Path.Combine(_tempDir, "Templates");
        Directory.CreateDirectory(_templatesDir);
        Directory.CreateDirectory(Path.Combine(_templatesDir, "pages", "home"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); }
        catch { /* cleanup best-effort */ }
    }

    private TemplateFileWatcher CreateWatcher(bool enableHotReload = true)
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.ContentRootPath.Returns(_tempDir);

        var options = Options.Create(new FluidHtmxOptions
        {
            EnableHotReload = enableHotReload,
            TemplatesPath = "Templates"
        });

        var cache = new TemplateCache();

        return new TemplateFileWatcher(env, options, cache, NullLogger<TemplateFileWatcher>.Instance);
    }

    private (TemplateFileWatcher Watcher, TemplateCache Cache) CreateWatcherWithCache(bool enableHotReload = true)
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.ContentRootPath.Returns(_tempDir);

        var options = Options.Create(new FluidHtmxOptions
        {
            EnableHotReload = enableHotReload,
            TemplatesPath = "Templates"
        });

        var cache = new TemplateCache();

        var watcher = new TemplateFileWatcher(env, options, cache, NullLogger<TemplateFileWatcher>.Instance);
        return (watcher, cache);
    }

    [Fact]
    public async Task DoesNotStart_WhenHotReloadDisabled()
    {
        var watcher = CreateWatcher(enableHotReload: false);
        using var cts = new CancellationTokenSource();

        // Should complete immediately without starting the FileSystemWatcher
        await watcher.StartAsync(cts.Token);

        // Give it a moment, then stop — no errors expected
        await watcher.StopAsync(CancellationToken.None);
        watcher.Dispose();
    }

    [Fact]
    public async Task InvalidatesCache_WhenFileChanged()
    {
        var (watcher, cache) = CreateWatcherWithCache();

        // Pre-create the file so that later writes trigger Changed events
        var filePath = Path.Combine(_templatesDir, "pages", "home", "index.liquid");
        await File.WriteAllTextAsync(filePath, "Original content");

        // Populate cache
        var parser = new FluidParser();
        parser.TryParse("Hello", out var template, out _);
        await cache.GetOrAddAsync("pages/home/index", () => Task.FromResult<IFluidTemplate>(template!));

        using var cts = new CancellationTokenSource();
        await watcher.StartAsync(cts.Token);

        // Allow watcher to initialize
        await Task.Delay(100);

        // Write a file change
        await File.WriteAllTextAsync(filePath, "Updated content");

        // Wait for debounce + processing (longer delay for CI/Linux inotify)
        await Task.Delay(500);

        // Cache should have been invalidated — a new factory call should execute
        var factoryCalled = false;
        await cache.GetOrAddAsync("pages/home/index", () =>
        {
            factoryCalled = true;
            return Task.FromResult<IFluidTemplate>(template!);
        });

        factoryCalled.Should().BeTrue("file watcher should have invalidated the cache entry");

        await watcher.StopAsync(CancellationToken.None);
        watcher.Dispose();
    }

    [Fact]
    public void ConvertsFilePath_ToTemplateName()
    {
        var watcher = CreateWatcher();

        var filePath = Path.Combine(_tempDir, "Templates", "pages", "home", "index.liquid");
        var result = watcher.ConvertToTemplateName(filePath);

        result.Should().Be("pages/home/index");

        watcher.Dispose();
    }

    [Fact]
    public async Task Debounces_RapidChanges()
    {
        var (watcher, cache) = CreateWatcherWithCache();

        // Pre-create the file
        var filePath = Path.Combine(_templatesDir, "pages", "home", "index.liquid");
        await File.WriteAllTextAsync(filePath, "Original");

        // Populate cache
        var parser = new FluidParser();
        parser.TryParse("Hello", out var template, out _);
        await cache.GetOrAddAsync("pages/home/index", () => Task.FromResult<IFluidTemplate>(template!));

        using var cts = new CancellationTokenSource();
        await watcher.StartAsync(cts.Token);

        // Allow watcher to initialize
        await Task.Delay(100);

        // Rapid writes to the same file
        for (var i = 0; i < 5; i++)
        {
            await File.WriteAllTextAsync(filePath, $"Content {i}");
            await Task.Delay(10);
        }

        // Wait for debounce + processing
        await Task.Delay(500);

        // Cache should be invalidated (but only processed once due to debounce)
        var factoryCalled = false;
        await cache.GetOrAddAsync("pages/home/index", () =>
        {
            factoryCalled = true;
            return Task.FromResult<IFluidTemplate>(template!);
        });

        factoryCalled.Should().BeTrue("cache should have been invalidated after debounced writes");

        await watcher.StopAsync(CancellationToken.None);
        watcher.Dispose();
    }
}
