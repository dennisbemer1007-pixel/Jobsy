using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

public static class TrainingMatchRules
{
    public const int MaxResults = 3;

    public static IReadOnlyList<string> SplitCsv(string? csv)
        => string.IsNullOrWhiteSpace(csv)
            ? []
            : csv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.ToLowerInvariant())
                .Distinct()
                .ToList();

    public static string JoinCsv(IEnumerable<string>? values)
        => string.Join(',', (values ?? []).Select(v => v.Trim().ToLowerInvariant()).Where(v => v.Length > 0).Distinct());

    public static int Score(
        TrainingProviderKind kind,
        IReadOnlyList<string> offerFields,
        IReadOnlyList<string> offerKeys,
        IReadOnlyList<string> detectedFields,
        string searchBlob)
    {
        var folded = CareerOccupationKeys.Fold(searchBlob);
        var fieldHits = offerFields.Count(f => detectedFields.Contains(f, StringComparer.OrdinalIgnoreCase));
        var keyHits = offerKeys.Count(k => folded.Contains(k, StringComparison.Ordinal) || CareerOccupationKeys.Hits(folded, k));
        var regionalBoost = kind == TrainingProviderKind.RegionalPartner && fieldHits > 0 ? 40 : 0;
        var nationalFallback = kind == TrainingProviderKind.NationalAffiliate ? 8 : 0;
        return regionalBoost + fieldHits * 20 + keyHits * 12 + nationalFallback;
    }
}
