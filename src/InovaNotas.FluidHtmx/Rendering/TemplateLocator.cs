using InovaNotas.FluidHtmx.Configuration;
using InovaNotas.FluidHtmx.Exceptions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace InovaNotas.FluidHtmx.Rendering;

public class TemplateLocator
{
    private readonly IFileProvider _fileProvider;

    public TemplateLocator(IOptions<FluidHtmxOptions> options, IFileProvider? physicalProvider = null)
    {
        var opts = options.Value;

        var providers = new List<IFileProvider>();

        if (physicalProvider is not null)
        {
            providers.Add(physicalProvider);
        }
        else
        {
            var templatesPath = Path.Combine(Directory.GetCurrentDirectory(), opts.TemplatesPath);
            if (Directory.Exists(templatesPath))
            {
                providers.Add(new PhysicalFileProvider(templatesPath));
            }
        }

        var assembly = typeof(TemplateLocator).Assembly;
        try
        {
            providers.Add(new ManifestEmbeddedFileProvider(assembly, "Templates"));
        }
        catch (InvalidOperationException)
        {
            // No embedded manifest — skip
        }

        _fileProvider = providers.Count switch
        {
            0 => new NullFileProvider(),
            1 => providers[0],
            _ => new CompositeFileProvider(providers)
        };
    }

    public IFileProvider FileProvider => _fileProvider;

    public async Task<string> ReadTemplateAsync(string name)
    {
        var path = name.EndsWith(".liquid") ? name : $"{name}.liquid";
        var fileInfo = _fileProvider.GetFileInfo(path);

        if (!fileInfo.Exists)
            throw new TemplateNotFoundException(name);

        await using var stream = fileInfo.CreateReadStream();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
