using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class TestProgressMotivationTests
{
    [Fact]
    public void Quick_scan_halfway_matches_deep_halfway_key()
    {
        const int total = 25;
        const int half = 12;
        Assert.Equal(
            TestProgressMotivation.Halfway,
            TestProgressMotivation.ForQuickScan(AssessmentKind.Competence, half, total, half));
    }

    [Fact]
    public void Quick_scan_block_start_returns_part_start()
    {
        Assert.Equal(
            TestProgressMotivation.PartStart,
            TestProgressMotivation.ForQuickScan(AssessmentKind.Competence, 0, 25, 5));
    }

    [Fact]
    public void Deep_alias_still_works()
    {
        Assert.Equal(
            DeepTestMotivation.Halfway,
            TestProgressMotivation.ForProgress(
                AssessmentKind.Competence,
                75,
                150,
                DeepTestMotivation.PartIndexForGlobalIndex(AssessmentKind.Competence, 75)));
    }
}
