namespace Jobsy.Core.Rules;

/// <summary>
/// Compares candidate and vacancy Schwartz value profiles (workplace drivers).
/// </summary>
public static class SchwartzValuesFitRules
{
    /// <summary>Similarity 0–1 between candidate values and vacancy values (or candidate strength when vacancy is absent).</summary>
    public static double Fit01(SchwartzValuesScores candidate, SchwartzValuesScores? vacancyOrNull)
    {
        if (vacancyOrNull is not null)
        {
            var sum = 0d;
            var count = 0;
            foreach (var code in SchwartzValuesCatalog.CategoryCodes)
            {
                var c = GetPercentOrNull(candidate, code);
                var v = GetPercentOrNull(vacancyOrNull, code);
                if (c is null || v is null)
                {
                    continue;
                }

                sum += 1 - Math.Abs(c.Value - v.Value) / 100d;
                count++;
            }

            if (count > 0)
            {
                return Math.Clamp(sum / count, 0, 1);
            }
        }

        return CandidateTopDriversStrength01(candidate);
    }

    public static int FitPercent(SchwartzValuesScores candidate, SchwartzValuesScores? vacancyOrNull)
        => (int)Math.Clamp(Math.Round(100 * Fit01(candidate, vacancyOrNull), MidpointRounding.AwayFromZero), 0, 100);

    /// <summary>
    /// Fit against vacancy title/description heuristics (drijfveren implied by the role text).
    /// </summary>
    public static double Fit01(SchwartzValuesScores candidate, string? vacancyTitle, string? vacancyDescription)
    {
        var inferred = InferVacancyDrivers(vacancyTitle, vacancyDescription);
        return Fit01(candidate, inferred);
    }

    /// <summary>Infer which Schwartz drivers a vacancy emphasizes from plain job text.</summary>
    public static SchwartzValuesScores? InferVacancyDrivers(string? vacancyTitle, string? vacancyDescription)
    {
        var blob = string.Join(' ', new[] { vacancyTitle, vacancyDescription }.Where(s => !string.IsNullOrWhiteSpace(s)))
            .ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(blob))
        {
            return null;
        }

        int Score(params string[] keys)
            => keys.Count(k => blob.Contains(k, StringComparison.Ordinal)) switch
            {
                0 => 45,
                1 => 62,
                2 => 74,
                _ => 86
            };

        var autonomy = Score("zelfstandig", "eigen regie", "autonomie", "initiatief", "vrijheid", "uitdaging", "afwisseling");
        var connection = Score("team", "samenwerk", "collega", "klantgericht", "gastvrij", "mensgericht", "zorg");
        var achievement = Score("target", "resultaat", "prestatie", "kpi", "doelstelling", "groei", "ambitieu");
        var stability = Score("zekerheid", "vast contract", "veilig", "procedure", "protocol", "stabiel", "voorspelbaar");
        var impact = Score("duurzaam", "maatschappelijk", "impact", "milieu", "inclusie", "eerlijk", "verantwoord");

        if (autonomy <= 45 && connection <= 45 && achievement <= 45 && stability <= 45 && impact <= 45)
        {
            return null;
        }

        return new SchwartzValuesScores(autonomy, connection, achievement, stability, impact);
    }

    private static double CandidateTopDriversStrength01(SchwartzValuesScores candidate)
    {
        var percents = SchwartzValuesCatalog.CategoryCodes
            .Select(code => GetPercentOrNull(candidate, code))
            .Where(p => p is not null)
            .Select(p => p!.Value)
            .OrderByDescending(p => p)
            .Take(3)
            .ToList();

        if (percents.Count == 0)
        {
            return 0.55;
        }

        var mean = percents.Average() / 100d;
        return Math.Clamp(mean, 0, 1);
    }

    private static int? GetPercentOrNull(SchwartzValuesScores scores, string code) => code switch
    {
        SchwartzValuesCatalog.Autonomy => scores.Autonomy,
        SchwartzValuesCatalog.Connection => scores.Connection,
        SchwartzValuesCatalog.Achievement => scores.Achievement,
        SchwartzValuesCatalog.Stability => scores.Stability,
        SchwartzValuesCatalog.Impact => scores.Impact,
        _ => null
    };
}
