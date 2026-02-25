using System.Collections.Concurrent;
using Fluid;

namespace InovaNotas.FluidHtmx.Rendering;

public class TemplateCache
{
    private readonly ConcurrentDictionary<string, IFluidTemplate> _cache = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<IFluidTemplate> GetOrAddAsync(string key, Func<Task<IFluidTemplate>> factory)
    {
        if (_cache.TryGetValue(key, out var cached))
            return cached;

        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            if (_cache.TryGetValue(key, out cached))
                return cached;

            var template = await factory();
            _cache[key] = template;
            return template;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public void Invalidate(string key)
    {
        _cache.TryRemove(key, out _);
    }

    public void Clear()
    {
        _cache.Clear();
    }
}
