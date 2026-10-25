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
        Assert.Contains("kompas__eyebrow", hub, StringComparison.Ordinal);
        Assert.Contains("kompas__intro--hub-mockup", hub, StringComparison.Ordinal);
        Assert.Contains("Kompas.Ssr.Eyebrow", hub, StringComparison.Ordinal);
        Assert.Contains("Kompas.Ssr.HubTitle", hub, StringComparison.Ordinal);
        Assert.Contains("Kompas.Ssr.HubLead", hub, StringComparison.Ordinal);
        Assert.Contains("ob-rustig__kompas-tests--hub-mockup", hub, StringComparison.Ordinal);
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
