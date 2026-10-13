using Jobsy.Web.Models;

namespace Jobsy.Web.KandidaatBanen;

/// <summary>DNA dimension rows for Match "Waarom jij past" (08).</summary>
public sealed record MatchDnaRow(
    string Kind,
    string LabelKey,
    int? Percent,
    string WhyKey,
    string TestHref);

public static class MatchDnaRows
{
    private static readonly (string Kind, string LabelKey, string WhyKey, string TestHref)[] Spec =
    [
        ("culture", "Kb.Dna.Culture", "Kb.Why.culture", KbRoutes.CultureTest),
        ("values", "Kb.Dna.Values", "Kb.Why.values", KbRoutes.ValuesTest),
        ("competency", "Kb.Dna.Competencies", "Kb.Why.competency", "/candidate/tests"),
        ("interest", "Kb.Dna.Interests", "Kb.Why.interest", "/candidate/tests")
    ];

    /// <summary>
    /// Desktop: all 4 dimensions (missing → "Nog niet gedaan" + link).
    /// Mobile: only dimensions with data, max <paramref name="maxWithData"/>.
    /// </summary>
    public static IReadOnlyList<MatchDnaRow> Build(
        CandidateFitDimensionsModel? dims,
        IReadOnlyList<string>? whyKinds,
        bool includeMissing,
        int maxWithData = 3)
    {
        var why = new HashSet<string>(whyKinds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        var rows = new List<MatchDnaRow>(4);

        foreach (var (kind, labelKey, whyKey, href) in Spec)
        {
            var percent = PercentFor(dims, kind);
            if (percent is null && !includeMissing)
            {
                continue;
            }

            // Prefer a why-kind that maps to this dimension; otherwise the dimension's default Kb.Why.*.
            var reasonKey = why.Contains(kind) || percent is not null ? whyKey : whyKey;
            rows.Add(new MatchDnaRow(kind, labelKey, percent, reasonKey, href));
        }

        if (!includeMissing && rows.Count > maxWithData)
        {
            return rows.Take(maxWithData).ToList();
        }

        return rows;
    }

    private static int? PercentFor(CandidateFitDimensionsModel? dims, string kind) => kind switch
    {
        "culture" => dims?.Culture,
        "values" => dims?.Values,
        "competency" => dims?.Competencies,
        "interest" => dims?.Interests,
        _ => null
    };
}
