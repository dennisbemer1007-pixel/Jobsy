namespace Jobsy.Tests;

public sealed class MascotStickyBarGuardTests
{
    [Fact]
    public void Vacancy_detail_sticky_apply_is_registered_for_coach_clearance()
    {
        var root = FindRepoRoot();
        var detail = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/VacancyDetail.razor"));
        Assert.Contains("data-sticky-footer", detail, StringComparison.Ordinal);
        Assert.Contains("kb-detail__sticky-apply", detail, StringComparison.Ordinal);

        var js = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/js/app-core.js"));
        Assert.Contains("syncStickyFooters", js, StringComparison.Ordinal);
        Assert.Contains("[data-sticky-footer]", js, StringComparison.Ordinal);

        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".lobsy-coach-dock", css, StringComparison.Ordinal);
        Assert.Contains("var(--sticky-footer-h", css, StringComparison.Ordinal);
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
