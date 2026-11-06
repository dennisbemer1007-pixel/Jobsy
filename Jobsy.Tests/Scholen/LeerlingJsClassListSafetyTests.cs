using System.Text.RegularExpressions;

namespace Jobsy.Tests.Scholen;

public sealed class LeerlingJsClassListSafetyTests
{
    private static readonly string[] PupilSsrScripts =
    [
        "Jobsy.Web/wwwroot/js/jobsy-dom-safe.js",
        "Jobsy.Web/wwwroot/js/read-aloud.js",
        "Jobsy.Web/wwwroot/js/leerling-journey-ssr.js",
        "Jobsy.Web/wwwroot/js/leerling-login.js",
    ];

    private static readonly string[] SchoolSsrScripts =
    [
        "Jobsy.Web/wwwroot/js/jobsy-dom-safe.js",
        "Jobsy.Web/wwwroot/js/scholen-portal-ssr.js",
    ];

    [Fact]
    public void Pupil_journey_scripts_use_dom_safe_class_helpers()
    {
        var root = RepoRoot();
        var journey = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/js/leerling-journey-ssr.js"));
        Assert.Contains("function clToggle", journey, StringComparison.Ordinal);
        Assert.DoesNotContain(".classList.", journey, StringComparison.Ordinal);

        var readAloud = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/js/read-aloud.js"));
        Assert.Contains("jobsyDomSafe", readAloud, StringComparison.Ordinal);
        Assert.DoesNotContain("btn.classList.", readAloud, StringComparison.Ordinal);
    }

    [Fact]
    public void Pupil_and_school_ssr_script_bundles_avoid_unsafe_classList_chains()
    {
        var root = RepoRoot();
        foreach (var rel in PupilSsrScripts.Concat(SchoolSsrScripts).Distinct(StringComparer.Ordinal))
        {
            if (rel.EndsWith("jobsy-dom-safe.js", StringComparison.Ordinal))
            {
                continue;
            }

            var src = File.ReadAllText(Path.Combine(root, rel));
            AssertNoUnsafeClassListAccess(rel, src);
        }
    }

    [Fact]
    public void School_portal_frame_includes_csp_safe_scripts()
    {
        var root = RepoRoot();
        var frame = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/ScholenPortalSsrFrame.cs"));
        Assert.Contains("scholen-portal-ssr.js", frame, StringComparison.Ordinal);
        Assert.Contains("jobsy-dom-safe.js", frame, StringComparison.Ordinal);

        var pupilFrame = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/PupilLeerlingSsrFrame.cs"));
        Assert.Contains("jobsy-dom-safe.js", pupilFrame, StringComparison.Ordinal);
        Assert.Contains("read-aloud.js", pupilFrame, StringComparison.Ordinal);
        Assert.Contains("leerling-journey-ssr.js", pupilFrame, StringComparison.Ordinal);
    }

    private static void AssertNoUnsafeClassListAccess(string rel, string src)
    {
        foreach (Match match in Regex.Matches(src, @"[^\s].*?\.classList\."))
        {
            var line = LineContaining(src, match.Index);
            if (line.Contains("jobsyDomSafe", StringComparison.Ordinal)
                || line.Contains("function classList", StringComparison.Ordinal)
                || line.Contains("return el && el.classList", StringComparison.Ordinal))
            {
                continue;
            }

            Assert.Fail($"Unsafe .classList. in {rel}: {line.Trim()}");
        }
    }

    private static string LineContaining(string src, int index)
    {
        var start = src.LastIndexOf('\n', Math.Min(index, src.Length - 1));
        if (start < 0)
        {
            start = 0;
        }
        else
        {
            start += 1;
        }

        var end = src.IndexOf('\n', index);
        if (end < 0)
        {
            end = src.Length;
        }

        return src[start..end];
    }

    private static string RepoRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
