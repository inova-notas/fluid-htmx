using Fluid;
using FluentAssertions;
using InovaNotas.FluidHtmx.Htmx.Filters;
using Xunit;

namespace InovaNotas.FluidHtmx.Tests.Htmx;

public class HtmxFiltersTests
{
    private readonly FluidParser _parser;
    private readonly TemplateOptions _templateOptions;

    public HtmxFiltersTests()
    {
        _parser = new FluidParser();
        _templateOptions = new TemplateOptions();
        HtmxFiltersRegistration.Register(_templateOptions);
    }

    private async Task<string> RenderAsync(string template, Action<TemplateContext>? configure = null)
    {
        _parser.TryParse(template, out var parsed, out var error).Should().BeTrue(error ?? "");
        var context = new TemplateContext(_templateOptions);
        configure?.Invoke(context);
        return (await parsed!.RenderAsync(context)).Trim();
    }

    [Fact]
    public async Task ToJson_SerializesObject()
    {
        var result = await RenderAsync("{{ data | to_json }}", ctx =>
            ctx.SetValue("data", new { name = "test", value = 42 }));

        result.Should().Contain("\"name\"");
        result.Should().Contain("\"test\"");
        result.Should().Contain("42");
    }

    [Fact]
    public async Task ToJson_SerializesString()
    {
        var result = await RenderAsync("{{ data | to_json }}", ctx =>
            ctx.SetValue("data", "hello"));

        result.Should().Contain("hello");
    }

    [Fact]
    public async Task ActiveClass_ReturnsClassWhenPathMatches()
    {
        var result = await RenderAsync("{{ \"/about\" | active_class: \"btn-active\" }}", ctx =>
            ctx.SetValue("_request_path", "/about"));

        result.Should().Be("btn-active");
    }

    [Fact]
    public async Task ActiveClass_ReturnsEmptyWhenPathDoesNotMatch()
    {
        var result = await RenderAsync("{{ \"/about\" | active_class: \"btn-active\" }}", ctx =>
            ctx.SetValue("_request_path", "/home"));

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ActiveClass_IsCaseInsensitive()
    {
        var result = await RenderAsync("{{ \"/About\" | active_class: \"active\" }}", ctx =>
            ctx.SetValue("_request_path", "/about"));

        result.Should().Be("active");
    }

    [Fact]
    public async Task Pluralize_ReturnsSingular()
    {
        var result = await RenderAsync("{{ 1 | pluralize: \"item\", \"items\" }}");

        result.Should().Be("item");
    }

    [Fact]
    public async Task Pluralize_ReturnsPlural()
    {
        var result = await RenderAsync("{{ 5 | pluralize: \"item\", \"items\" }}");

        result.Should().Be("items");
    }

    [Fact]
    public async Task Pluralize_ReturnsPluralForZero()
    {
        var result = await RenderAsync("{{ 0 | pluralize: \"item\", \"items\" }}");

        result.Should().Be("items");
    }

    [Fact]
    public async Task HxVals_SerializesToJson()
    {
        var result = await RenderAsync("{{ data | hx_vals }}", ctx =>
            ctx.SetValue("data", new { id = 1, action = "save" }));

        result.Should().Contain("\"id\"");
        result.Should().Contain("1");
        result.Should().Contain("\"action\"");
        result.Should().Contain("\"save\"");
    }

    [Fact]
    public async Task AppendQuery_AppendsParameter()
    {
        var result = await RenderAsync("{{ \"/search\" | append_query: \"q\", \"hello world\" }}");

        result.Should().Be("/search?q=hello%20world");
    }

    [Fact]
    public async Task AppendQuery_AppendsToExistingQuery()
    {
        var result = await RenderAsync("{{ \"/search?q=test\" | append_query: \"page\", \"2\" }}");

        result.Should().Be("/search?q=test&page=2");
    }

    [Fact]
    public async Task EscapeAttr_EncodesHtmlCharacters()
    {
        var result = await RenderAsync("{{ value | escape_attr }}", ctx =>
            ctx.SetValue("value", "<script>alert('xss')</script>"));

        result.Should().NotContain("<script>");
        result.Should().Contain("&lt;script&gt;");
    }
}
