using InovaNotas.FluidHtmx.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InovaNotas.FluidHtmx.Rendering;

public class TemplateFileWatcher : BackgroundService
{
    private readonly IWebHostEnvironment _env;
    private readonly FluidHtmxOptions _options;
    private readonly TemplateCache _cache;
    private readonly ILogger<TemplateFileWatcher> _logger;

    private FileSystemWatcher? _watcher;
    private Timer? _debounceTimer;
    private readonly HashSet<string> _pendingInvalidations = [];
    private readonly object _lock = new();

    private static readonly TimeSpan DebounceInterval = TimeSpan.FromMilliseconds(50);

    public TemplateFileWatcher(
        IWebHostEnvironment env,
        IOptions<FluidHtmxOptions> options,
        TemplateCache cache,
        ILogger<TemplateFileWatcher> logger)
    {
        _env = env;
        _options = options.Value;
        _cache = cache;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableHotReload)
            return Task.CompletedTask;

        var templatesPath = Path.Combine(_env.ContentRootPath, _options.TemplatesPath);

        if (!Directory.Exists(templatesPath))
        {
            _logger.LogWarning("Templates path '{Path}' does not exist, file watcher not started", templatesPath);
            return Task.CompletedTask;
        }

        _watcher = new FileSystemWatcher(templatesPath, "*.liquid")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Deleted += OnFileChanged;
        _watcher.Renamed += OnFileRenamed;

        _watcher.EnableRaisingEvents = true;

        _logger.LogInformation("Watching for template changes in '{Path}'", templatesPath);

        stoppingToken.Register(DisposeWatcher);

        return Task.CompletedTask;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        var templateName = ConvertToTemplateName(e.FullPath);
        ScheduleInvalidation(templateName);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        var oldName = ConvertToTemplateName(e.OldFullPath);
        var newName = ConvertToTemplateName(e.FullPath);
        ScheduleInvalidation(oldName);
        ScheduleInvalidation(newName);
    }

    private void ScheduleInvalidation(string templateName)
    {
        lock (_lock)
        {
            _pendingInvalidations.Add(templateName);
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(FlushInvalidations, null, DebounceInterval, Timeout.InfiniteTimeSpan);
        }
    }

    private void FlushInvalidations(object? state)
    {
        string[] names;
        lock (_lock)
        {
            names = [.. _pendingInvalidations];
            _pendingInvalidations.Clear();
        }

        foreach (var name in names)
        {
            _cache.Invalidate(name);
            _logger.LogDebug("Template cache invalidated: {TemplateName}", name);
        }
    }

    public string ConvertToTemplateName(string filePath)
    {
        var basePath = Path.Combine(_env.ContentRootPath, _options.TemplatesPath);

        var relativePath = Path.GetRelativePath(basePath, filePath);

        // Strip .liquid extension and normalize separators
        if (relativePath.EndsWith(".liquid", StringComparison.OrdinalIgnoreCase))
            relativePath = relativePath[..^".liquid".Length];

        return relativePath.Replace('\\', '/');
    }

    private void DisposeWatcher()
    {
        _debounceTimer?.Dispose();
        _debounceTimer = null;

        if (_watcher is not null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
            _logger.LogInformation("Template file watcher stopped");
        }
    }

    public override void Dispose()
    {
        DisposeWatcher();
        base.Dispose();
    }
}
