using System.Net;
using System.Text.RegularExpressions;
using InovaNotas.FluidHtmx.Exceptions;
using InovaNotas.FluidHtmx.Rendering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InovaNotas.FluidHtmx.Middleware;

public class DevelopmentErrorPageMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<DevelopmentErrorPageMiddleware> _logger;

    public DevelopmentErrorPageMiddleware(
        RequestDelegate next,
        IWebHostEnvironment env,
        ILogger<DevelopmentErrorPageMiddleware> logger)
    {
        _next = next;
        _env = env;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_env.IsDevelopment())
        {
            await _next(context);
            return;
        }

        try
        {
            await _next(context);
        }
        catch (TemplateParseException ex)
        {
            _logger.LogError(ex, "Template parse error in '{TemplateName}'", ex.TemplateName);
            await RenderParseErrorPage(context, ex);
        }
        catch (TemplateNotFoundException ex)
        {
            _logger.LogError(ex, "Template not found: '{TemplateName}'", ex.TemplateName);
            await RenderNotFoundErrorPage(context, ex);
        }
    }

    private static readonly Regex ErrorLineRegex = new(@"at \((\d+):(\d+)\)", RegexOptions.Compiled);

    private async Task RenderParseErrorPage(HttpContext context, TemplateParseException ex)
    {
        var errorLine = ParseErrorLine(ex.Message);
        var templateSource = await TryReadTemplateSource(context, ex.TemplateName);

        var sourceHtml = templateSource is not null
            ? RenderSourceContext(templateSource, errorLine)
            : "<p>Source not available.</p>";

        var html = BuildErrorPage(
            "Template Parse Error",
            $"<strong>Template:</strong> {WebUtility.HtmlEncode(ex.TemplateName)}.liquid",
            $"<strong>Error:</strong> {WebUtility.HtmlEncode(ex.Message)}",
            sourceHtml);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(html);
    }

    private static async Task RenderNotFoundErrorPage(HttpContext context, TemplateNotFoundException ex)
    {
        var html = BuildErrorPage(
            "Template Not Found",
            $"<strong>Template:</strong> {WebUtility.HtmlEncode(ex.TemplateName)}.liquid",
            $"<strong>Error:</strong> {WebUtility.HtmlEncode(ex.Message)}",
            null);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(html);
    }

    private static async Task<string?> TryReadTemplateSource(HttpContext context, string templateName)
    {
        try
        {
            var locator = context.RequestServices.GetService(typeof(TemplateLocator)) as TemplateLocator;
            if (locator is null) return null;
            return await locator.ReadTemplateAsync(templateName);
        }
        catch
        {
            return null;
        }
    }

    private static int? ParseErrorLine(string message)
    {
        var match = ErrorLineRegex.Match(message);
        return match.Success && int.TryParse(match.Groups[1].Value, out var line) ? line : null;
    }

    private static string RenderSourceContext(string source, int? errorLine = null)
    {
        var lines = source.Split('\n');
        var sb = new System.Text.StringBuilder();

        sb.Append("<pre style=\"background:#1e1e2e;color:#cdd6f4;padding:16px;border-radius:8px;overflow-x:auto;font-size:14px;line-height:1.6;\">");

        for (var i = 0; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var lineNum = lineNumber.ToString().PadLeft(4);
            var encoded = WebUtility.HtmlEncode(lines[i].TrimEnd('\r'));
            var isError = errorLine.HasValue && lineNumber == errorLine.Value;

            if (isError)
            {
                sb.Append($"<span style=\"display:inline-block;width:100%;background:#f38ba820;border-left:3px solid #f38ba8;padding-left:4px;\">");
                sb.Append($"<span style=\"color:#f38ba8;user-select:none;font-weight:bold;\">{lineNum} | </span>{encoded}");
                sb.Append("</span>\n");
            }
            else
            {
                sb.Append($"<span style=\"color:#6c7086;user-select:none;\">{lineNum} | </span>{encoded}\n");
            }
        }

        sb.Append("</pre>");
        return sb.ToString();
    }

    private static string BuildErrorPage(string title, string templateInfo, string errorInfo, string? sourceHtml)
    {
        var sourceSection = sourceHtml is not null
            ? $"<h2 style=\"margin-top:24px;font-size:16px;color:#a6adc8;\">Template Source</h2>{sourceHtml}"
            : "";

        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8"/>
                <title>FluidHtmx - {{WebUtility.HtmlEncode(title)}}</title>
                <style>
                    body {
                        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                        background: #1e1e2e;
                        color: #cdd6f4;
                        margin: 0;
                        padding: 40px;
                    }
                    .container {
                        max-width: 900px;
                        margin: 0 auto;
                    }
                    h1 {
                        color: #f38ba8;
                        font-size: 24px;
                        border-bottom: 2px solid #f38ba8;
                        padding-bottom: 12px;
                    }
                    .info {
                        background: #313244;
                        padding: 16px;
                        border-radius: 8px;
                        margin: 16px 0;
                        line-height: 1.8;
                    }
                    .info strong {
                        color: #89b4fa;
                    }
                    .footer {
                        margin-top: 32px;
                        font-size: 12px;
                        color: #6c7086;
                    }
                </style>
            </head>
            <body>
                <div class="container">
                    <h1>{{WebUtility.HtmlEncode(title)}}</h1>
                    <div class="info">
                        <p>{{templateInfo}}</p>
                        <p>{{errorInfo}}</p>
                    </div>
                    {{sourceSection}}
                    <div class="footer">
                        FluidHtmx Development Error Page &mdash; This page is only shown in Development environment.
                    </div>
                </div>
            </body>
            </html>
            """;
    }
}
