using System.Diagnostics;
using InovaNotas.FluidHtmx.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InovaNotas.FluidHtmx.Assets;

public class TailwindWatchService : BackgroundService
{
    private readonly IWebHostEnvironment _env;
    private readonly AssetOptions _options;
    private readonly ILogger<TailwindWatchService> _logger;

    public TailwindWatchService(
        IWebHostEnvironment env,
        IOptions<AssetOptions> options,
        ILogger<TailwindWatchService> logger)
    {
        _env = env;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_env.IsDevelopment() || !_options.TailwindEnabled)
            return;

        _logger.LogInformation("Starting Tailwind CSS watch (v{Version})…", _options.TailwindVersion);

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"tool run tailwindcss watch -i {_options.InputCss} -o {_options.OutputCss}",
            WorkingDirectory = _env.ContentRootPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process? process = null;
        try
        {
            process = Process.Start(psi);
            if (process is null)
            {
                _logger.LogWarning("Failed to start Tailwind CSS watch process");
                return;
            }

            _ = ReadStreamAsync(process.StandardOutput, LogLevel.Information, stoppingToken);
            _ = ReadStreamAsync(process.StandardError, LogLevel.Warning, stoppingToken);

            await process.WaitForExitAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Stopping Tailwind CSS watch…");
        }
        finally
        {
            if (process is { HasExited: false })
            {
                process.Kill(entireProcessTree: true);
                _logger.LogInformation("Tailwind CSS watch process stopped");
            }

            process?.Dispose();
        }
    }

    private async Task ReadStreamAsync(StreamReader reader, LogLevel level, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;
            _logger.Log(level, "[Tailwind] {Line}", line);
        }
    }
}
