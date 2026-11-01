using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.Matching;

namespace Jobsy.Tests.Scholen;

public class PupilVakkenMatchingTests
{
    [Theory]
    [InlineData(5, 4, 0.90)]
    [InlineData(4, 3, 0.65)]
    [InlineData(3, 5, 0.70)]
    [InlineData(2, 3, 0.35)]
    public void SubjectScore_uses_sixty_forty_leuk_goed(int leuk, int goed, double expected)
    {
        Assert.Equal(expected, PupilVakScores.SubjectScore(leuk, goed), 2);
    }

    [Fact]
    public void Proposal_example_elektricien_and_verpleegkundige()
    {
        var riasec = new Jobsy.Core.Rules.RiasecScores(80, 50, 40, 50, 30, 30);
        var vak = new List<PupilVakScoreEntry>
        {
            new("natuur-techniek", 5, 4),
            new("rekenen", 4, 3),
            new("tekenen", 4, 4),
            new("gym", 3, 5),
            new("taal", 2, 3),
        };

        var likes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "techniek" };
        var elek = Score("elektricien", riasec, vak, likes);
        var verpleeg = Score("verpleegkundige", riasec, vak, likes);

        Assert.InRange(elek, 0.86, 0.90);
        Assert.InRange(verpleeg, 0.38, 0.42);
    }

    [Fact]
    public void DreamJobEsco_catalog_has_no_unmatched_keys()
    {
        Assert.Empty(PupilDreamJobEscoCatalog.UnmatchedJobKeys);
    }

    [Fact]
    public void Max_two_per_beroepsgroep_in_top_five()
    {
        var matches = new[]
        {
            new PupilG78JobMatch("elektricien", 0.92),
            new PupilG78JobMatch("automonteur", 0.91),
            new PupilG78JobMatch("schilder", 0.90),
            new PupilG78JobMatch("timmerman", 0.89),
            new PupilG78JobMatch("leraar", 0.88),
            new PupilG78JobMatch("verpleegkundige", 0.87),
        };

        var picked = PupilCareerMatchSelection.SelectTop(
            matches,
            m => m.MatchScore,
            m => PupilG78CareerScoring.PrimaryBeroepssoortKey(m.JobKey) ?? "",
            0.70,
            5,
            2);

        Assert.True(picked.Count <= 5);
        var groups = picked
            .GroupBy(m => PupilG78CareerScoring.PrimaryBeroepssoortKey(m.JobKey))
            .Select(g => g.Count());
        Assert.All(groups, c => Assert.True(c <= 2));
    }

    private static double Score(
        string jobKey,
        Jobsy.Core.Rules.RiasecScores riasec,
        IReadOnlyList<PupilVakScoreEntry> vak,
        IReadOnlySet<string> likes)
    {
        var riasecFit = 0.80; // R for elektricien; simplified for verpleeg uses S from letter pool
        foreach (var (letter, jobs) in PupilRiasecJobIdeas.Groep78LetterPoolForScoring)
        {
            if (!jobs.Any(j => string.Equals(j, jobKey, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var code = letter switch
            {
                'R' => Jobsy.Core.Rules.CareerTestCatalog.Realistic,
                'I' => Jobsy.Core.Rules.CareerTestCatalog.Investigative,
                'A' => Jobsy.Core.Rules.CareerTestCatalog.Artistic,
                'S' => Jobsy.Core.Rules.CareerTestCatalog.Social,
                'E' => Jobsy.Core.Rules.CareerTestCatalog.Enterprising,
                'C' => Jobsy.Core.Rules.CareerTestCatalog.Conventional,
                _ => Jobsy.Core.Rules.CareerTestCatalog.Social
            };
            riasecFit = riasec.Get(code) / 100.0;
        }

        var subjectFit = PupilSubjectOccupationFit.FitForDreamJob(jobKey, vak);
        var chipFit = likes.Contains("techniek")
                      && PupilRiasecJobIdeas.LikeJobsForScoring.TryGetValue("techniek", out var tJobs)
                      && tJobs.Any(j => string.Equals(j, jobKey, StringComparison.OrdinalIgnoreCase))
            ? 1.0
            : 0.15;

        return PupilCareerMatchWeights.Riasec * riasecFit
               + PupilCareerMatchWeights.Vakken * subjectFit
               + PupilCareerMatchWeights.Overige * chipFit;
    }
}
