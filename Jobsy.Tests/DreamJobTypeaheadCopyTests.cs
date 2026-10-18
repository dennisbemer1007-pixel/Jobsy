using Jobsy.Web.Components.Candidate.Onboarding;

namespace Jobsy.Tests;

public class DreamJobTypeaheadCopyTests
{
    [Fact]
    public void DisplayTitle_title_cases_dutch()
        => Assert.Equal("Dierenartsassistent", DreamJobTypeaheadCopy.DisplayTitle("dierenartsassistent"));

    [Fact]
    public void DisplayDescription_clamps_with_ellipsis()
    {
        var longText = new string('a', 80);
        var result = DreamJobTypeaheadCopy.DisplayDescription(longText, 60);
        Assert.NotNull(result);
        Assert.True(result!.Length <= 60);
        Assert.EndsWith("…", result);
    }
}
