using System.Diagnostics;
using InovaNotas.FluidHtmx.Layouts;
using InovaNotas.FluidHtmx.Sample.DaisyUI.Layouts;

namespace InovaNotas.FluidHtmx.Sample.DaisyUI.Providers;

public class DaisyLayoutDataProvider : ILayoutDataProvider<DaisyLayout>
{
    public Task<Dictionary<string, object>> GetDataAsync(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value ?? "/";

        var navItems = new[]
        {
            new Dictionary<string, object>
            {
                ["label"] = "Home",
                ["url"] = "/",
                ["active"] = path == "/"
            },
            new Dictionary<string, object>
            {
                ["label"] = "Components",
                ["url"] = "/components",
                ["active"] = path == "/components"
            },
            new Dictionary<string, object>
            {
                ["label"] = "About",
                ["url"] = "/about",
                ["active"] = path == "/about"
            }
        };

        var pageTitle = path switch
        {
            "/components" => "Components",
            "/about" => "About",
            _ => "Home"
        };

        var renderTimeMs = 0.0;
        if (httpContext.Items["RequestStopwatch"] is Stopwatch sw)
            renderTimeMs = sw.Elapsed.TotalMilliseconds;

        var data = new Dictionary<string, object>
        {
            ["app_name"] = "FluidHtmx DaisyUI",
            ["nav_items"] = navItems,
            ["page_title"] = pageTitle,
            ["current_year"] = DateTime.UtcNow.Year,
            ["render_time_ms"] = renderTimeMs.ToString("F2")
        };

        return Task.FromResult(data);
    }
}
