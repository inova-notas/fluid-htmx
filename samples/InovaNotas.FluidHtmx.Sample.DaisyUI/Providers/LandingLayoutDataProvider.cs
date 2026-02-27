using InovaNotas.FluidHtmx.Layouts;
using InovaNotas.FluidHtmx.Sample.DaisyUI.Layouts;

namespace InovaNotas.FluidHtmx.Sample.DaisyUI.Providers;

public class LandingLayoutDataProvider : ILayoutDataProvider<LandingLayout>
{
    public Task<Dictionary<string, object>> GetDataAsync(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value ?? "/";

        var data = new Dictionary<string, object>
        {
            ["app_name"] = "FluidHtmx",
            ["current_year"] = DateTime.UtcNow.Year,
            ["repo_url"] = "https://github.com/inovanotas/fluid-htmx",
            ["nav_sections"] = path == "/docs" ? GetDocsSections() : GetLandingSections()
        };

        return Task.FromResult(data);
    }

    private static object[] GetLandingSections() =>
    [
        new Dictionary<string, object> { ["id"] = "features", ["label"] = "Features" },
        new Dictionary<string, object> { ["id"] = "install", ["label"] = "Install" },
        new Dictionary<string, object> { ["id"] = "quick-start", ["label"] = "Quick Start" },
        new Dictionary<string, object> { ["id"] = "htmx-tags", ["label"] = "HTMX Tags" },
        new Dictionary<string, object> { ["id"] = "components", ["label"] = "Components" },
        new Dictionary<string, object> { ["id"] = "assets", ["label"] = "Assets" }
    ];

    private static object[] GetDocsSections() =>
    [
        new Dictionary<string, object> { ["id"] = "setup", ["label"] = "Setup" },
        new Dictionary<string, object> { ["id"] = "layouts", ["label"] = "Layouts" },
        new Dictionary<string, object> { ["id"] = "rendering", ["label"] = "Rendering" },
        new Dictionary<string, object> { ["id"] = "htmx-tags", ["label"] = "HTMX Tags" },
        new Dictionary<string, object> { ["id"] = "filters", ["label"] = "Filters" },
        new Dictionary<string, object> { ["id"] = "response-helpers", ["label"] = "Response Helpers" },
        new Dictionary<string, object> { ["id"] = "components", ["label"] = "Components" },
        new Dictionary<string, object> { ["id"] = "assets", ["label"] = "Assets" },
        new Dictionary<string, object> { ["id"] = "template-resolution", ["label"] = "Template Resolution" }
    ];
}
