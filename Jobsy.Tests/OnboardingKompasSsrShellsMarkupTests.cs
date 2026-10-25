using Xunit;

namespace Jobsy.Tests;

public sealed class OnboardingKompasSsrShellsMarkupTests
{
    [Fact]
    public void BuildHubBody_markup_places_reiskaart_before_intro()
    {
        var path = Path.Combine(RepoRoot, "Jobsy.Web/Onboarding/OnboardingKompasSsrShells.cs");
        var src = File.ReadAllText(path);
        var hubStart = src.IndexOf("public static string BuildHubBody", StringComparison.Ordinal);
        Assert.True(hubStart >= 0);
        var hub = src[hubStart..];
        var reis = hub.IndexOf("kompas-hub-layout__reiskaart", StringComparison.Ordinal);
        var intro = hub.IndexOf("kompas__intro", StringComparison.Ordinal);
        Assert.True(reis >= 0);
        Assert.True(intro > reis);
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
