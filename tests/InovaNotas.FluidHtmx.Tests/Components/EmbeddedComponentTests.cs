using Fluid;
using FluentAssertions;
using InovaNotas.FluidHtmx.Configuration;
using InovaNotas.FluidHtmx.Rendering;
using Microsoft.Extensions.Options;
using Xunit;

namespace InovaNotas.FluidHtmx.Tests.Components;

public class EmbeddedComponentTests
{
    private static readonly FluidParser Parser = new();

    private static async Task<string> RenderComponentAsync(string componentName, Dictionary<string, object> parameters)
    {
        var source = await ReadEmbeddedComponentAsync(componentName);
        Parser.TryParse(source, out var template, out var error).Should().BeTrue(error);

        var context = new TemplateContext();
        foreach (var kvp in parameters)
        {
            context.SetValue(kvp.Key, kvp.Value);
        }

        return await template!.RenderAsync(context);
    }

    private static async Task<string> ReadEmbeddedComponentAsync(string componentName)
    {
        var options = Options.Create(new FluidHtmxOptions());
        var locator = new TemplateLocator(options, physicalProvider: null);
        return await locator.ReadTemplateAsync($"components/{componentName}");
    }

    [Theory]
    [InlineData("toast")]
    [InlineData("confirm-dialog")]
    [InlineData("modal")]
    public async Task AllComponents_CanBeRendered(string componentName)
    {
        var source = await ReadEmbeddedComponentAsync(componentName);
        var parsed = Parser.TryParse(source, out var template, out var error);

        parsed.Should().BeTrue($"component '{componentName}' should parse without errors: {error}");

        var context = new TemplateContext();
        var html = await template!.RenderAsync(context);

        html.Should().NotBeNull();
    }

    [Fact]
    public async Task Toast_RendersWithMessage()
    {
        var html = await RenderComponentAsync("toast", new Dictionary<string, object>
        {
            ["message"] = "Saved!",
            ["type"] = "success"
        });

        html.Should().Contain("alert-success");
        html.Should().Contain("Saved!");
        html.Should().Contain("toast-end");
    }

    [Fact]
    public async Task Toast_RendersAutoDismiss()
    {
        var html = await RenderComponentAsync("toast", new Dictionary<string, object>
        {
            ["message"] = "Done",
            ["type"] = "info",
            ["duration"] = 5000
        });

        html.Should().Contain("hx-on::load");
        html.Should().Contain("5000");
    }

    [Fact]
    public async Task ConfirmDialog_RendersDialogElement()
    {
        var html = await RenderComponentAsync("confirm-dialog", new Dictionary<string, object>
        {
            ["id"] = "delete-user",
            ["title"] = "Delete user?",
            ["message"] = "This cannot be undone.",
            ["confirm_url"] = "/users/1",
            ["confirm_method"] = "delete"
        });

        html.Should().Contain("<dialog id=\"delete-user\"");
        html.Should().Contain("Delete user?");
        html.Should().Contain("This cannot be undone.");
        html.Should().Contain("hx-delete=\"/users/1\"");
        html.Should().Contain("modal");
    }

    [Fact]
    public async Task ConfirmDialog_RendersCustomLabels()
    {
        var html = await RenderComponentAsync("confirm-dialog", new Dictionary<string, object>
        {
            ["id"] = "archive",
            ["title"] = "Archive?",
            ["message"] = "Item will be archived.",
            ["confirm_url"] = "/items/1/archive",
            ["confirm_method"] = "post",
            ["confirm_label"] = "Yes, archive",
            ["cancel_label"] = "No, keep it"
        });

        html.Should().Contain("Yes, archive");
        html.Should().Contain("No, keep it");
        html.Should().Contain("hx-post=\"/items/1/archive\"");
    }

    [Fact]
    public async Task Modal_RendersHtmxAttributes()
    {
        var html = await RenderComponentAsync("modal", new Dictionary<string, object>
        {
            ["id"] = "edit-user",
            ["title"] = "Edit User",
            ["url"] = "/users/1/edit",
            ["trigger_label"] = "Edit",
            ["trigger_class"] = "btn btn-primary"
        });

        html.Should().Contain("<dialog id=\"edit-user\"");
        html.Should().Contain("Edit User");
        html.Should().Contain("hx-get=\"/users/1/edit\"");
        html.Should().Contain("btn btn-primary");
        html.Should().Contain("Edit");
    }

    [Fact]
    public async Task Modal_RendersLargeSize()
    {
        var html = await RenderComponentAsync("modal", new Dictionary<string, object>
        {
            ["id"] = "lg-modal",
            ["url"] = "/content",
            ["trigger_label"] = "Open",
            ["size"] = "lg"
        });

        html.Should().Contain("max-w-5xl");
    }
}
