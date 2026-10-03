namespace Jobsy.Core.Rules;

/// <summary>
/// Short, rule-based lines for the candidate's own Lobsy-CV.
/// This is not the AI "Wie ben ik" story and is not attached to employer snapshots.
/// </summary>
public static class LobsyCvHighlightLines
{
    public static IReadOnlyList<string> Build(
        CompetencyScores? competency,
        RiasecScores? career,
        CulturePersonalityScores? culture,
        SchwartzValuesScores? values)
    {
        var lines = new List<string>();
        if (competency is { IsComplete: true })
        {
            var top = CompetencyTestCatalog.CategoryCodes
                .Select(code => (Code: code, Score: competency.TryGet(code)))
                .Where(x => x.Score is not null)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Code, StringComparer.Ordinal)
                .FirstOrDefault();
            if (top.Code is not null)
            {
                lines.Add(top.Code);
            }
        }

        if (career is { IsComplete: true })
        {
            var ranked = RiasecRanking.Rank(
                career.Realistic, career.Investigative, career.Artistic,
                career.Social, career.Enterprising, career.Conventional);
            if (ranked.Count > 0)
            {
                lines.Add(string.Join(" · ", ranked.Take(3).Select(x => CareerCompassBuilder.TypeLabel(x.Code))));
            }
        }

        if (culture is not null)
        {
            var pairs = OnboardingWizardCatalog.CultureDimensionCodes
                .Select(code => (code, Score: CultureScore(culture, code)))
                .Where(x => x.Score is not null)
                .Select(x => (x.code, (int?)x.Score))
                .ToList();
            if (pairs.Count == OnboardingWizardCatalog.CultureDimensionCodes.Length)
            {
                var best = DimensionRanking.Rank(pairs, DimensionRanking.CultureTieBreak);
                if (best.Count > 0)
                {
                    lines.Add(CulturePersonalityCatalog.EverydayLabel(best[0].Code));
                }
            }
        }

        if (values is { IsComplete: true })
        {
            var best = DimensionRanking.Rank(
                SchwartzValuesCatalog.CategoryCodes.Select(code => (code, (int?)values.Get(code))),
                DimensionRanking.ValueTieBreak);
            if (best.Count > 0)
            {
                lines.Add(SchwartzValuesCatalog.EverydayLabel(best[0].Code));
            }
        }

        return lines;
    }

    private static int? CultureScore(CulturePersonalityScores scores, string code) => code switch
    {
        CulturePersonalityCatalog.Autonomy => scores.Autonomy,
        CulturePersonalityCatalog.Informal => scores.Informal,
        CulturePersonalityCatalog.Collaboration => scores.Collaboration,
        CulturePersonalityCatalog.Flexibility => scores.Flexibility,
        CulturePersonalityCatalog.Innovation => scores.Innovation,
        CulturePersonalityCatalog.PeopleFirst => scores.PeopleFirst,
        _ => null
    };
}
