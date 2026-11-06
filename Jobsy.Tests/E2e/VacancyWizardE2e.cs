using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>Helpers for vacancy create wizard Playwright runs.</summary>
public static class VacancyWizardE2e
{
    public const string EmployerEmail = "ondernemer@jobsy.local";
    public const string EmployerPassword = "Jobsy123!";

    public static string ArtifactsRoot =>
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JOBSY_E2E_ARTIFACTS_ROOT"))
            ? "/cursor/stores/self/vacature-wizard/e2e"
            : Environment.GetEnvironmentVariable("JOBSY_E2E_ARTIFACTS_ROOT")!;

    public static bool ShouldRun =>
        string.Equals(
            Environment.GetEnvironmentVariable("JOBSY_E2E_VACANCY_WIZARD"),
            "1",
            StringComparison.Ordinal);

    public static async Task ShotAsync(IPage page, string viewportTag, string stepSlug)
    {
        var dir = Path.Combine(ArtifactsRoot, viewportTag);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{stepSlug}.png");
        await page.ScreenshotAsync(new() { Path = path, FullPage = false });
    }
}
