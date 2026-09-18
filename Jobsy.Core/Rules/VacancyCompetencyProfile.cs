using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Infers a vacancy's typical competency profile (0–100) from sector and wording.
/// Used for person–job fit; not a hard apply gate.
/// </summary>
public static class VacancyCompetencyProfile
{
    public static CompetencyScores Infer(Vacancy vacancy)
        => Infer(
            WorkTypeLabels.ResolveLabels(vacancy.WorkTypes, vacancy.WorkTypeLabels),
            vacancy.Title,
            vacancy.Description);

    public static CompetencyScores Infer(VacancyDiscoveryRecord vacancy)
        => Infer(vacancy.WorkTypeLabelList, vacancy.Title, vacancy.Description);

    public static CompetencyScores Infer(
        IReadOnlyList<string>? workTypes,
        string? title,
        string? description)
    {
        var types = workTypes?
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        int samenwerken;
        int resultaat;
        int stress;
        int innovatie;
        if (types.Count == 0)
        {
            samenwerken = 60;
            resultaat = 60;
            stress = 60;
            innovatie = 60;
        }
        else
        {
            var profiles = types.Select(ForWorkType).ToList();
            samenwerken = Average(profiles.Select(p => p.Samenwerken ?? 60));
            resultaat = Average(profiles.Select(p => p.Resultaatgerichtheid ?? 60));
            stress = Average(profiles.Select(p => p.Stressbestendigheid ?? 60));
            innovatie = Average(profiles.Select(p => p.Innovatie ?? 60));
        }

        var blob = $"{title} {description}";
        samenwerken = Boost(samenwerken, blob, "samenwerk", "team", "collega", "klantgericht", "gastvrij");
        resultaat = Boost(resultaat, blob, "resultaat", "target", "omzet", "haal", "deadline", "productie");
        stress = Boost(stress, blob, "druk", "piek", "nacht", "stress", "spoed", "avond");
        innovatie = Boost(innovatie, blob, "innovat", "oploss", "creatief", "verbeter", "bedenk", "probleem");

        return new CompetencyScores(
            Math.Clamp(samenwerken, 20, 95),
            Math.Clamp(resultaat, 20, 95),
            Math.Clamp(stress, 20, 95),
            Math.Clamp(innovatie, 20, 95));
    }

    public static double Fit01(CompetencyScores candidate, CompetencyScores vacancy)
    {
        var sum = 0d;
        var n = 0;
        foreach (var category in CompetencyTestCatalog.CategoryCodes)
        {
            var c = candidate.TryGet(category);
            var t = vacancy.TryGet(category) ?? 60;
            if (c is null)
            {
                continue;
            }

            var target = Math.Max(t, 1);
            sum += Math.Clamp(c.Value / (double)target, 0, 1);
            n++;
        }

        return n == 0 ? 0.5 : sum / n;
    }

    private static CompetencyScores ForWorkType(string label) => label.Trim() switch
    {
        WorkTypeLabels.Horeca => new(80, 65, 85, 45),
        WorkTypeLabels.Winkel => new(80, 70, 70, 45),
        WorkTypeLabels.Logistiek => new(55, 80, 70, 40),
        WorkTypeLabels.Tuinbouw => new(50, 75, 70, 45),
        WorkTypeLabels.Zorg => new(85, 65, 80, 50),
        WorkTypeLabels.Kantoor => new(70, 75, 55, 75),
        WorkTypeLabels.Bouw => new(60, 80, 70, 50),
        WorkTypeLabels.Schoonmaak => new(50, 75, 60, 35),
        WorkTypeLabels.Productie => new(55, 85, 65, 40),
        _ => new(60, 60, 60, 60)
    };

    private static int Average(IEnumerable<int> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? 60 : (int)Math.Round(list.Average(), MidpointRounding.AwayFromZero);
    }

    private static int Boost(int current, string blob, params string[] needles)
    {
        if (string.IsNullOrWhiteSpace(blob))
        {
            return current;
        }

        var hit = needles.Any(n => blob.Contains(n, StringComparison.OrdinalIgnoreCase));
        return hit ? current + 8 : current;
    }
}
