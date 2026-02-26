using Fluid;
using FluentAssertions;
using InovaNotas.FluidHtmx.Assets;
using InovaNotas.FluidHtmx.Htmx;
using InovaNotas.FluidHtmx.Htmx.Tags;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Xunit;

namespace InovaNotas.FluidHtmx.Tests.Htmx;

public class HtmxTagsTests
{
    private readonly HtmxFluidParser _parser;
    private readonly TemplateOptions _templateOptions;

    public HtmxTagsTests()
    {
        _parser = new HtmxFluidParser();
        HtmxTagsRegistration.Register(_parser);
        _templateOptions = new TemplateOptions();
    }

    private async Task<string> RenderAsync(string template, Action<TemplateContext>? configure = null)
    {
        _parser.TryParse(template, out var parsed, out var error).Should().BeTrue(error ?? "");
        var context = new TemplateContext(_templateOptions);
        configure?.Invoke(context);
        return await parsed!.RenderAsync(context);
    }

    // ── hx_script ──

    [Fact]
    public async Task HxScript_RendersScriptTag()
    {
        var result = await RenderAsync("{% hx_script %}");

        result.Should().Contain("<script src=\"/js/htmx.min.js\"></script>");
    }

    // ── hx_link ──

    [Fact]
    public async Task HxLink_RendersAnchorWithDefaults()
    {
        var result = await RenderAsync("{% hx_link \"/about\" %}About{% endhx_link %}");

        result.Should().Contain("href=\"/about\"");
        result.Should().Contain("hx-get=\"/about\"");
        result.Should().Contain("hx-target=\"#main-content\"");
        result.Should().Contain("hx-swap=\"innerHTML\"");
        result.Should().Contain("hx-push-url=\"true\"");
    }

    [Fact]
    public async Task HxLink_RendersBodyContent()
    {
        var result = await RenderAsync("{% hx_link \"/about\" %}About Page{% endhx_link %}");

        result.Should().Contain(">About Page</a>");
    }

    [Fact]
    public async Task HxLink_OverridesWithNamedParams()
    {
        var result = await RenderAsync(
            "{% hx_link \"/about\", target: \"#sidebar\", swap: \"outerHTML\", push_url: \"false\" %}About{% endhx_link %}");

        result.Should().Contain("hx-target=\"#sidebar\"");
        result.Should().Contain("hx-swap=\"outerHTML\"");
        result.Should().Contain("hx-push-url=\"false\"");
    }

    [Fact]
    public async Task HxLink_NamedParamsDoNotLeakToNextTag()
    {
        var result = await RenderAsync(
            "{% hx_link \"/first\", target: \"#sidebar\" %}First{% endhx_link %}{% hx_link \"/second\" %}Second{% endhx_link %}");

        result.Should().Contain("hx-target=\"#sidebar\"");
        result.Should().Contain("hx-target=\"#main-content\"");
    }

    // ── hx_button ──

    [Fact]
    public async Task HxButton_DefaultsToPost()
    {
        var result = await RenderAsync("{% hx_button \"/api/delete\" %}Delete{% endhx_button %}");

        result.Should().Contain("<button");
        result.Should().Contain("hx-post=\"/api/delete\"");
        result.Should().Contain(">Delete</button>");
    }

    [Fact]
    public async Task HxButton_WithNamedParams()
    {
        var result = await RenderAsync(
            "{% hx_button \"/api/item\", method: \"delete\", target: \"#list\", swap: \"outerHTML\", confirm: \"Are you sure?\" %}Remove{% endhx_button %}");

        result.Should().Contain("hx-delete=\"/api/item\"");
        result.Should().Contain("hx-target=\"#list\"");
        result.Should().Contain("hx-swap=\"outerHTML\"");
        result.Should().Contain("hx-confirm=\"Are you sure?\"");
        result.Should().Contain(">Remove</button>");
    }

    [Fact]
    public async Task HxButton_MethodGet()
    {
        var result = await RenderAsync(
            "{% hx_button \"/partials/data\", method: \"get\", target: \"#content\" %}Load{% endhx_button %}");

        result.Should().Contain("hx-get=\"/partials/data\"");
        result.Should().Contain("hx-target=\"#content\"");
    }

    // ── hx_form ──

    [Fact]
    public async Task HxForm_DefaultsToPost()
    {
        var result = await RenderAsync("{% hx_form \"/submit\" %}<input name=\"name\" />{% endhx_form %}");

        result.Should().Contain("<form");
        result.Should().Contain("hx-post=\"/submit\"");
        result.Should().Contain("<input name=\"name\" />");
        result.Should().Contain("</form>");
    }

    [Fact]
    public async Task HxForm_WithNamedParams()
    {
        var result = await RenderAsync(
            "{% hx_form \"/update\", method: \"put\", target: \"#result\", swap: \"outerHTML\" %}...{% endhx_form %}");

        result.Should().Contain("hx-put=\"/update\"");
        result.Should().Contain("hx-target=\"#result\"");
        result.Should().Contain("hx-swap=\"outerHTML\"");
    }

    // ── hx_lazy ──

    [Fact]
    public async Task HxLazy_RendersWithDefaults()
    {
        var result = await RenderAsync("{% hx_lazy \"/partials/data\" %}Loading...{% endhx_lazy %}");

        result.Should().Contain("<div");
        result.Should().Contain("hx-get=\"/partials/data\"");
        result.Should().Contain("hx-trigger=\"load\"");
        result.Should().Contain("hx-swap=\"outerHTML\"");
        result.Should().Contain("Loading...");
        result.Should().Contain("</div>");
    }

    [Fact]
    public async Task HxLazy_WithNamedParams()
    {
        var result = await RenderAsync(
            "{% hx_lazy \"/partials/data\", trigger: \"revealed\", swap: \"innerHTML\" %}Placeholder{% endhx_lazy %}");

        result.Should().Contain("hx-trigger=\"revealed\"");
        result.Should().Contain("hx-swap=\"innerHTML\"");
    }

    // ── hx_swap_oob ──

    [Fact]
    public async Task HxSwapOob_RendersWithOobAttribute()
    {
        var result = await RenderAsync("{% hx_swap_oob \"#notifications\" %}New content{% endhx_swap_oob %}");

        result.Should().Contain("id=\"notifications\"");
        result.Should().Contain("hx-swap-oob=\"true\"");
        result.Should().Contain("New content");
    }

    [Fact]
    public async Task HxSwapOob_HandlesIdWithoutHash()
    {
        var result = await RenderAsync("{% hx_swap_oob \"alerts\" %}Alert!{% endhx_swap_oob %}");

        result.Should().Contain("id=\"alerts\"");
    }

    // ── csrf_token ──

    [Fact]
    public async Task CsrfToken_RendersHiddenInput()
    {
        var result = await RenderAsync("{% csrf_token %}", ctx =>
            ctx.SetValue("_antiforgery_token", "test-token-123"));

        result.Should().Contain("<input type=\"hidden\"");
        result.Should().Contain("name=\"__RequestVerificationToken\"");
        result.Should().Contain("value=\"test-token-123\"");
    }

    [Fact]
    public async Task CsrfToken_RendersEmptyValueWhenNoToken()
    {
        var result = await RenderAsync("{% csrf_token %}");

        result.Should().Contain("value=\"\"");
    }

    // ── hx_indicator ──

    [Fact]
    public async Task HxIndicator_RendersSpinner()
    {
        var result = await RenderAsync("{% hx_indicator %}");

        result.Should().Contain("class=\"htmx-indicator\"");
        result.Should().Contain("loading-spinner");
    }

    // ── asset_css ──

    [Fact]
    public async Task AssetCss_RendersLinkTag()
    {
        var result = await RenderAsync("{% asset_css \"/css/app.css\" %}");

        result.Should().Contain("<link rel=\"stylesheet\" href=\"/css/app.css\" />");
    }

    [Fact]
    public async Task AssetCss_WithManifest_RendersVersionedPath()
    {
        var content = "body { color: red; }"u8.ToArray();
        var fileInfo = Substitute.For<IFileInfo>();
        fileInfo.Exists.Returns(true);
        fileInfo.PhysicalPath.Returns("/wwwroot/css/app.css");
        fileInfo.CreateReadStream().Returns(_ => new MemoryStream(content));

        var fileProvider = Substitute.For<IFileProvider>();
        fileProvider.GetFileInfo("css/app.css").Returns(fileInfo);

        var env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns(Environments.Production);
        env.WebRootFileProvider.Returns(fileProvider);

        var manifest = new AssetManifest(env);

        var result = await RenderAsync("{% asset_css \"/css/app.css\" %}", ctx =>
            ctx.SetValue("_asset_manifest", manifest));

        result.Should().Contain("<link rel=\"stylesheet\" href=\"/css/app.css?v=");
        result.Should().Contain("\" />");
    }

    // ── asset_js ──

    [Fact]
    public async Task AssetJs_RendersScriptTag()
    {
        var result = await RenderAsync("{% asset_js \"/js/app.js\" %}");

        result.Should().Contain("<script src=\"/js/app.js\"></script>");
    }

    [Fact]
    public async Task AssetJs_WithManifest_RendersVersionedPath()
    {
        var content = "console.log('hello');"u8.ToArray();
        var fileInfo = Substitute.For<IFileInfo>();
        fileInfo.Exists.Returns(true);
        fileInfo.PhysicalPath.Returns("/wwwroot/js/app.js");
        fileInfo.CreateReadStream().Returns(_ => new MemoryStream(content));

        var fileProvider = Substitute.For<IFileProvider>();
        fileProvider.GetFileInfo("js/app.js").Returns(fileInfo);

        var env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns(Environments.Production);
        env.WebRootFileProvider.Returns(fileProvider);

        var manifest = new AssetManifest(env);

        var result = await RenderAsync("{% asset_js \"/js/app.js\" %}", ctx =>
            ctx.SetValue("_asset_manifest", manifest));

        result.Should().Contain("<script src=\"/js/app.js?v=");
        result.Should().Contain("\"></script>");
    }
}
