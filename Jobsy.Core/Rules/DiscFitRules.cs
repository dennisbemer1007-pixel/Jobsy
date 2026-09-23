namespace Jobsy.Core.Rules;

/// <summary>How a DISC Quick-Scan scores against a job title or occupation.</summary>
public static class DiscFitRules
{
    public static int FitPercent(CareerOccupation? occupation, DiscScores scores)
    {
        var needed = Needed(occupation);
        if (needed.Count == 0)
        {
            return Average(scores);
        }

        return (int)Math.Round(needed.Select(scores.Get).Average(), MidpointRounding.AwayFromZero);
    }

    public static double Fit01(DiscScores scores, string? vacancyTitle, string? vacancyDescription)
    {
        var blob = CareerOccupationKeys.Fold(string.Join(' ', new[] { vacancyTitle, vacancyDescription }.Where(s => !string.IsNullOrWhiteSpace(s))));
        var needed = Infer(blob);
        if (needed.Count == 0)
        {
            return Average(scores) / 100d;
        }

        return needed.Select(scores.Get).Average() / 100d;
    }

    public static double PillarFit01(string pillarId, DiscScores scores)
    {
        var (d, i, s, c) = Targets(pillarId);
        var pairs = new (int Candidate, int Target)[]
        {
            (scores.Dominant ?? 55, d),
            (scores.Invloed ?? 55, i),
            (scores.Stabiel ?? 55, s),
            (scores.Nauwkeurig ?? 55, c)
        };
        var sum = 0d;
        foreach (var (candidate, target) in pairs)
        {
            sum += Math.Clamp(1 - Math.Abs(candidate - target) / 100d, 0, 1);
        }

        return sum / pairs.Length;
    }

    public static IReadOnlyList<string> Needed(CareerOccupation? occupation)
    {
        if (occupation is null)
        {
            return DiscTestCatalog.CategoryCodes;
        }

        var top = occupation.Weights.OrderByDescending(w => w.Weight).Select(w => w.Code).FirstOrDefault();
        return top switch
        {
            CareerTestCatalog.Social => [DiscTestCatalog.Invloed, DiscTestCatalog.Stabiel],
            CareerTestCatalog.Enterprising => [DiscTestCatalog.Dominant, DiscTestCatalog.Invloed],
            CareerTestCatalog.Investigative => [DiscTestCatalog.Nauwkeurig, DiscTestCatalog.Dominant],
            CareerTestCatalog.Artistic => [DiscTestCatalog.Invloed, DiscTestCatalog.Dominant],
            CareerTestCatalog.Conventional => [DiscTestCatalog.Nauwkeurig, DiscTestCatalog.Stabiel],
            _ => [DiscTestCatalog.Dominant, DiscTestCatalog.Stabiel]
        };
    }

    private static int Average(DiscScores scores)
        => (int)Math.Round(
            DiscTestCatalog.CategoryCodes.Select(scores.Get).Average(),
            MidpointRounding.AwayFromZero);

    private static IReadOnlyList<string> Infer(string folded)
    {
        if (string.IsNullOrWhiteSpace(folded))
        {
            return [];
        }

        var hits = new List<string>();
        void Add(string code, params string[] needles)
        {
            if (needles.Any(n => folded.Contains(n, StringComparison.Ordinal)))
            {
                hits.Add(code);
            }
        }

        Add(DiscTestCatalog.Stabiel, "zorg", "verpleeg", "kas", "teelt", "tuin", "kweker");
        Add(DiscTestCatalog.Invloed, "verkoop", "winkel", "retail", "horeca", "klant", "balie");
        Add(DiscTestCatalog.Dominant, "logistiek", "magazijn", "chauffeur", "ploeg", "leiding");
        Add(DiscTestCatalog.Nauwkeurig, "kwaliteit", "admin", "planning", "controle", "lab");
        return hits.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static (int D, int I, int S, int C) Targets(string pillarId) => pillarId switch
    {
        "informeel" => (70, 80, 40, 40),
        "groei" => (75, 70, 40, 50),
        "stabiel" => (40, 40, 85, 75),
        "zelfstandig" => (80, 35, 45, 70),
        "samen" => (40, 85, 75, 45),
        "kalm" => (45, 40, 90, 60),
        "creatief" => (55, 80, 40, 40),
        "zorgvuldig" => (40, 35, 70, 90),
        _ => (55, 55, 55, 55)
    };
}
