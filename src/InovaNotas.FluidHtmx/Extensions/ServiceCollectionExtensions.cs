using InovaNotas.FluidHtmx.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InovaNotas.FluidHtmx.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFluidHtmx(
        this IServiceCollection services,
        Action<FluidHtmxBuilder> configure)
    {
        var builder = new FluidHtmxBuilder(services);
        configure(builder);
        builder.Validate();
        builder.Build();
        return services;
    }
}
