using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Infers a vacancy's typical RIASEC profile from sector and wording for career-interest matching.
/// </summary>
public static class VacancyRiasecProfile
{
    public static IReadOnlyList<string> InferTags(Vacancy vacancy)
        => InferTags(
            WorkTypeLabels.ResolveLabels(vacancy.WorkTypes, vacancy.WorkTypeLabels),
            vacancy.Title,
            vacancy.Description);

    public static IReadOnlyList<string> InferTags(VacancyDiscoveryRecord vacancy)
        => InferTags(vacancy.WorkTypeLabelList, vacancy.Title, vacancy.Description);

    public static IReadOnlyList<string> InferTags(
        IReadOnlyList<string>? workTypes,
        string? title,
        string? description)
    {
        var tags = new List<string>();
        var types = workTypes?
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        foreach (var label in types)
        {
            foreach (var tag in ForWorkType(label))
            {
                if (!tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                {
                    tags.Add(tag);
                }
            }
        }

        var blob = $"{title} {description}";
        AddIf(blob, tags, CareerTestCatalog.Realistic, "kas", "tuin", "magazijn", "vorkhef", "technisch", "onderhoud", "bouw", "productie");
        AddIf(blob, tags, CareerTestCatalog.Investigative, "onderzoek", "analyse", "lab", "data", "kwaliteit");
        AddIf(blob, tags, CareerTestCatalog.Artistic, "creatief", "vormgeving", "design", "styling");
        AddIf(blob, tags, CareerTestCatalog.Social, "klant", "gastvrij", "zorg", "begeleiding", "team");
        AddIf(blob, tags, CareerTestCatalog.Enterprising, "verkoop", "sales", "omzet", "ondernem", "leiding");
        AddIf(blob, tags, CareerTestCatalog.Conventional, "administratie", "kassa", "order", "planning", "administratief");

        if (tags.Count == 0)
        {
            return [CareerTestCatalog.Realistic, CareerTestCatalog.Social];
        }

        return tags;
    }

    public static double Fit01(IReadOnlyList<string>? candidateTags, IReadOnlyList<string>? vacancyTags)
    {
        var cand = candidateTags?.Where(t => !string.IsNullOrWhiteSpace(t)).ToList() ?? [];
        var vac = vacancyTags?.Where(t => !string.IsNullOrWhiteSpace(t)).ToList() ?? [];
        if (cand.Count == 0 || vac.Count == 0)
        {
            return 0.5;
        }

        var vacSet = vac.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hit = cand.Count(t => vacSet.Contains(t));
        return Math.Clamp(hit / (double)Math.Max(vac.Count, 1), 0, 1);
    }

    /// <summary>
    /// Score-based fit: average of the candidate's type percentages for the vacancy's interest tags.
    /// </summary>
    public static double Fit01(RiasecScores scores, IReadOnlyList<string>? vacancyTags)
    {
        var vac = vacancyTags?.Where(t => !string.IsNullOrWhiteSpace(t)).ToList() ?? [];
        if (vac.Count == 0 || !scores.IsComplete)
        {
            return Fit01(CareerTestCatalog.DeriveRiasecTags(scores), vacancyTags);
        }

        return Math.Clamp(vac.Average(tag => scores.Get(tag) / 100.0), 0, 1);
    }

    private static IReadOnlyList<string> ForWorkType(string label) => label.Trim() switch
    {
        WorkTypeLabels.Horeca => [CareerTestCatalog.Social, CareerTestCatalog.Enterprising],
        WorkTypeLabels.Winkel => [CareerTestCatalog.Enterprising, CareerTestCatalog.Social, CareerTestCatalog.Conventional],
        WorkTypeLabels.Logistiek => [CareerTestCatalog.Realistic, CareerTestCatalog.Conventional],
        WorkTypeLabels.Tuinbouw => [CareerTestCatalog.Realistic],
        WorkTypeLabels.Zorg => [CareerTestCatalog.Social],
        WorkTypeLabels.Kantoor => [CareerTestCatalog.Conventional, CareerTestCatalog.Investigative],
        WorkTypeLabels.Bouw => [CareerTestCatalog.Realistic],
        WorkTypeLabels.Schoonmaak => [CareerTestCatalog.Realistic, CareerTestCatalog.Conventional],
        WorkTypeLabels.Productie => [CareerTestCatalog.Realistic, CareerTestCatalog.Conventional],
        _ => []
    };

    private static void AddIf(string blob, List<string> tags, string code, params string[] needles)
    {
        if (string.IsNullOrWhiteSpace(blob))
        {
            return;
        }

        if (needles.Any(n => blob.Contains(n, StringComparison.OrdinalIgnoreCase))
            && !tags.Contains(code, StringComparer.OrdinalIgnoreCase))
        {
            tags.Add(code);
        }
    }
}
