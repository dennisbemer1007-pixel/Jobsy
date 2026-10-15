namespace Jobsy.Tests;

/// <summary>
/// Work status and custom availability stay optional; they must not gate Volgende / CanNext.
/// </summary>
public class OnboardingNextOptionalFieldsTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void Wizard_CanNext_step1_does_not_require_work_status()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Pages/Candidate/OnboardingWizard.razor"));
        var canNext = ExtractSwitchArm(src, "private bool CanNext => _step switch", "};");
        var step1 = ExtractCaseBlock(canNext, "1 =>");
        Assert.DoesNotContain("WorkStatus", step1, StringComparison.Ordinal);
    }

    [Fact]
    public void Discovery_CanNext_step1_does_not_require_work_status()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Pages/Candidate/DiscoveryJourney.razor"));
        var canNext = ExtractSwitchArm(src, "_countedStep switch", "};");
        var step1 = ExtractCaseBlock(canNext, "1 =>");
        Assert.DoesNotContain("WorkStatus", step1, StringComparison.Ordinal);
    }

    [Fact]
    public void Work_status_ui_is_marked_optional_in_copy()
    {
        var strings = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Localization/UiStringsOnboardingV2.cs"));
        Assert.Contains("WorkStatus.Hint", strings, StringComparison.Ordinal);
        Assert.Contains("Optioneel", strings, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Availability_custom_mode_is_separate_from_preset_summary()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Candidate/Onboarding/AvailabilityStep.razor"));
        Assert.Contains("HasSelection && !CustomMode", src, StringComparison.Ordinal);
        Assert.Contains("ShowMatrix => CustomMode || Draft.FineTune", src, StringComparison.Ordinal);
    }

    private static string ExtractSwitchArm(string src, string startMarker, string endMarker)
    {
        var start = src.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, startMarker);
        var end = src.IndexOf(endMarker, start, StringComparison.Ordinal);
        Assert.True(end > start, endMarker);
        return src[start..end];
    }

    private static string ExtractCaseBlock(string switchBody, string caseMarker)
    {
        var start = switchBody.IndexOf(caseMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, caseMarker);
        var nextCase = switchBody.IndexOf("\n            ", start + caseMarker.Length, StringComparison.Ordinal);
        if (nextCase < 0)
        {
            nextCase = switchBody.Length;
        }

        return switchBody[start..nextCase];
    }
}
