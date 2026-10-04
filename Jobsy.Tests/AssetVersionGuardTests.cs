using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Jobsy.Tests;

/// <summary>
/// Guards against shipping wwwroot changes behind a stale <c>?v=</c> while
/// <c>Cache-Control: public,max-age=31536000,immutable</c> is in effect.
///
/// After changing a versioned asset under <c>Jobsy.Web/wwwroot</c>, bump its
/// <c>?v=</c> in App.razor (or the loader that references it), then refresh the
/// manifest with one line from the repo root:
/// <c>python3 Jobsy.Tests/update-asset-versions.py</c>
/// </summary>
public class AssetVersionGuardTests
{
    private static readonly Regex HtmlAssetRef = new(
        """(?i)(?:src|href)\s*=\s*["'](?!https?:|//|data:)(?<url>[^"']+\?v=[^"']+)["']""",
        RegexOptions.Compiled);

    private static readonly Regex JsAssetRef = new(
        """["'](?<url>/(?:js|css|service-worker)[^"']+\?v=[^"']+)["']""",
        RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void Versioned_wwwroot_assets_match_checked_in_manifest()
    {
        var root = FindRepoRoot();
        var wwwroot = Path.Combine(root, "Jobsy.Web", "wwwroot");
        var manifestPath = Path.Combine(root, "Jobsy.Tests", "asset-versions.json");
        Assert.True(File.Exists(manifestPath), "Missing Jobsy.Tests/asset-versions.json");

        var manifest = JsonSerializer.Deserialize<Dictionary<string, AssetVersionEntry>>(
            File.ReadAllText(manifestPath), JsonOptions)
            ?? throw new InvalidOperationException("asset-versions.json deserialized to null.");

        var refs = CollectVersionedLocalRefs(root);
        Assert.NotEmpty(refs);

        foreach (var (relativePath, version, sourceFile) in refs)
        {
            if (!manifest.TryGetValue(relativePath, out var entry))
            {
                Assert.Fail(
                    $"{relativePath} is versioned in {sourceFile} (?v={version}) but missing from asset-versions.json — add it and keep the SHA in sync.");
            }

            if (!string.Equals(entry.V, version, StringComparison.Ordinal))
            {
                Assert.Fail(
                    $"{relativePath}: host uses ?v={version} ({sourceFile}) but asset-versions.json has v={entry.V}. Align both.");
            }

            var physical = Path.Combine(wwwroot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(physical), $"wwwroot/{relativePath} referenced from {sourceFile} does not exist.");

            var sha = Sha256Hex(physical);
            if (!string.Equals(sha, entry.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                Assert.Fail(
                    $"wwwroot/{relativePath} changed – bump its ?v in App.razor and update asset-versions.json");
            }
        }

        // Every manifest entry must still match the file on disk (catches loader-only assets).
        foreach (var (relativePath, entry) in manifest)
        {
            var physical = Path.Combine(wwwroot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(physical), $"asset-versions.json lists missing file wwwroot/{relativePath}");
            var sha = Sha256Hex(physical);
            if (!string.Equals(sha, entry.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                Assert.Fail(
                    $"wwwroot/{relativePath} changed – bump its ?v in App.razor and update asset-versions.json");
            }
        }
    }

    private static readonly Regex SortableVersion = new(@"^\d{8}-\d{2}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    [Fact]
    public void Shell_and_feature_css_versions_are_sortable_yyyymmdd_nn()
    {
        var root = FindRepoRoot();
        var manifestPath = Path.Combine(root, "Jobsy.Tests", "asset-versions.json");
        var manifest = JsonSerializer.Deserialize<Dictionary<string, AssetVersionEntry>>(
            File.ReadAllText(manifestPath), JsonOptions)
            ?? throw new InvalidOperationException("asset-versions.json deserialized to null.");

        foreach (var (relativePath, entry) in manifest)
        {
            var required = relativePath is "css/app.min.css" or "js/app-core.js" or "js/lobsyPush.js" or "js/read-aloud.js"
                || (relativePath.StartsWith("css/features/", StringComparison.Ordinal)
                    && relativePath.EndsWith(".css", StringComparison.Ordinal));
            if (!required)
            {
                continue;
            }

            Assert.True(
                SortableVersion.IsMatch(entry.V),
                $"{relativePath} uses ?v={entry.V}; expected yyyymmdd-NN.");
        }

        foreach (var name in new[] { "service-worker.js", "service-worker.published.js" })
        {
            var js = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", name));
            var match = Regex.Match(js, @"CACHE_VERSION = ""lobsy-shell(?:-published)?-v(?<tag>\d{8}-\d{2})""");
            Assert.True(match.Success, $"{name} cache name is not yyyymmdd-NN.");
            foreach (Match precache in Regex.Matches(js, @"\?v=(?<tag>[^""'\s]+)"))
            {
                Assert.True(
                    SortableVersion.IsMatch(precache.Groups["tag"].Value),
                    $"{name} precache uses ?v={precache.Groups["tag"].Value}; expected yyyymmdd-NN.");
            }
        }
    }

    [Fact]
    public void App_razor_and_layouts_use_sortable_asset_versions()
    {
        var root = FindRepoRoot();
        var files = new List<string>
        {
            Path.Combine(root, "Jobsy.Web", "Components", "App.razor"),
        };
        var layoutDir = Path.Combine(root, "Jobsy.Web", "Components", "Layout");
        files.AddRange(Directory.EnumerateFiles(layoutDir, "*.razor", SearchOption.AllDirectories));

        var version = new Regex(@"\?v=(?<tag>[^""'\s&#]+)", RegexOptions.CultureInvariant);
        var problems = new List<string>();
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            foreach (Match match in version.Matches(text))
            {
                var start = Math.Max(0, match.Index - 80);
                var window = text[start..match.Index];
                if (window.Contains("_framework/blazor.web.js", StringComparison.Ordinal))
                {
                    continue;
                }

                var tag = match.Groups["tag"].Value;
                if (!SortableVersion.IsMatch(tag))
                {
                    problems.Add($"{Path.GetRelativePath(root, file)}: ?v={tag}");
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void Manifest_and_service_worker_lists_use_the_same_sortable_versions()
    {
        var root = FindRepoRoot();
        var wwwroot = Path.Combine(root, "Jobsy.Web", "wwwroot");
        var manifestJson = File.ReadAllText(Path.Combine(wwwroot, "manifest.webmanifest"));
        var version = new Regex(@"\?v=(?<tag>[^""'\s]+)", RegexOptions.CultureInvariant);
        var manifestTags = version.Matches(manifestJson).Select(m => m.Groups["tag"].Value).ToList();
        Assert.NotEmpty(manifestTags);
        Assert.All(manifestTags, tag => Assert.True(SortableVersion.IsMatch(tag), $"manifest ?v={tag}"));

        var iconSrc = new Regex(
            @"""src"":\s*""/(?<path>icons/[^""?]+)\?v=(?<tag>[^""]+)""",
            RegexOptions.CultureInvariant);
        var manifestIcons = iconSrc.Matches(manifestJson)
            .Select(m => (Path: m.Groups["path"].Value, Tag: m.Groups["tag"].Value))
            .ToList();
        Assert.Contains(manifestIcons, icon => icon.Path == "icons/icon-192.png");
        Assert.Contains(manifestIcons, icon => icon.Path == "icons/icon-512.png");

        var manifestPath = Path.Combine(root, "Jobsy.Tests", "asset-versions.json");
        var catalog = JsonSerializer.Deserialize<Dictionary<string, AssetVersionEntry>>(
            File.ReadAllText(manifestPath), JsonOptions)
            ?? throw new InvalidOperationException("asset-versions.json deserialized to null.");

        foreach (var name in new[] { "service-worker.js", "service-worker.published.js" })
        {
            var js = File.ReadAllText(Path.Combine(wwwroot, name));
            var precache = Regex.Matches(js, @"""/(?<path>[^""?]+)\?v=(?<tag>[^""]+)""");
            Assert.NotEmpty(precache);
            foreach (Match item in precache)
            {
                var path = item.Groups["path"].Value;
                var tag = item.Groups["tag"].Value;
                Assert.True(SortableVersion.IsMatch(tag), $"{name} precache {path}?v={tag}");
                if (catalog.TryGetValue(path, out var entry))
                {
                    Assert.Equal(entry.V, tag);
                }

                var fromManifest = manifestIcons.FirstOrDefault(icon => icon.Path == path);
                if (fromManifest.Path is not null)
                {
                    Assert.Equal(fromManifest.Tag, tag);
                }
            }
        }
    }

    [Fact]
    public void JobMap_text_font_uses_single_OpenFreeMap_Noto_faces_only()
    {
        var root = FindRepoRoot();
        foreach (var name in new[] { "jobMap.js", "jobMap.min.js" })
        {
            var path = Path.Combine(root, "Jobsy.Web", "wwwroot", "js", name);
            Assert.True(File.Exists(path), $"Missing {name}");
            var js = File.ReadAllText(path);

            foreach (Match m in Regex.Matches(js, """["']text-font["']\s*:\s*(\[[^\]]*\])"""))
            {
                var fonts = m.Groups[1].Value;
                Assert.DoesNotContain("Arial Unicode MS", fonts, StringComparison.Ordinal);
                Assert.DoesNotContain("Open Sans", fonts, StringComparison.Ordinal);
            }

            Assert.DoesNotContain("Arial Unicode MS", js, StringComparison.Ordinal);
            Assert.DoesNotContain("Open Sans", js, StringComparison.Ordinal);
        }

        var detail = Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "vacancyDetailMap.js");
        if (File.Exists(detail))
        {
            var js = File.ReadAllText(detail);
            Assert.DoesNotContain("Arial Unicode MS", js, StringComparison.Ordinal);
            Assert.DoesNotContain("Open Sans", js, StringComparison.Ordinal);
        }
    }

    private static List<(string RelativePath, string Version, string SourceFile)> CollectVersionedLocalRefs(string root)
    {
        var results = new List<(string, string, string)>();
        var hostFiles = new List<string>
        {
            Path.Combine(root, "Jobsy.Web", "Components", "App.razor"),
        };
        hostFiles.AddRange(Directory.EnumerateFiles(
            Path.Combine(root, "Jobsy.Web", "Components"),
            "*.razor",
            SearchOption.AllDirectories));
        hostFiles.Add(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "app-core.js"));
        hostFiles.Add(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "lobsyPush.js"));

        foreach (var file in hostFiles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!File.Exists(file))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            var sourceRel = Path.GetRelativePath(root, file).Replace('\\', '/');
            var isJs = file.EndsWith(".js", StringComparison.OrdinalIgnoreCase);
            var matches = isJs ? JsAssetRef.Matches(text) : HtmlAssetRef.Matches(text);
            foreach (Match match in matches)
            {
                var url = match.Groups["url"].Value.Trim();
                var q = url.IndexOf("?v=", StringComparison.Ordinal);
                if (q < 0)
                {
                    continue;
                }

                var pathPart = url[..q].TrimStart('~', '/');
                var version = url[(q + 3)..];
                if (pathPart.StartsWith("_framework/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Only track real wwwroot files (skip absolute CDN /mailto etc.).
                if (pathPart.Contains("://", StringComparison.Ordinal) || pathPart.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var physical = Path.Combine(root, "Jobsy.Web", "wwwroot", pathPart.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(physical))
                {
                    continue;
                }

                results.Add((pathPart.Replace('\\', '/'), version, sourceRel));
            }
        }

        return results;
    }

    private static string Sha256Hex(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed class AssetVersionEntry
    {
        public string V { get; set; } = "";
        public string Sha256 { get; set; } = "";
    }

    private static string FindRepoRoot()
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
