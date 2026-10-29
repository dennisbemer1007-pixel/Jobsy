namespace Jobsy.Tests;

public sealed class OnboardingKreeftStripGuardTests
{
    [Fact]
    public void Onboarding_panorama_points_at_kreeft_v4_webp_strips()
    {
        var root = FindRepoRoot();
        var panorama = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Onboarding", "OnboardingRustigPanoramaAssets.cs"));
        Assert.Contains("OnboardingKreeftStripAssets", panorama, StringComparison.Ordinal);
        Assert.DoesNotContain("rustig-panorama/start-desktop.jpg", panorama, StringComparison.Ordinal);

        var hero = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Candidate", "Onboarding", "OnboardingRustigHero.razor"));
        Assert.Contains("height=\"225\"", hero, StringComparison.Ordinal);
        Assert.Contains("height=\"105\"", hero, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Repo root not found.");
    }
}
