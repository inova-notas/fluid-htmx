using System.Text.Encodings.Web;
using System.Text.Json;
using Fluid;
using Fluid.Values;

namespace InovaNotas.FluidHtmx.Htmx.Filters;

public static class HtmxFiltersRegistration
{
    public static void Register(TemplateOptions options)
    {
        options.Filters.AddFilter("to_json", ToJson);
        options.Filters.AddFilter("hx_vals", HxVals);
        options.Filters.AddFilter("active_class", ActiveClass);
        options.Filters.AddFilter("pluralize", Pluralize);
        options.Filters.AddFilter("append_query", AppendQuery);
        options.Filters.AddFilter("escape_attr", EscapeAttr);
    }

    private static ValueTask<FluidValue> ToJson(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        var obj = input.ToObjectValue();
        var json = JsonSerializer.Serialize(obj);
        return new ValueTask<FluidValue>(new StringValue(json));
    }

    private static ValueTask<FluidValue> HxVals(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        var obj = input.ToObjectValue();
        var json = JsonSerializer.Serialize(obj);
        return new ValueTask<FluidValue>(new StringValue(json));
    }

    private static ValueTask<FluidValue> ActiveClass(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        var path = input.ToStringValue();
        var className = arguments.At(0).ToStringValue();
        var requestPath = context.GetValue("_request_path")?.ToStringValue() ?? "";

        var isActive = string.Equals(path, requestPath, StringComparison.OrdinalIgnoreCase);
        var result = isActive ? className : "";

        return new ValueTask<FluidValue>(new StringValue(result));
    }

    private static ValueTask<FluidValue> Pluralize(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        var count = (int)input.ToNumberValue();
        var singular = arguments.At(0).ToStringValue();
        var plural = arguments.At(1).ToStringValue();

        return new ValueTask<FluidValue>(new StringValue(count == 1 ? singular : plural));
    }

    private static ValueTask<FluidValue> AppendQuery(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        var url = input.ToStringValue();
        var key = arguments.At(0).ToStringValue();
        var value = arguments.At(1).ToStringValue();

        var separator = url.Contains('?') ? "&" : "?";
        var result = $"{url}{separator}{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";

        return new ValueTask<FluidValue>(new StringValue(result));
    }

    private static ValueTask<FluidValue> EscapeAttr(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        var value = input.ToStringValue();
        var encoded = HtmlEncoder.Default.Encode(value);
        return new ValueTask<FluidValue>(new StringValue(encoded));
    }
}
