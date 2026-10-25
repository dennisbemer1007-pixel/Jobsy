using System.Collections.Concurrent;
using System.Text.Json;

namespace Jobsy.Web.Hosting;

/// <summary>
/// Resolves <c>?v=</c> for wwwroot assets from the checked-in manifest (kept in sync with
/// <c>Jobsy.Tests/asset-versions.json</c> via <c>update-asset-versions.py</c>).
/// </summary>
public static class WwwrootAssetCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly Lazy<IReadOnlyDictionary<string, string>> Versions = new(LoadVersions);

    public static string Version(string relativeWwwrootPath)
    {
        var key = NormalizeKey(relativeWwwrootPath);
        if (Versions.Value.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
        {
            return v;
        }

        throw new InvalidOperationException(
            $"wwwroot-asset-versions.json has no version for '{key}'. Run: python3 Jobsy.Tests/update-asset-versions.py");
    }

    public static string Url(string relativeWwwrootPath)
    {
        var path = relativeWwwrootPath.TrimStart('~', '/');
        return $"{path}?v={Version(path)}";
    }

    public static string UrlLeadingSlash(string relativeWwwrootPath)
    {
        var path = relativeWwwrootPath.TrimStart('~').TrimStart('/');
        return $"/{path}?v={Version(path)}";
    }

    private static string NormalizeKey(string relativeWwwrootPath) =>
        relativeWwwrootPath.Replace('\\', '/').TrimStart('~', '/');

    private static IReadOnlyDictionary<string, string> LoadVersions()
    {
        var path = ResolveManifestPath();
        if (path is null || !File.Exists(path))
        {
            throw new InvalidOperationException(
                "Missing wwwroot-asset-versions.json — run python3 Jobsy.Tests/update-asset-versions.py");
        }

        var raw = JsonSerializer.Deserialize<Dictionary<string, ManifestEntry>>(File.ReadAllText(path), JsonOptions)
                  ?? throw new InvalidOperationException("wwwroot-asset-versions.json deserialized to null.");

        var map = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, entry) in raw)
        {
            if (!string.IsNullOrWhiteSpace(entry.V))
            {
                map[key] = entry.V;
            }
        }

        return map;
    }

    private static string? ResolveManifestPath()
    {
        var candidates = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, "Hosting", "wwwroot-asset-versions.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Hosting", "wwwroot-asset-versions.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Jobsy.Web", "Hosting", "wwwroot-asset-versions.json"),
        };

        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 6 && !string.IsNullOrEmpty(dir); i++)
        {
            candidates.Add(Path.Combine(dir, "Jobsy.Web", "Hosting", "wwwroot-asset-versions.json"));
            candidates.Add(Path.Combine(dir, "Jobsy.Tests", "asset-versions.json"));
            dir = Directory.GetParent(dir)?.FullName ?? "";
        }

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private sealed class ManifestEntry
    {
        public string V { get; set; } = "";
        public string Sha256 { get; set; } = "";
    }
}
