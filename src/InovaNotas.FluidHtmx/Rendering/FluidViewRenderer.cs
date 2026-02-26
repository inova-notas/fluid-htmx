using Fluid;
using InovaNotas.FluidHtmx.Assets;
using InovaNotas.FluidHtmx.Configuration;
using InovaNotas.FluidHtmx.Exceptions;
using InovaNotas.FluidHtmx.Htmx;
using InovaNotas.FluidHtmx.Layouts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InovaNotas.FluidHtmx.Rendering;

public class FluidViewRenderer : IViewRenderer
{
    private readonly FluidHtmxOptions _options;
    private readonly TemplateCache _cache;
    private readonly TemplateLocator _locator;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FluidViewRenderer> _logger;
    private readonly AssetManifest? _assetManifest;

    public FluidViewRenderer(
        IOptions<FluidHtmxOptions> options,
        TemplateCache cache,
        TemplateLocator locator,
        IServiceProvider serviceProvider,
        ILogger<FluidViewRenderer> logger,
        AssetManifest? assetManifest = null)
    {
        _options = options.Value;
        _cache = cache;
        _locator = locator;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _assetManifest = assetManifest;

        _options.TemplateOptions.FileProvider = _locator.FileProvider;
    }

    public Task<IResult> RenderAsync(HttpContext httpContext, string templateName, object? model = null)
    {
        if (_options.InvokeDefaultLayout is null)
            throw new FluidHtmxConfigException(
                "No default layout configured. Use DefaultLayout<T>() in AddFluidHtmx or use RenderAsync<TLayout>().");

        return _options.InvokeDefaultLayout(this, httpContext, templateName, model);
    }

    public async Task<IResult> RenderAsync<TLayout>(HttpContext httpContext, string templateName, object? model = null)
        where TLayout : LayoutDefinition, new()
    {
        var isHtmxRequest = httpContext.Request.Headers.ContainsKey(HtmxHeaders.Request);

        if (isHtmxRequest)
        {
            _logger.LogDebug("HTMX request detected, rendering partial for {Template}", templateName);
            return await RenderPartialAsync(templateName, model);
        }

        var contentHtml = await RenderTemplateAsync(templateName, model);

        var layout = new TLayout();
        var layoutData = new Dictionary<string, object>
        {
            ["content"] = contentHtml,
            ["_request_path"] = httpContext.Request.Path.Value ?? "/"
        };

        if (model is not null)
            layoutData["model"] = model;

        var provider = _serviceProvider.GetService<ILayoutDataProvider<TLayout>>();
        if (provider is not null)
        {
            var extraData = await provider.GetDataAsync(httpContext);
            foreach (var kvp in extraData)
            {
                layoutData[kvp.Key] = kvp.Value;
            }
        }

        var layoutHtml = await RenderTemplateAsync($"layouts/{layout.TemplateName}", layoutData);

        return Results.Content(layoutHtml, "text/html; charset=utf-8");
    }

    public async Task<IResult> RenderPartialAsync(string templateName, object? model = null)
    {
        var html = await RenderTemplateAsync(templateName, model);
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private async Task<string> RenderTemplateAsync(string templateName, object? model)
    {
        var template = await _cache.GetOrAddAsync(templateName, () => ParseTemplateAsync(templateName));

        var context = new TemplateContext(model ?? new { }, _options.TemplateOptions);

        if (_assetManifest is not null)
            context.SetValue("_asset_manifest", _assetManifest);

        if (model is Dictionary<string, object> dict)
        {
            foreach (var kvp in dict)
            {
                context.SetValue(kvp.Key, kvp.Value);
            }
        }

        return await template.RenderAsync(context);
    }

    private async Task<IFluidTemplate> ParseTemplateAsync(string templateName)
    {
        var source = await _locator.ReadTemplateAsync(templateName);

        if (!_options.Parser.TryParse(source, out var parsed, out var error))
            throw new TemplateParseException(templateName, error);

        return parsed;
    }
}
