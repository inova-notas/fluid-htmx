using InovaNotas.FluidHtmx.Assets;
using InovaNotas.FluidHtmx.Exceptions;
using InovaNotas.FluidHtmx.Htmx.Filters;
using InovaNotas.FluidHtmx.Htmx.Tags;
using InovaNotas.FluidHtmx.Layouts;
using InovaNotas.FluidHtmx.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

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
            opts.DaisyUIThemes = _options.Assets.DaisyUIThemes;
        });

        _services.AddSingleton<AssetManifest>();

        if (_options.Assets.TailwindEnabled)
            _services.AddHostedService<TailwindWatchService>();

        if (_options.EnableHotReload)
            _services.AddHostedService<TemplateFileWatcher>();

        _services.AddSingleton<TemplateCache>();
        _services.AddSingleton<TemplateLocator>();
        _services.AddScoped<IViewRenderer, FluidViewRenderer>();
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
