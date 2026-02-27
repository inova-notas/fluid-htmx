using InovaNotas.FluidHtmx.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InovaNotas.FluidHtmx.Assets;

public class DaisyUISetupService : IHostedService
{
    private readonly IWebHostEnvironment _env;
    private readonly AssetOptions _assetOptions;
    private readonly FluidHtmxOptions _fluidOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DaisyUISetupService> _logger;

    private const string DaisyUIMjsFile = "daisyui.mjs";
    private const string DaisyUIThemeFile = "daisyui-theme.mjs";

    public DaisyUISetupService(
        IWebHostEnvironment env,
        IOptions<AssetOptions> assetOptions,
        IOptions<FluidHtmxOptions> fluidOptions,
        IHttpClientFactory httpClientFactory,
        ILogger<DaisyUISetupService> logger)
    {
        _env = env;
        _assetOptions = assetOptions.Value;
        _fluidOptions = fluidOptions.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_assetOptions.DaisyUIEnabled)
            return;

        var stylesDir = Path.Combine(_env.ContentRootPath, Path.GetDirectoryName(_assetOptions.InputCss)!);
        Directory.CreateDirectory(stylesDir);

        var version = NormalizeVersion(_assetOptions.DaisyUIVersion);

        await DownloadIfMissingAsync(stylesDir, DaisyUIMjsFile, version, cancellationToken);
        await DownloadIfMissingAsync(stylesDir, DaisyUIThemeFile, version, cancellationToken);

        ScaffoldCssIfMissing(stylesDir);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    internal static string NormalizeVersion(string version) =>
        version.StartsWith('v') ? version : $"v{version}";

    private async Task DownloadIfMissingAsync(string stylesDir, string fileName, string version, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(stylesDir, fileName);
        if (File.Exists(filePath))
        {
            _logger.LogDebug("DaisyUI file {FileName} already exists, skipping download", fileName);
            return;
        }

        var url = $"https://github.com/saadeghi/daisyui/releases/download/{version}/{fileName}";

        _logger.LogInformation("Downloading DaisyUI {FileName} from {Url}…", fileName, url);

        var client = _httpClientFactory.CreateClient("DaisyUI");
        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        await File.WriteAllBytesAsync(filePath, content, cancellationToken);

        _logger.LogInformation("Downloaded DaisyUI {FileName} ({Bytes} bytes)", fileName, content.Length);
    }

    private void ScaffoldCssIfMissing(string stylesDir)
    {
        var cssPath = Path.Combine(_env.ContentRootPath, _assetOptions.InputCss);
        if (File.Exists(cssPath))
        {
            _logger.LogDebug("CSS file {CssPath} already exists, skipping scaffold", cssPath);
            return;
        }

        var content = GenerateCssContent(stylesDir);
        File.WriteAllText(cssPath, content);
        _logger.LogInformation("Scaffolded DaisyUI CSS at {CssPath}", cssPath);
    }

    internal string GenerateCssContent(string stylesDir)
    {
        var templatesAbsolute = Path.GetFullPath(Path.Combine(_env.ContentRootPath, _fluidOptions.TemplatesPath));
        var relativePath = Path.GetRelativePath(stylesDir, templatesAbsolute).Replace('\\', '/');

        var lines = new List<string>
        {
            "@import \"tailwindcss\";",
            "",
            $"@source not \"./{DaisyUIMjsFile.Replace(".mjs", "")}{{,*}}.mjs\";",
            $"@source \"{relativePath}/**/*.liquid\";",
            ""
        };

        if (_assetOptions.DaisyUIThemes.Count > 0)
        {
            var firstTheme = _assetOptions.DaisyUIThemes[0];
            var otherThemes = _assetOptions.DaisyUIThemes.Skip(1).ToList();

            lines.Add($"@plugin \"./{DaisyUIMjsFile}\"{{");

            if (otherThemes.Count > 0)
            {
                var allThemes = string.Join(" ", _assetOptions.DaisyUIThemes.Select((t, i) =>
                    i == 0 ? $"{t} --default" : t));
                lines.Add($"    themes: {allThemes};");
            }
            else
            {
                lines.Add($"    themes: {firstTheme} --default;");
            }

            lines.Add("}");
        }
        else
        {
            lines.Add($"@plugin \"./{DaisyUIMjsFile}\";");
        }

        lines.Add($"@plugin \"./{DaisyUIThemeFile}\";");
        lines.Add("");

        return string.Join('\n', lines);
    }
}
