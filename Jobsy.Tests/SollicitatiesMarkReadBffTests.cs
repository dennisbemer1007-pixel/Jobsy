using System.IO;
using System.Linq;

namespace Jobsy.Tests;

public sealed class SollicitatiesMarkReadBffTests
{
    [Fact]
    public void Wwwroot_js_never_posts_legacy_after_match_messages_read()
    {
        var root = FindRepoRoot();
        var jsRoot = Path.Combine(root, "Jobsy.Web/wwwroot/js");
        var offenders = Directory
            .EnumerateFiles(jsRoot, "*.js", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("after-match/messages/read", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root, path))
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "Legacy mark-read URL still in: " + string.Join(", ", offenders));
    }

    [Fact]
    public void Sollicitaties_ssr_loads_mark_read_flush_before_berichten_with_bumped_cache()
    {
        var root = FindRepoRoot();
        var razor = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateApplicationsSsr.razor"));
        Assert.Contains("sollicitaties-mark-read-flush.js?v=20261025-mark-read-bff2", razor, StringComparison.Ordinal);
        Assert.Contains("sollicitaties-berichten.js?v=20261025-mark-read-bff2", razor, StringComparison.Ordinal);

        var flushIdx = razor.IndexOf("sollicitaties-mark-read-flush.js", StringComparison.Ordinal);
        var berichtenIdx = razor.IndexOf("sollicitaties-berichten.js", StringComparison.Ordinal);
        Assert.True(flushIdx >= 0 && berichtenIdx > flushIdx);
    }

    [Fact]
    public void Web_host_maps_legacy_mark_read_path_to_same_bff_handler()
    {
        var root = FindRepoRoot();
        var source = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/SollicitatiesClientEndpoints.cs"));
        Assert.Contains("/api/sollicitaties-client/berichten/mark-read", source, StringComparison.Ordinal);
        Assert.Contains("/api/me/applications/{applicationId:guid}/after-match/messages/read", source, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "Jobsy.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? "";
        }

        throw new InvalidOperationException("Could not find repo root.");
    }
}
