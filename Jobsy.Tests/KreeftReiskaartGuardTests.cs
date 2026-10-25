namespace Jobsy.Tests;

public sealed class KreeftReiskaartGuardTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(10)]
    [InlineData(16)]
    public void Catalog_covers_journey_screens(int screen)
    {
        var state = Jobsy.Web.Onboarding.KreeftReiskaartCatalog.ForScreen(screen);
        Assert.Equal(screen, state.ScreenNumber);
        Assert.InRange(state.ZoneIndex, 0, 4);
        Assert.InRange(state.Shed, 0, 10);
    }

    [Theory]
    [InlineData(1, 4)]
    [InlineData(8, 11)]
    [InlineData(9, 13)]
    [InlineData(10, 14)]
    public void Onboarding_steps_map_to_mockup_screens(int step, int screen)
    {
        Assert.Equal(screen, Jobsy.Web.Onboarding.KreeftReiskaartCatalog.ScreenForOnboardingStep(step));
    }

    [Fact]
    public void Wired_surfaces_include_reiskaart_markup()
    {
        var root = FindRepoRoot();
        var files = new[]
        {
            Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Candidate", "OnboardingStartSsr.razor"),
            Path.Combine(root, "Jobsy.Web", "Onboarding", "OnboardingStartWelcomeSsrShell.cs"),
            Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Login.razor"),
            Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Landing.razor"),
            Path.Combine(root, "Jobsy.Web", "Passport", "PassportSsrShells.cs"),
            Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Candidate", "OnboardingKompasReadySsr.razor"),
        };

        foreach (var path in files)
        {
            var text = File.ReadAllText(path);
            Assert.Contains("KreeftReiskaart", text, StringComparison.Ordinal);
        }

        var htmlBuilder = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Media", "KreeftReiskaartHtml.cs"));
        Assert.Contains("kreeft-reiskaart", htmlBuilder, StringComparison.Ordinal);
        Assert.Contains("data-testid=", htmlBuilder, StringComparison.Ordinal);
        Assert.Contains("class=\\\"route\\\"", htmlBuilder, StringComparison.Ordinal);
        Assert.Contains("class=\\\"lagen", htmlBuilder, StringComparison.Ordinal);
        Assert.Contains("kreeft-reiskaart.css", File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Candidate", "OnboardingStartSsr.razor")), StringComparison.Ordinal);
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
