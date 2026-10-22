namespace Jobsy.Tests;

public sealed class OnboardingStartSsrPageTests
{
    [Fact]
    public void OnboardingStartSsr_is_static_ssr_without_blazor_circuit()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Jobsy.Web",
            "Components", "Pages", "Candidate", "OnboardingStartSsr.razor");
        path = Path.GetFullPath(path);
        var source = File.ReadAllText(path);
        Assert.Contains("@page \"/candidate/start\"", source, StringComparison.Ordinal);
        Assert.Contains("[ExcludeFromInteractiveRouting]", source, StringComparison.Ordinal);
        Assert.Contains("[NoBlazorRuntime]", source, StringComparison.Ordinal);
        Assert.Contains("method=\"post\"", source, StringComparison.Ordinal);
        Assert.Contains("data-enhance-nav=\"false\"", source, StringComparison.Ordinal);
        Assert.Contains("data-ob-ssr-form", source, StringComparison.Ordinal);
    }

    [Fact]
    public void OnboardingWizard_uses_internal_wizard_route()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Jobsy.Web",
            "Components", "Pages", "Candidate", "OnboardingWizard.razor");
        path = Path.GetFullPath(path);
        var source = File.ReadAllText(path);
        Assert.Contains("@page \"/candidate/start/wizard\"", source, StringComparison.Ordinal);
    }
}
