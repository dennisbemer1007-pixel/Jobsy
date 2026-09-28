using Jobsy.Core.Rules;

namespace Jobsy.Core.Reports.Career;

public static class CareerDeepReportBuilder
{
    public static CareerDeepReport Build(
        IReadOnlyList<DeepAnalysisDomainScore> domainScores,
        CareerCompassSnapshot? compass,
        IReadOnlyDictionary<string, double>? normMeans,
        DateTime utcNow)
    {
        var riasec = DeepAnalysisCatalog.ToRiasecScores(domainScores);
        var domains = CareerTestCatalog.RiasecCodes
            .Select(code =>
            {
                var score = riasec.Get(code);
                return new DeepDomainScore
                {
                    Domain = code,
                    Score = score,
                    LevelKey = DeepReportCatalog.LevelKey(score),
                    NormMean = normMeans is not null && normMeans.TryGetValue(code, out var m) ? m : null
                };
            })
            .ToList();

        var holland = CareerTestCatalog.HollandCode(riasec);
        var ranked = domains.OrderByDescending(d => d.Score).ThenBy(d => d.Domain).ToList();
        var top3 = ranked.Take(3).Select(d => d.Domain).ToList();
        var bottom2 = ranked.TakeLast(2).Select(d => d.Domain).ToList();

        var occupations = (compass?.AllOccupations ?? [])
            .OrderByDescending(m => m.Percent)
            .Take(10)
            .Select(m => new DeepOccupationFit
            {
                TitleNl = m.Title,
                TitleEn = EnglishOccupation(m.Title),
                MatchPercent = m.Percent,
                ReasonNl = m.Why,
                ReasonEn = EnglishReason(m.Why),
                Band = m.Band
            })
            .ToList();

        var comparisonAvailable = normMeans is { Count: > 0 };

        return new CareerDeepReport
        {
            ReportVersion = CareerDeepReportJson.CurrentReportVersion,
            GeneratedAtUtc = utcNow,
            FromOpenAi = false,
            Summary = LocalizedReportText.FromPair(
                $"Je Holland-code is {holland}. Je scoort het sterkst op {string.Join(", ", top3.Select(c => DeepReportCatalog.RiasecLabel(c, "nl")))}.",
                $"Your Holland code is {holland}. You score highest on {string.Join(", ", top3.Select(c => DeepReportCatalog.RiasecLabel(c, "en")))}."),
            Domains = domains,
            HollandCode = holland,
            Occupations = occupations,
            ActionPlan = BuildActionPlan(top3),
            StrengthKeys = top3.Select(c => $"strength.{c}").ToList(),
            PitfallKeys = bottom2.Select(c => $"pitfall.{c}").ToList(),
            ComparisonAvailable = comparisonAvailable
        };
    }

    private static List<DeepActionStep> BuildActionPlan(IReadOnlyList<string> top3)
    {
        var steps = new List<DeepActionStep>();
        foreach (var code in top3.Take(3))
        {
            var nl = DeepReportCatalog.RiasecLabel(code, "nl");
            var en = DeepReportCatalog.RiasecLabel(code, "en");
            steps.Add(new DeepActionStep
            {
                Title = LocalizedReportText.FromPair(
                    $"Verken werk rondom «{nl}»",
                    $"Explore work around “{en}”"),
                Body = LocalizedReportText.FromPair(
                    $"Kies één concreet voorbeeldberoep uit je topmatches en praat met iemand die dit werk doet.",
                    $"Pick one concrete role from your top matches and talk to someone who does that work.")
            });
        }

        while (steps.Count < 3)
        {
            steps.Add(new DeepActionStep
            {
                Title = LocalizedReportText.FromPair("Noteer wat je energie geeft", "Note what gives you energy"),
                Body = LocalizedReportText.FromPair(
                    "Houd een week bij welke taken je energie geven. Dat scherpt je volgende keuzes.",
                    "For one week, note which tasks give you energy. That sharpens your next choices.")
            });
        }

        return steps;
    }

    private static string EnglishOccupation(string nl) => nl switch
    {
        "Verpleegkundige" => "Nurse",
        "Docent" => "Teacher",
        "Softwareontwikkelaar" => "Software developer",
        "Accountant" => "Accountant",
        "Verkoper" => "Sales advisor",
        "Monteur" => "Technician",
        "Ontwerper" => "Designer",
        "Onderzoeker" => "Researcher",
        _ => nl // compass titles may already be mixed; keep as-is when unknown
    };

    private static string EnglishReason(string nl)
    {
        if (string.IsNullOrWhiteSpace(nl))
        {
            return "";
        }

        // Plain rewrite of common Dutch reason stems without machine calques.
        return nl
            .Replace("Past bij", "Fits", StringComparison.Ordinal)
            .Replace("je sterke", "your strong", StringComparison.Ordinal)
            .Replace("jouw", "your", StringComparison.OrdinalIgnoreCase);
    }
}
