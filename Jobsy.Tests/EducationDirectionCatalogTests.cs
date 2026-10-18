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

    [Fact]
    public void Education_step_razor_uses_typeahead_not_tiles()
    {
        var path = FindRepoRoot("Jobsy.Web/Components/Candidate/Onboarding/EducationStep.razor");
        var src = File.ReadAllText(path);
        Assert.Contains("EducationDirectionInput", src, StringComparison.Ordinal);
        Assert.DoesNotContain("ob-wizard__direction-chips", src, StringComparison.Ordinal);
        Assert.DoesNotContain("EducationDirectionField", src, StringComparison.Ordinal);
    }

    private static string FindRepoRoot(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var path = Path.Combine(dir.FullName, relative);
            if (File.Exists(path))
            {
                return path;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException($"Could not find {relative}.");
    }
}
