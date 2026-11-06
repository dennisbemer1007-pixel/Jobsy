using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class EducationDirectionCatalogTests
{
    [Fact]
    public void SupportsDirection_false_for_basisschool_and_none()
    {
        Assert.False(EducationDirectionCatalog.SupportsDirection(null));
        Assert.False(EducationDirectionCatalog.SupportsDirection(""));
        Assert.False(EducationDirectionCatalog.SupportsDirection(EducationLevelLabels.None));
        Assert.False(EducationDirectionCatalog.SupportsDirection("Basisschool"));
    }

    [Fact]
    public void SupportsDirection_true_for_secondary_and_up()
    {
        Assert.True(EducationDirectionCatalog.SupportsDirection("HAVO"));
        Assert.True(EducationDirectionCatalog.SupportsDirection("MBO 2"));
    }

    [Fact]
    public void Filter_matches_substring_case_insensitive()
    {
        var hits = EducationDirectionCatalog.Filter("tech");
        Assert.Contains(hits, h => h.Contains("Techniek", StringComparison.OrdinalIgnoreCase));
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void Education_step_razor_uses_typeahead_not_tiles()
    {
        var src = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Candidate/Onboarding/EducationStep.razor"));
        Assert.Contains("EducationDirectionInput", src, StringComparison.Ordinal);
        Assert.DoesNotContain("ob-wizard__direction-chips", src, StringComparison.Ordinal);
        Assert.DoesNotContain("EducationDirectionField", src, StringComparison.Ordinal);
    }
}
