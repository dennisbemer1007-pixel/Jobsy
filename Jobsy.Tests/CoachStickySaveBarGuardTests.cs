namespace Jobsy.Tests;

public sealed class CoachStickySaveBarGuardTests
{
    [Fact]
    public void Sticky_profile_save_bar_raises_coach_above_nav_and_measured_footer()
    {
        var root = FindRepoRoot();
        var variantB = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/lobsy-coach-variant-b.css"));
        Assert.Contains(
            ":has(.profile-page--candidate .profile-save-bar) .lobsy-coach-dock--variant-b",
            variantB,
            StringComparison.Ordinal);

        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(
            ".app-shell.has-bottom-nav:has(.profile-page--candidate .profile-save-bar) .lobsy-coach-dock",
            css,
            StringComparison.Ordinal);
        Assert.Contains("max(var(--sticky-footer-h, 0px), var(--profile-save-bar-h, 4.25rem))", css, StringComparison.Ordinal);

        var ssrJs = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/js/lobsy-coach-ssr.js"));
        Assert.Contains("syncStickyFooterHeights", ssrJs, StringComparison.Ordinal);
        Assert.Contains("position !== \"fixed\"", ssrJs, StringComparison.Ordinal);

        var coreJs = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/js/app-core.js"));
        Assert.Contains("position !== \"fixed\"", coreJs, StringComparison.Ordinal);
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

            dir = Path.GetDirectoryName(dir)!;
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
