using Jobsy.Core.Rules;

namespace Jobsy.Core.Passport;

/// <summary>
/// Turns completed-test scores into short word labels.
/// The percent is used only to pick an order. It is never written into the label.
/// </summary>
public static class PassportDnaWords
{
    public static IReadOnlyList<string> Competency(CompetencyScores? scores, string language, int take = 2)
        => Labels(CompetencyItems(scores, language, take));

    public static IReadOnlyList<(string Code, string Label)> CompetencyItems(
        CompetencyScores? scores,
        string language,
        int take = 2)
    {
        if (scores is not { IsComplete: true })
        {
            return [];
        }

        return CompetencyTestCatalog.CategoryCodes
            .Select(code => (Code: code, Score: scores.TryGet(code)))
            .Where(x => x.Score is not null)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Code, StringComparer.Ordinal)
            .Take(Math.Max(1, take))
            .Select(x => (x.Code, Label: DimensionLabels.For(x.Code, language)))
            .Where(x => !string.IsNullOrWhiteSpace(x.Label))
            .DistinctBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<string> Career(RiasecScores? scores, string language, int take = 2)
        => Labels(CareerItems(scores, language, take));

    public static IReadOnlyList<(string Code, string Label)> CareerItems(
        RiasecScores? scores,
        string language,
        int take = 2)
    {
        if (scores is not { IsComplete: true })
        {
            return [];
        }

        var ranked = RiasecRanking.Rank(
            scores.Realistic, scores.Investigative, scores.Artistic,
            scores.Social, scores.Enterprising, scores.Conventional);
        return ranked
            .Take(Math.Max(1, take))
            .Select(x => (x.Code, Label: DimensionLabels.For(x.Code, language)))
            .Where(x => !string.IsNullOrWhiteSpace(x.Label))
            .DistinctBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<string> Culture(CulturePersonalityScores? scores, string language, int take = 1)
        => Labels(CultureItems(scores, language, take));

    public static IReadOnlyList<(string Code, string Label)> CultureItems(
        CulturePersonalityScores? scores,
        string language,
        int take = 1)
    {
        if (scores is null)
        {
            return [];
        }

        var pairs = OnboardingWizardCatalog.CultureDimensionCodes
            .Select(code => (code, Score: CultureScore(scores, code)))
            .Where(x => x.Score is not null)
            .Select(x => (x.code, (int?)x.Score))
            .ToList();
        if (pairs.Count != OnboardingWizardCatalog.CultureDimensionCodes.Length)
        {
            return [];
        }

        return DimensionRanking.Rank(pairs, DimensionRanking.CultureTieBreak)
            .Take(Math.Max(1, take))
            .Select(x => (x.Code, Label: DimensionLabels.For(x.Code, language)))
            .Where(x => !string.IsNullOrWhiteSpace(x.Label))
            .DistinctBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<string> Values(SchwartzValuesScores? scores, string language, int take = 1)
        => Labels(ValueItems(scores, language, take));

    public static IReadOnlyList<(string Code, string Label)> ValueItems(
        SchwartzValuesScores? scores,
        string language,
        int take = 1)
    {
        if (scores is not { IsComplete: true })
        {
            return [];
        }

        return DimensionRanking.Rank(
                SchwartzValuesCatalog.CategoryCodes.Select(code => (code, (int?)scores.Get(code))),
                DimensionRanking.ValueTieBreak)
            .Take(Math.Max(1, take))
            .Select(x => (x.Code, Label: DimensionLabels.For(x.Code, language)))
            .Where(x => !string.IsNullOrWhiteSpace(x.Label))
            .DistinctBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> Labels(IReadOnlyList<(string Code, string Label)> items)
        => items.Select(item => item.Label).ToArray();

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
