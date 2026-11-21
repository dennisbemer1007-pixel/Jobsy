using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class CandidateEmployerYearRangeFormatTests
{
    [Fact]
    public void Start_only_without_end_or_current_shows_single_year()
    {
        Assert.Equal("2022", CandidateEmployerYearRangeFormat.Format("2022-01", null, false, "heden"));
    }

    [Fact]
    public void Range_shows_start_and_end()
    {
        Assert.Equal(
            "2019 - 2024",
            CandidateEmployerYearRangeFormat.Format("2019-01", "2024-12", false, "heden"));
    }

    [Fact]
    public void Current_shows_present_label()
    {
        Assert.Equal(
            "2019 - nu",
            CandidateEmployerYearRangeFormat.Format("2019-01", null, true, "nu"));
    }
}
