namespace Jobsy.Core.Rules;

/// <summary>
/// Compares candidate culture/personality preferences with company culture (and vacancy pillars).
/// Uses absolute-difference fit on shared dimensions — objective and jargon-free.
/// </summary>
public static class CulturePersonalityFitRules
{
    /// <summary>Similarity 0–1 between two culture/personality profiles on culture dimensions.</summary>
    public static double CultureFit01(CulturePersonalityScores candidate, CulturePersonalityScores company)
    {
        var sum = 0d;
        foreach (var code in CulturePersonalityCatalog.CultureDimensionCodes)
        {
            sum += DimFit01(candidate.Get(code), company.Get(code));
        }

        return sum / CulturePersonalityCatalog.CultureDimensionCodes.Length;
    }

    public static int CultureFitPercent(CulturePersonalityScores candidate, CulturePersonalityScores company)
        => (int)Math.Clamp(Math.Round(100 * CultureFit01(candidate, company), MidpointRounding.AwayFromZero), 0, 100);

    /// <summary>How well candidate culture prefs map onto a vacancy culture pillar id.</summary>
    public static double PillarFit01(string pillarId, CulturePersonalityScores scores)
    {
        var targets = Targets(pillarId);
        var sum = 0d;
        foreach (var (code, target) in targets)
        {
            sum += DimFit01(scores.Get(code), target);
        }

        return sum / targets.Length;
    }

    /// <summary>Personality facet contribution for vacancy title heuristics (replaces DISC job-fit).</summary>
    public static double PersonalityFit01(CulturePersonalityScores scores, string? vacancyTitle, string? vacancyDescription)
    {
        var blob = string.Join(' ', new[] { vacancyTitle, vacancyDescription }.Where(s => !string.IsNullOrWhiteSpace(s)))
            .ToLowerInvariant();
        var needed = InferFacets(blob);
        if (needed.Count == 0)
        {
            return PersonalityAverage(scores) / 100d;
        }

        return needed.Select(scores.Get).Average() / 100d;
    }

    public static int FitPercent(CareerOccupation? occupation, CulturePersonalityScores scores)
    {
        var needed = NeededFacets(occupation);
        if (needed.Count == 0)
        {
            return PersonalityAverage(scores);
        }

        return (int)Math.Round(needed.Select(scores.Get).Average(), MidpointRounding.AwayFromZero);
    }

    public static IReadOnlyList<string> NeededFacets(CareerOccupation? occupation)
    {
        if (occupation is null)
        {
            return CulturePersonalityCatalog.PersonalityFacetCodes;
        }

        var top = occupation.Weights.OrderByDescending(w => w.Weight).Select(w => w.Code).FirstOrDefault();
        return top switch
        {
            CareerTestCatalog.Social =>
            [
                CulturePersonalityCatalog.Agreeableness,
                CulturePersonalityCatalog.Extraversion,
                CulturePersonalityCatalog.Collaboration
            ],
            CareerTestCatalog.Enterprising =>
            [
                CulturePersonalityCatalog.Extraversion,
                CulturePersonalityCatalog.Autonomy,
                CulturePersonalityCatalog.Innovation
            ],
            CareerTestCatalog.Investigative =>
            [
                CulturePersonalityCatalog.Openness,
                CulturePersonalityCatalog.Conscientiousness,
                CulturePersonalityCatalog.Autonomy
            ],
            CareerTestCatalog.Artistic =>
            [
                CulturePersonalityCatalog.Openness,
                CulturePersonalityCatalog.Innovation,
                CulturePersonalityCatalog.Autonomy
            ],
            CareerTestCatalog.Conventional =>
            [
                CulturePersonalityCatalog.Conscientiousness,
                CulturePersonalityCatalog.EmotionalStability,
                CulturePersonalityCatalog.Flexibility
            ],
            _ =>
            [
                CulturePersonalityCatalog.Conscientiousness,
                CulturePersonalityCatalog.EmotionalStability,
                CulturePersonalityCatalog.Collaboration
            ]
        };
    }

    private static double DimFit01(int candidate, int target)
        => Math.Clamp(1 - Math.Abs(candidate - target) / 100d, 0, 1);

    private static int PersonalityAverage(CulturePersonalityScores scores)
        => (int)Math.Round(
            CulturePersonalityCatalog.PersonalityFacetCodes.Select(scores.Get).Average(),
            MidpointRounding.AwayFromZero);

    private static (string Code, int Target)[] Targets(string pillarId)
        => pillarId.Trim().ToLowerInvariant() switch
        {
            "informeel" =>
            [
                (CulturePersonalityCatalog.Informal, 85),
                (CulturePersonalityCatalog.Autonomy, 70),
                (CulturePersonalityCatalog.Flexibility, 75),
                (CulturePersonalityCatalog.PeopleFirst, 70)
            ],
            "groei" =>
            [
                (CulturePersonalityCatalog.Innovation, 88),
                (CulturePersonalityCatalog.Autonomy, 75),
                (CulturePersonalityCatalog.Flexibility, 80),
                (CulturePersonalityCatalog.Openness, 85)
            ],
            "stabiel" =>
            [
                (CulturePersonalityCatalog.Flexibility, 35),
                (CulturePersonalityCatalog.Innovation, 40),
                (CulturePersonalityCatalog.Autonomy, 45),
                (CulturePersonalityCatalog.Conscientiousness, 85)
            ],
            "zelfstandig" =>
            [
                (CulturePersonalityCatalog.Autonomy, 90),
                (CulturePersonalityCatalog.Collaboration, 40),
                (CulturePersonalityCatalog.PeopleFirst, 40),
                (CulturePersonalityCatalog.Conscientiousness, 80)
            ],
            "samen" =>
            [
                (CulturePersonalityCatalog.Collaboration, 90),
                (CulturePersonalityCatalog.PeopleFirst, 85),
                (CulturePersonalityCatalog.Agreeableness, 80),
                (CulturePersonalityCatalog.Extraversion, 70)
            ],
            "kalm" =>
            [
                (CulturePersonalityCatalog.EmotionalStability, 90),
                (CulturePersonalityCatalog.Flexibility, 50),
                (CulturePersonalityCatalog.Informal, 55),
                (CulturePersonalityCatalog.PeopleFirst, 65)
            ],
            "creatief" =>
            [
                (CulturePersonalityCatalog.Innovation, 92),
                (CulturePersonalityCatalog.Openness, 90),
                (CulturePersonalityCatalog.Autonomy, 75),
                (CulturePersonalityCatalog.Flexibility, 80)
            ],
            "zorgvuldig" =>
            [
                (CulturePersonalityCatalog.Conscientiousness, 90),
                (CulturePersonalityCatalog.Flexibility, 40),
                (CulturePersonalityCatalog.Innovation, 40),
                (CulturePersonalityCatalog.Informal, 40)
            ],
            _ =>
            [
                (CulturePersonalityCatalog.Collaboration, 55),
                (CulturePersonalityCatalog.Autonomy, 55),
                (CulturePersonalityCatalog.Innovation, 55),
                (CulturePersonalityCatalog.PeopleFirst, 55)
            ]
        };

    private static IReadOnlyList<string> InferFacets(string folded)
    {
        if (string.IsNullOrWhiteSpace(folded))
        {
            return [];
        }

        var hits = new List<string>();
        void Add(string code, params string[] needles)
        {
            if (needles.Any(n => folded.Contains(n, StringComparison.Ordinal)) && !hits.Contains(code))
            {
                hits.Add(code);
            }
        }

        Add(CulturePersonalityCatalog.Extraversion, "klant", "verkoop", "receptie", "balie", "teamlead");
        Add(CulturePersonalityCatalog.Conscientiousness, "administratie", "kwaliteit", "controle", "boekhoud", "zorgvuldig");
        Add(CulturePersonalityCatalog.Openness, "design", "innovatie", "creatief", "onderzoek", "ontwikkel");
        Add(CulturePersonalityCatalog.Agreeableness, "zorg", "begeleiding", "hr", "coaching", "horeca");
        Add(CulturePersonalityCatalog.EmotionalStability, "spoed", "crisis", "druk", "nacht", "acute");
        return hits;
    }
}
