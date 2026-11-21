using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class CvProposalFieldDisplayTests
{
    [Fact]
    public void ListDisplayLines_splits_employers_and_single_start_year()
    {
        var profile = new CvExtractedProfile(
            Employers:
            [
                new CandidateEmployerHistoryDto(
                    "Huisartsenpraktijk De Linde",
                    Role: "Verpleegkundige",
                    StartMonth: "2019-01",
                    EndMonth: "2024-12"),
                new CandidateEmployerHistoryDto(
                    "Dierenasiel Scheveningen",
                    Role: "Vrijwilliger",
                    StartMonth: "2022-01",
                    EndMonth: null)
            ]);

        var lines = CvProposalFieldDisplay.ListDisplayLines(CvExtractionFieldKeys.Employers, profile);

        Assert.Equal(2, lines.Count);
        Assert.All(lines, l => Assert.Equal(CvExtractionFieldKeys.Employers, l.FieldKey));
        Assert.Contains("2019–2024", lines[0].Label, StringComparison.Ordinal);
        Assert.Contains("2022", lines[1].Label, StringComparison.Ordinal);
        Assert.DoesNotContain("2022 -", lines[1].Label, StringComparison.Ordinal);
    }
}
