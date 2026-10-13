using System.Text.Json;

namespace Jobsy.Tests;

/// <summary>
/// Reads <c>Jobsy.Tests/asset-versions.json</c> so tests assert that a host
/// file uses <c>?v=</c> matching the checked-in guard, instead of hard-coding
/// literal version tags that drift on every asset bump.
/// </summary>
internal static class AssetVersions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly Lazy<Dictionary<string, Entry>> Manifest = new(Load);

    public static string Version(string relativeWwwrootPath)
    {
        var key = relativeWwwrootPath.Replace('\\', '/').TrimStart('/');
        if (!Manifest.Value.TryGetValue(key, out var entry) || string.IsNullOrWhiteSpace(entry.V))
        {
            throw new InvalidOperationException(
                $"asset-versions.json has no version for '{key}'. Run: python3 Jobsy.Tests/update-asset-versions.py");
        }

        return entry.V;
    }

    /// <summary>
    /// Asserts <paramref name="hostText"/> references <paramref name="relativeWwwrootPath"/>
    /// with the manifest <c>?v=</c> (and therefore that a version query exists).
    /// </summary>
    public static void AssertHostUsesManifestVersion(string hostText, string relativeWwwrootPath)
    {
        var key = relativeWwwrootPath.Replace('\\', '/').TrimStart('/');
        var version = Version(key);
        var needle = $"{key}?v={version}";
        if (!hostText.Contains(needle, StringComparison.Ordinal))
        {
            // Some hosts use a leading slash (/js/...).
            var alt = $"/{needle}";
            Assert.True(
                hostText.Contains(alt, StringComparison.Ordinal),
                $"Expected host text to contain '{needle}' or '{alt}' (from asset-versions.json).");
            return;
        }

        Assert.Contains(needle, hostText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Asserts a versioned reference exists for the asset (any <c>?v=</c>), and that
    /// it matches the manifest when the concrete tag can be parsed.
    /// </summary>
    public static void AssertVersionedRefMatchesManifest(string hostText, string relativeWwwrootPath)
    {
        var key = relativeWwwrootPath.Replace('\\', '/').TrimStart('/');
        var pattern = System.Text.RegularExpressions.Regex.Escape(key) + @"\?v=([^""'\s]+)";
        var match = System.Text.RegularExpressions.Regex.Match(hostText, pattern);
        Assert.True(match.Success, $"Expected a ?v= reference to {key} in host text.");
        var actual = match.Groups[1].Value;
        var expected = Version(key);
        Assert.Equal(expected, actual);
    }

    private static Dictionary<string, Entry> Load()
    {
        var path = Path.Combine(TestRepo.FindRoot(), "Jobsy.Tests", "asset-versions.json");
        Assert.True(File.Exists(path), "Missing Jobsy.Tests/asset-versions.json");
        var data = JsonSerializer.Deserialize<Dictionary<string, Entry>>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException("asset-versions.json deserialized to null.");
        return data;
    }

    private sealed class Entry
    {
        public string V { get; set; } = "";
        public string Sha256 { get; set; } = "";
    }
}

/// <summary>Shared repo-root lookup for test helpers.</summary>
internal static class TestRepo
{
    public static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found from test base directory.");
    }
}
