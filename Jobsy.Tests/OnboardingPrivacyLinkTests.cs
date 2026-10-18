namespace Jobsy.Tests;

public sealed class OnboardingPrivacyLinkTests
{
    [Fact]
    public void Onboarding_wizard_links_to_privacy_data_for_account_deletion()
    {
        var root = Directory.GetParent(typeof(OnboardingPrivacyLinkTests).Assembly.Location)!
            .Parent!.Parent!.Parent!.Parent!.FullName;
        var wizard = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/OnboardingWizard.razor"));
        Assert.Contains("href=\"/privacy/data\"", wizard, StringComparison.Ordinal);
        Assert.Contains("Onboarding.AccountPrivacyLink", wizard, StringComparison.Ordinal);
    }
}
