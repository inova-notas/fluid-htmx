using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace InovaNotas.FluidHtmx.Assets;

public class AssetManifest
{
    private readonly IWebHostEnvironment _env;
    private readonly ConcurrentDictionary<string, string> _cache = new();

    public AssetManifest(IWebHostEnvironment env)
    {
        _env = env;
    }

    public string Resolve(string path)
    {
        if (_env.IsDevelopment())
            return path;

        return _cache.GetOrAdd(path, ComputeVersionedPath);
    }

    private string ComputeVersionedPath(string path)
    {
        var fileInfo = _env.WebRootFileProvider.GetFileInfo(path.TrimStart('/'));

        if (!fileInfo.Exists || fileInfo.PhysicalPath is null)
            return path;

        using var stream = fileInfo.CreateReadStream();
        var hash = SHA256.HashData(stream);
        var shortHash = Convert.ToHexString(hash)[..8].ToLowerInvariant();

        var separator = path.Contains('?') ? '&' : '?';
        return $"{path}{separator}v={shortHash}";
    }
}
