namespace Jobsy.Tests.Scholen;

public sealed class LeerlingJsClassListSafetyTests
{
    [Fact]
    public void Pupil_journey_scripts_use_dom_safe_class_helpers()
    {
        var root = RepoRoot();
        var journey = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/js/leerling-journey-ssr.js"));
        Assert.Contains("function clToggle", journey, StringComparison.Ordinal);
        Assert.DoesNotContain(".classList.", journey, StringComparison.Ordinal);

        var readAloud = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/js/read-aloud.js"));
        Assert.Contains("jobsyDomSafe", readAloud, StringComparison.Ordinal);
    }

    [Fact]
    public void School_portal_frame_includes_csp_safe_scripts()
    {
        var root = RepoRoot();
        var frame = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/ScholenPortalSsrFrame.cs"));
        Assert.Contains("scholen-portal-ssr.js", frame, StringComparison.Ordinal);
        Assert.Contains("jobsy-dom-safe.js", frame, StringComparison.Ordinal);
    }

    private static string RepoRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
