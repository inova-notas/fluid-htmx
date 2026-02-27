using InovaNotas.FluidHtmx.Assets;
using InovaNotas.FluidHtmx.Exceptions;
using InovaNotas.FluidHtmx.Htmx.Filters;
using InovaNotas.FluidHtmx.Htmx.Tags;
using InovaNotas.FluidHtmx.Layouts;
using InovaNotas.FluidHtmx.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace InovaNotas.FluidHtmx.Configuration;

public class FluidHtmxBuilder
{
    private readonly IServiceCollection _services;
    private readonly FluidHtmxOptions _options = new();
    private Type? _defaultLayoutType;
    private readonly List<(Type LayoutType, Type? ProviderType)> _layouts = [];

    public FluidHtmxBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public FluidHtmxOptions Options => _options;

    public FluidHtmxBuilder TemplatesPath(string path)
    {
        _options.TemplatesPath = path;
        return this;
    }

    public FluidHtmxBuilder EnableHotReload(bool enable = true)
    {
        _options.EnableHotReload = enable;
        return this;
    }

    public FluidHtmxBuilder Assets(Action<AssetOptions> configure)
    {
        configure(_options.Assets);
        return this;
    }

    public FluidHtmxBuilder DefaultLayout<TLayout>() where TLayout : LayoutDefinition, new()
    {
        _defaultLayoutType = typeof(TLayout);
        _options.DefaultLayoutType = typeof(TLayout);
        return this;
    }

    public FluidHtmxBuilder AddLayout<TLayout, TProvider>()
        where TLayout : LayoutDefinition, new()
        where TProvider : class, ILayoutDataProvider<TLayout>
    {
        _layouts.Add((typeof(TLayout), typeof(TProvider)));
        return this;
    }

    public FluidHtmxBuilder AddLayout<TLayout>()
        where TLayout : LayoutDefinition, new()
    {
        _layouts.Add((typeof(TLayout), null));
        return this;
    }

    public void Validate()
    {
        if (_defaultLayoutType is null && _layouts.Count == 0)
            throw new FluidHtmxConfigException(
                "At least one layout must be configured. Use DefaultLayout<T>() or AddLayout<T>().");
    }

    public void Build()
    {
        if (_defaultLayoutType is not null)
        {
            CompileDefaultLayoutDelegate(_defaultLayoutType);
        }

        HtmxTagsRegistration.Register(_options.Parser);
        HtmxFiltersRegistration.Register(_options.TemplateOptions);

        foreach (var (layoutType, providerType) in _layouts)
        {
            if (providerType is not null)
            {
                var serviceType = typeof(ILayoutDataProvider<>).MakeGenericType(layoutType);
                _services.AddScoped(serviceType, providerType);
            }
        }

        _services.Configure<FluidHtmxOptions>(opts =>
        {
            opts.TemplatesPath = _options.TemplatesPath;
            opts.EnableHotReload = _options.EnableHotReload;
            opts.TemplateOptions = _options.TemplateOptions;
            opts.DefaultLayoutType = _options.DefaultLayoutType;
            opts.InvokeDefaultLayout = _options.InvokeDefaultLayout;
            opts.Parser = _options.Parser;
        });

        _services.Configure<AssetOptions>(opts =>
        {
            opts.TailwindEnabled = _options.Assets.TailwindEnabled;
            opts.TailwindVersion = _options.Assets.TailwindVersion;
            opts.InputCss = _options.Assets.InputCss;
            opts.OutputCss = _options.Assets.OutputCss;
            opts.DaisyUIEnabled = _options.Assets.DaisyUIEnabled;
            opts.DaisyUIVersion = _options.Assets.DaisyUIVersion;
            opts.DaisyUIThemes = _options.Assets.DaisyUIThemes;
        });

        _services.AddSingleton<AssetManifest>();

        if (_options.Assets.DaisyUIEnabled)
        {
            _services.AddHttpClient("DaisyUI");
            _services.AddHostedService<DaisyUISetupService>();
        }

        if (_options.Assets.TailwindEnabled)
            _services.AddHostedService<TailwindWatchService>();

        if (_options.EnableHotReload)
            _services.AddHostedService<TemplateFileWatcher>();

        _services.AddSingleton<TemplateCache>();
        _services.AddSingleton<TemplateLocator>();
        _services.AddScoped<IViewRenderer, FluidViewRenderer>();
    }

    public FluidHtmxBuilder EjectComponent(string name, ILogger? logger = null)
    {
        var embeddedProvider = CreateEmbeddedProvider();
        var relativePath = $"components/{name}.liquid";
        var fileInfo = embeddedProvider.GetFileInfo(relativePath);

        if (!fileInfo.Exists)
            throw new FluidHtmxConfigException($"Embedded component '{name}' not found.");

        var destDir = Path.Combine(Directory.GetCurrentDirectory(), _options.TemplatesPath, "components");
        var destPath = Path.Combine(destDir, $"{name}.liquid");

        if (File.Exists(destPath))
        {
            logger?.LogWarning("Component '{Name}' already exists at {Path}. Skipping.", name, destPath);
            return this;
        }

        Directory.CreateDirectory(destDir);

        using var stream = fileInfo.CreateReadStream();
        using var fileStream = File.Create(destPath);
        stream.CopyTo(fileStream);

        logger?.LogInformation("Ejected component '{Name}' to {Path}.", name, destPath);
        return this;
    }

    public FluidHtmxBuilder EjectAllComponents(ILogger? logger = null)
    {
        var embeddedProvider = CreateEmbeddedProvider();
        var contents = embeddedProvider.GetDirectoryContents("components");

        foreach (var file in contents)
        {
            if (file.IsDirectory || !file.Name.EndsWith(".liquid"))
                continue;

            var componentName = Path.GetFileNameWithoutExtension(file.Name);
            EjectComponent(componentName, logger);
        }

        return this;
    }

    private static ManifestEmbeddedFileProvider CreateEmbeddedProvider()
    {
        var assembly = typeof(FluidHtmxBuilder).Assembly;
        return new ManifestEmbeddedFileProvider(assembly, "Templates");
    }

    private void CompileDefaultLayoutDelegate(Type layoutType)
    {
        var method = typeof(FluidHtmxBuilder)
            .GetMethod(nameof(CreateTypedDelegate), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(layoutType);

        _options.InvokeDefaultLayout = (Func<IViewRenderer, HttpContext, string, object?, Task<IResult>>)method.Invoke(null, null)!;
    }

    private static Func<IViewRenderer, HttpContext, string, object?, Task<IResult>> CreateTypedDelegate<TLayout>()
        where TLayout : LayoutDefinition, new()
    {
        return (renderer, httpContext, templateName, model) =>
            renderer.RenderAsync<TLayout>(httpContext, templateName, model);
    }
}
