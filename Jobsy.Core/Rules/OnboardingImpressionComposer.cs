namespace Jobsy.Core.Rules;

public sealed record OnboardingImpressionCore(
    IReadOnlyList<OnboardingImpressionCoreItem> Strengths,
    IReadOnlyList<OnboardingImpressionCoreItem> RiasecTop,
    OnboardingImpressionCoreItem? CultureHighlight,
    OnboardingImpressionCoreItem? TopValue);

public sealed record OnboardingImpressionCoreItem(string Code, string Label, string Sentence, int? Percent);

public static class OnboardingImpressionComposer
{
    public static OnboardingImpressionCore Compose(
        CompetencyScores? competency,
        RiasecScores? career,
        CulturePersonalityScores? culture,
        SchwartzValuesScores? values)
    {
        var strengths = TopCompetencyItems(competency, 2);
        var riasecItems = career is { } rs
            ? RiasecRanking.Rank(
                    rs.Realistic, rs.Investigative, rs.Artistic,
                    rs.Social, rs.Enterprising, rs.Conventional)
                .Take(2)
                .Select(x => new OnboardingImpressionCoreItem(
                    x.Code,
                    DimensionLabels.For(x.Code),
                    OnboardingImpressionLibrary.RiasecSentence(x.Code),
                    rs.Get(x.Code)))
                .ToList()
            : [];

        OnboardingImpressionCoreItem? cultureHighlight = null;
        if (culture is { } cs)
        {
            var ranked = DimensionRanking.Rank(
                OnboardingWizardCatalog.CultureDimensionCodes.Select(code => (code, (int?)cs.Get(code))),
                DimensionRanking.CultureTieBreak);
            var best = ranked[0];
            cultureHighlight = new OnboardingImpressionCoreItem(
                best.Code,
                DimensionLabels.For(best.Code),
                OnboardingImpressionLibrary.CultureSentence(best.Code),
                best.Score);
        }

        OnboardingImpressionCoreItem? topValue = null;
        if (values is { } vs)
        {
            var ranked = DimensionRanking.Rank(
                SchwartzValuesCatalog.CategoryCodes.Select(code => (code, (int?)vs.Get(code))),
                DimensionRanking.ValueTieBreak);
            var best = ranked[0];
            topValue = new OnboardingImpressionCoreItem(
                best.Code,
                DimensionLabels.For(best.Code),
                OnboardingImpressionLibrary.ValueSentence(best.Code),
                best.Score);
        }

        return new OnboardingImpressionCore(strengths, riasecItems, cultureHighlight, topValue);
    }

    public static List<OnboardingImpressionCoreItem> TopCompetencyItems(CompetencyScores? scores, int take)
    {
        if (scores is null)
        {
            return [];
        }

        return CompetencyTestCatalog.CategoryCodes
            .Concat([CompetencyTestCatalog.Extraversie])
            .Select(code => (Code: code, Pct: scores.TryGet(code)))
            .Where(x => x.Pct is not null)
            .OrderByDescending(x => x.Pct)
            .ThenBy(x => x.Code, StringComparer.Ordinal)
            .Take(take)
            .Select(x => new OnboardingImpressionCoreItem(
                x.Code,
                DimensionLabels.For(x.Code),
                OnboardingImpressionLibrary.StrengthSentence(x.Code),
                x.Pct))
            .ToList();
    }
}
