using System.Collections.Concurrent;
using System.Diagnostics;

namespace Jobsy.Web.Services;

/// <summary>
/// Development-only counter of outbound API calls attributed to the current page path.
/// </summary>
public sealed class ApiCallTracker
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ApiCallTracker> _logger;
    private readonly ConcurrentDictionary<string, int> _counts = new(StringComparer.OrdinalIgnoreCase);
    private string _currentPath = "/";

    public ApiCallTracker(IHostEnvironment environment, ILogger<ApiCallTracker> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public bool IsEnabled => _environment.IsDevelopment();

    public void SetPath(string? path)
    {
        if (!IsEnabled)
        {
            return;
        }

        var normalized = Normalize(path);
        if (string.Equals(_currentPath, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Flush(_currentPath);
        _currentPath = normalized;
        _counts[_currentPath] = 0;
    }

    public void Record(Uri? requestUri)
    {
        if (!IsEnabled || requestUri is null)
        {
            return;
        }

        var path = requestUri.IsAbsoluteUri ? requestUri.AbsolutePath : requestUri.OriginalString;
        if (path.Contains("/health", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/notifications/", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/notifications", StringComparison.OrdinalIgnoreCase))
        {
            // Shell polling — not attributed to the active page budget.
            return;
        }

        _counts.AddOrUpdate(_currentPath, 1, static (_, n) => n + 1);
    }

    public void Flush(string? path = null)
    {
        if (!IsEnabled)
        {
            return;
        }

        var key = string.IsNullOrWhiteSpace(path) ? _currentPath : Normalize(path);
        if (_counts.TryGetValue(key, out var n) && n > 0)
        {
            _logger.LogInformation("API calls for page {Path}: {Count}", key, n);
            Debug.WriteLine($"[Jobsy] API calls for page {key}: {n}");
        }
    }

    private static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var trimmed = path.Trim();
        var q = trimmed.IndexOf('?', StringComparison.Ordinal);
        if (q >= 0)
        {
            trimmed = trimmed[..q];
        }

        return string.IsNullOrEmpty(trimmed) ? "/" : trimmed;
    }
}
