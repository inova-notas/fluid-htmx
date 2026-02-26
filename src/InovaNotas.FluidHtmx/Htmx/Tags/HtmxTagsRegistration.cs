using System.IO;
using System.Text.Encodings.Web;
using Fluid;
using Fluid.Ast;
using Fluid.Values;
using InovaNotas.FluidHtmx.Assets;

namespace InovaNotas.FluidHtmx.Htmx.Tags;

public static class HtmxTagsRegistration
{
    private const string DefaultTarget = "#main-content";
    private const string DefaultSwap = "innerHTML";

    public static void Register(HtmxFluidParser parser)
    {
        RegisterHxScript(parser);
        RegisterHxLink(parser);
        RegisterHxButton(parser);
        RegisterHxForm(parser);
        RegisterHxLazy(parser);
        RegisterHxSwapOob(parser);
        RegisterCsrfToken(parser);
        RegisterHxIndicator(parser);
        RegisterAssetCss(parser);
        RegisterAssetJs(parser);
    }

    private static void RegisterHxScript(HtmxFluidParser parser)
    {
        parser.RegisterEmptyTag("hx_script", static async (writer, encoder, context) =>
        {
            await writer.WriteAsync("<script src=\"/js/htmx.min.js\"></script>");
            return Completion.Normal;
        });
    }

    private static void RegisterHxLink(HtmxFluidParser parser)
    {
        parser.RegisterParserBlock("hx_link", parser.ArgumentsListParser,
            static async (args, statements, writer, encoder, context) =>
        {
            var p = new TagParams(args, context);
            var url = await p.RequirePositional();
            var target = p.Get("target") ?? DefaultTarget;
            var swap = p.Get("swap") ?? DefaultSwap;
            var pushUrl = p.Get("push_url") ?? "true";

            await writer.WriteAsync($"<a href=\"{Enc(url)}\" hx-get=\"{Enc(url)}\" hx-target=\"{Enc(target)}\" hx-swap=\"{Enc(swap)}\" hx-push-url=\"{Enc(pushUrl)}\">");
            await RenderStatementsAsync(statements, writer, encoder, context);
            await writer.WriteAsync("</a>");

            return Completion.Normal;
        });
    }

    private static void RegisterHxButton(HtmxFluidParser parser)
    {
        parser.RegisterParserBlock("hx_button", parser.ArgumentsListParser,
            static async (args, statements, writer, encoder, context) =>
        {
            var p = new TagParams(args, context);
            var url = await p.RequirePositional();
            var method = p.Get("method") ?? "post";
            var target = p.Get("target");
            var swap = p.Get("swap");
            var confirm = p.Get("confirm");
            var cssClass = p.Get("class");

            await writer.WriteAsync($"<button hx-{method.ToLowerInvariant()}=\"{Enc(url)}\"");

            if (target is not null)
                await writer.WriteAsync($" hx-target=\"{Enc(target)}\"");
            if (swap is not null)
                await writer.WriteAsync($" hx-swap=\"{Enc(swap)}\"");
            if (confirm is not null)
                await writer.WriteAsync($" hx-confirm=\"{Enc(confirm)}\"");
            if (cssClass is not null)
                await writer.WriteAsync($" class=\"{Enc(cssClass)}\"");

            await writer.WriteAsync(">");
            await RenderStatementsAsync(statements, writer, encoder, context);
            await writer.WriteAsync("</button>");

            return Completion.Normal;
        });
    }

    private static void RegisterHxForm(HtmxFluidParser parser)
    {
        parser.RegisterParserBlock("hx_form", parser.ArgumentsListParser,
            static async (args, statements, writer, encoder, context) =>
        {
            var p = new TagParams(args, context);
            var url = await p.RequirePositional();
            var method = p.Get("method") ?? "post";
            var target = p.Get("target");
            var swap = p.Get("swap");

            await writer.WriteAsync($"<form hx-{method.ToLowerInvariant()}=\"{Enc(url)}\"");

            if (target is not null)
                await writer.WriteAsync($" hx-target=\"{Enc(target)}\"");
            if (swap is not null)
                await writer.WriteAsync($" hx-swap=\"{Enc(swap)}\"");

            await writer.WriteAsync(">");
            await RenderStatementsAsync(statements, writer, encoder, context);
            await writer.WriteAsync("</form>");

            return Completion.Normal;
        });
    }

    private static void RegisterHxLazy(HtmxFluidParser parser)
    {
        parser.RegisterParserBlock("hx_lazy", parser.ArgumentsListParser,
            static async (args, statements, writer, encoder, context) =>
        {
            var p = new TagParams(args, context);
            var url = await p.RequirePositional();
            var trigger = p.Get("trigger") ?? "load";
            var swap = p.Get("swap") ?? "outerHTML";

            await writer.WriteAsync($"<div hx-get=\"{Enc(url)}\" hx-trigger=\"{Enc(trigger)}\" hx-swap=\"{Enc(swap)}\">");
            await RenderStatementsAsync(statements, writer, encoder, context);
            await writer.WriteAsync("</div>");

            return Completion.Normal;
        });
    }

    private static void RegisterHxSwapOob(HtmxFluidParser parser)
    {
        parser.RegisterParserBlock("hx_swap_oob", parser.ArgumentsListParser,
            static async (args, statements, writer, encoder, context) =>
        {
            var p = new TagParams(args, context);
            var selector = await p.RequirePositional();
            var id = selector.TrimStart('#');

            await writer.WriteAsync($"<div id=\"{Enc(id)}\" hx-swap-oob=\"true\">");
            await RenderStatementsAsync(statements, writer, encoder, context);
            await writer.WriteAsync("</div>");

            return Completion.Normal;
        });
    }

    private static void RegisterCsrfToken(HtmxFluidParser parser)
    {
        parser.RegisterEmptyTag("csrf_token", static async (writer, encoder, context) =>
        {
            var token = GetContextString(context, "_antiforgery_token") ?? "";
            await writer.WriteAsync($"<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"{Enc(token)}\" />");
            return Completion.Normal;
        });
    }

    private static void RegisterHxIndicator(HtmxFluidParser parser)
    {
        parser.RegisterEmptyTag("hx_indicator", static async (writer, encoder, context) =>
        {
            await writer.WriteAsync("<span class=\"htmx-indicator\"><span class=\"loading loading-spinner\"></span></span>");
            return Completion.Normal;
        });
    }

    private static void RegisterAssetCss(HtmxFluidParser parser)
    {
        parser.RegisterParserTag("asset_css", parser.ArgumentsListParser,
            static async (args, writer, encoder, context) =>
        {
            var p = new TagParams(args, context);
            var path = await p.RequirePositional();
            var resolved = ResolveAssetPath(context, path);

            await writer.WriteAsync($"<link rel=\"stylesheet\" href=\"{Enc(resolved)}\" />");
            return Completion.Normal;
        });
    }

    private static void RegisterAssetJs(HtmxFluidParser parser)
    {
        parser.RegisterParserTag("asset_js", parser.ArgumentsListParser,
            static async (args, writer, encoder, context) =>
        {
            var p = new TagParams(args, context);
            var path = await p.RequirePositional();
            var resolved = ResolveAssetPath(context, path);

            await writer.WriteAsync($"<script src=\"{Enc(resolved)}\"></script>");
            return Completion.Normal;
        });
    }

    private static string ResolveAssetPath(TemplateContext context, string path)
    {
        var manifestValue = context.GetValue("_asset_manifest");
        if (manifestValue is not NilValue && !manifestValue.IsNil() && manifestValue.ToObjectValue() is AssetManifest manifest)
            return manifest.Resolve(path);
        return path;
    }

    private static async Task RenderStatementsAsync(
        IReadOnlyList<Statement> statements,
        TextWriter writer,
        TextEncoder encoder,
        TemplateContext context)
    {
        foreach (var statement in statements)
        {
            await statement.WriteToAsync(writer, encoder, context);
        }
    }

    private static string? GetContextString(TemplateContext context, string name)
    {
        var value = context.GetValue(name);
        if (value is NilValue || value.IsNil())
            return null;
        var str = value.ToStringValue();
        return string.IsNullOrEmpty(str) ? null : str;
    }

    private static string Enc(string value)
    {
        return HtmlEncoder.Default.Encode(value);
    }

    /// <summary>
    /// Helper to extract positional and named parameters from a tag's argument list.
    /// </summary>
    private readonly struct TagParams
    {
        private readonly IReadOnlyList<FilterArgument> _args;
        private readonly TemplateContext _context;
        private readonly Dictionary<string, string> _named;
        private readonly List<FilterArgument> _positional;

        public TagParams(IReadOnlyList<FilterArgument> args, TemplateContext context)
        {
            _args = args;
            _context = context;
            _named = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _positional = [];

            foreach (var arg in args)
            {
                if (string.IsNullOrEmpty(arg.Name))
                    _positional.Add(arg);
                else
                {
                    var val = arg.Expression.EvaluateAsync(context).GetAwaiter().GetResult();
                    var str = val.ToStringValue();
                    if (!string.IsNullOrEmpty(str))
                        _named[arg.Name] = str;
                }
            }
        }

        public async ValueTask<string> RequirePositional()
        {
            if (_positional.Count == 0)
                return "";
            var val = await _positional[0].Expression.EvaluateAsync(_context);
            return val.ToStringValue();
        }

        public string? Get(string name)
        {
            return _named.TryGetValue(name, out var value) ? value : null;
        }
    }
}
