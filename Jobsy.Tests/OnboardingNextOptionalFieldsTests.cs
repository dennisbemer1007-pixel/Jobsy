namespace Jobsy.Tests;

/// <summary>
/// Work status and custom availability stay optional; they must not gate Volgende / CanNext.
/// </summary>
public class OnboardingNextOptionalFieldsTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void Wizard_step1_validation_does_not_require_work_status()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Pages/Candidate/OnboardingWizard.razor"));
        var validation = ExtractMethodBody(src, "GetValidationIssues()");
        Assert.DoesNotContain("WorkStatus", validation, StringComparison.Ordinal);
        Assert.Contains("ValidatePersonal", validation, StringComparison.Ordinal);
    }

    [Fact]
    public void Discovery_step1_validation_does_not_require_work_status()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Pages/Candidate/DiscoveryJourney.razor"));
        var validation = ExtractMethodBody(src, "GetValidationIssues()");
        Assert.DoesNotContain("WorkStatus", validation, StringComparison.Ordinal);
        Assert.Contains("ValidatePersonal", validation, StringComparison.Ordinal);
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

    private static string ExtractMethodBody(string src, string startMarker)
    {
        var start = src.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, startMarker);
        var brace = src.IndexOf('{', start);
        Assert.True(brace >= 0, "{");
        var depth = 0;
        for (var i = brace; i < src.Length; i++)
        {
            if (src[i] == '{')
            {
                depth++;
            }
            else if (src[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return src[brace..(i + 1)];
                }
            }
        }

        Assert.Fail("Unclosed method body");
        return "";
    }
}
