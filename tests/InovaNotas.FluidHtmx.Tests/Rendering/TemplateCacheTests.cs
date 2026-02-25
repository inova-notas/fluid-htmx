using Fluid;
using FluentAssertions;
using InovaNotas.FluidHtmx.Rendering;
using Xunit;

namespace InovaNotas.FluidHtmx.Tests.Rendering;

public class TemplateCacheTests
{
    private readonly TemplateCache _cache = new();

    [Fact]
    public async Task GetOrAddAsync_CachesTemplate()
    {
        var callCount = 0;
        var parser = new FluidParser();
        parser.TryParse("Hello", out var template, out _);

        var result1 = await _cache.GetOrAddAsync("test", () =>
        {
            callCount++;
            return Task.FromResult<IFluidTemplate>(template!);
        });

        var result2 = await _cache.GetOrAddAsync("test", () =>
        {
            callCount++;
            return Task.FromResult<IFluidTemplate>(template!);
        });

        callCount.Should().Be(1);
        result1.Should().BeSameAs(result2);
    }

    [Fact]
    public async Task Invalidate_RemovesCachedEntry()
    {
        var parser = new FluidParser();
        parser.TryParse("Hello", out var template, out _);

        await _cache.GetOrAddAsync("test", () => Task.FromResult<IFluidTemplate>(template!));

        _cache.Invalidate("test");

        var callCount = 0;
        await _cache.GetOrAddAsync("test", () =>
        {
            callCount++;
            return Task.FromResult<IFluidTemplate>(template!);
        });

        callCount.Should().Be(1);
    }

    [Fact]
    public async Task Clear_RemovesAllEntries()
    {
        var parser = new FluidParser();
        parser.TryParse("A", out var templateA, out _);
        parser.TryParse("B", out var templateB, out _);

        await _cache.GetOrAddAsync("a", () => Task.FromResult<IFluidTemplate>(templateA!));
        await _cache.GetOrAddAsync("b", () => Task.FromResult<IFluidTemplate>(templateB!));

        _cache.Clear();

        var callCount = 0;
        await _cache.GetOrAddAsync("a", () =>
        {
            callCount++;
            return Task.FromResult<IFluidTemplate>(templateA!);
        });
        await _cache.GetOrAddAsync("b", () =>
        {
            callCount++;
            return Task.FromResult<IFluidTemplate>(templateB!);
        });

        callCount.Should().Be(2);
    }

    [Fact]
    public async Task GetOrAddAsync_ConcurrentAccess_OnlyCallsFactoryOnce()
    {
        var callCount = 0;
        var parser = new FluidParser();
        parser.TryParse("Concurrent", out var template, out _);

        var tasks = Enumerable.Range(0, 10).Select(_ =>
            _cache.GetOrAddAsync("concurrent", async () =>
            {
                Interlocked.Increment(ref callCount);
                await Task.Delay(10);
                return (IFluidTemplate)template!;
            }));

        await Task.WhenAll(tasks);

        callCount.Should().Be(1);
    }
}
