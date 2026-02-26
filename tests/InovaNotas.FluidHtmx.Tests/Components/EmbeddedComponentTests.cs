using Fluid;
using FluentAssertions;
using InovaNotas.FluidHtmx.Configuration;
using InovaNotas.FluidHtmx.Rendering;
using Microsoft.Extensions.FileProviders;
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
    [InlineData("alert")]
    [InlineData("badge")]
    [InlineData("breadcrumb")]
    [InlineData("pagination")]
    [InlineData("table")]
    [InlineData("empty-state")]
    [InlineData("toast")]
    [InlineData("confirm-dialog")]
    [InlineData("modal")]
    [InlineData("dropdown")]
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
    public async Task Alert_RendersWithTypeAndMessage()
    {
        var html = await RenderComponentAsync("alert", new Dictionary<string, object>
        {
            ["type"] = "success",
            ["message"] = "Operation completed"
        });

        html.Should().Contain("alert-success");
        html.Should().Contain("Operation completed");
    }

    [Fact]
    public async Task Badge_RendersWithTypeAndText()
    {
        var html = await RenderComponentAsync("badge", new Dictionary<string, object>
        {
            ["text"] = "New",
            ["type"] = "primary"
        });

        html.Should().Contain("badge-primary");
        html.Should().Contain("New");
    }

    [Fact]
    public async Task Badge_RendersOutlineVariant()
    {
        var html = await RenderComponentAsync("badge", new Dictionary<string, object>
        {
            ["text"] = "Draft",
            ["type"] = "warning",
            ["outline"] = true
        });

        html.Should().Contain("badge-outline");
        html.Should().Contain("badge-warning");
    }

    [Fact]
    public async Task Pagination_RendersCorrectPageLinks()
    {
        var html = await RenderComponentAsync("pagination", new Dictionary<string, object>
        {
            ["current_page"] = 2,
            ["total_pages"] = 5,
            ["base_url"] = "/users",
            ["target"] = "#user-list"
        });

        html.Should().Contain("hx-get=\"/users?page=1\"");
        html.Should().Contain("hx-get=\"/users?page=3\"");
        html.Should().Contain("hx-target=\"#user-list\"");
        html.Should().Contain("btn-active");
    }

    [Fact]
    public async Task Table_RendersHeadersAndRows()
    {
        var html = await RenderComponentAsync("table", new Dictionary<string, object>
        {
            ["headers"] = new List<string> { "Name", "Email" },
            ["rows"] = new List<List<string>>
            {
                new() { "Alice", "alice@example.com" },
                new() { "Bob", "bob@example.com" }
            },
            ["striped"] = true
        });

        html.Should().Contain("<th>Name</th>");
        html.Should().Contain("<th>Email</th>");
        html.Should().Contain("<td>Alice</td>");
        html.Should().Contain("<td>bob@example.com</td>");
        html.Should().Contain("table-striped");
    }

    [Fact]
    public async Task EmptyState_RendersWithTitleAndMessage()
    {
        var html = await RenderComponentAsync("empty-state", new Dictionary<string, object>
        {
            ["title"] = "No results",
            ["message"] = "Try a different search."
        });

        html.Should().Contain("No results");
        html.Should().Contain("Try a different search.");
    }

    [Fact]
    public async Task EmptyState_RendersActionButton()
    {
        var html = await RenderComponentAsync("empty-state", new Dictionary<string, object>
        {
            ["title"] = "No items",
            ["message"] = "Get started",
            ["action_label"] = "Create",
            ["action_url"] = "/items/new",
            ["target"] = "#content"
        });

        html.Should().Contain("Create");
        html.Should().Contain("hx-get=\"/items/new\"");
        html.Should().Contain("hx-target=\"#content\"");
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
    public async Task Dropdown_RendersWithItems()
    {
        var html = await RenderComponentAsync("dropdown", new Dictionary<string, object>
        {
            ["label"] = "Actions",
            ["items"] = new List<Dictionary<string, object>>
            {
                new() { ["label"] = "Edit", ["url"] = "/edit" },
                new() { ["label"] = "Delete", ["url"] = "/delete" }
            }
        });

        html.Should().Contain("Actions");
        html.Should().Contain("href=\"/edit\"");
        html.Should().Contain("href=\"/delete\"");
        html.Should().Contain("dropdown");
    }

    [Fact]
    public async Task Breadcrumb_RendersItems()
    {
        var html = await RenderComponentAsync("breadcrumb", new Dictionary<string, object>
        {
            ["items"] = new List<Dictionary<string, object>>
            {
                new() { ["label"] = "Home", ["url"] = "/" },
                new() { ["label"] = "Users", ["url"] = "/users" },
                new() { ["label"] = "Details" }
            }
        });

        html.Should().Contain("breadcrumbs");
        html.Should().Contain("href=\"/\"");
        html.Should().Contain("Home");
        html.Should().Contain("Details");
    }
}
