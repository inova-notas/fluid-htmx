using InovaNotas.FluidHtmx.Htmx;
using InovaNotas.FluidHtmx.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;

namespace InovaNotas.FluidHtmx.Extensions;

public static class EndpointExtensions
{
    public static IApplicationBuilder UseHtmx(this IApplicationBuilder app)
    {
        app.UseMiddleware<DevelopmentErrorPageMiddleware>();
        app.UseMiddleware<HtmxMiddleware>();

        var provider = new ManifestEmbeddedFileProvider(
            typeof(EndpointExtensions).Assembly, "wwwroot");
        app.UseStaticFiles(new StaticFileOptions { FileProvider = provider });

        return app;
    }
}
