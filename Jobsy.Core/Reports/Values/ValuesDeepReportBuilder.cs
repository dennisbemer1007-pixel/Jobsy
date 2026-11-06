using Jobsy.Core.Reports.Career;
using Jobsy.Core.Reports.Culture;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Reports.Values;

public static class ValuesDeepReportBuilder
{
    private static readonly (string Key, string[] Axes)[] OrgProfiles =
    [
        ("startup", [SchwartzValuesCatalog.Autonomy, SchwartzValuesCatalog.Achievement]),
        ("family", [SchwartzValuesCatalog.Connection, SchwartzValuesCatalog.Stability]),
        ("project", [SchwartzValuesCatalog.Achievement, SchwartzValuesCatalog.Autonomy]),
        ("corporate", [SchwartzValuesCatalog.Stability, SchwartzValuesCatalog.Achievement]),
        ("government", [SchwartzValuesCatalog.Impact, SchwartzValuesCatalog.Stability]),
        ("care", [SchwartzValuesCatalog.Connection, SchwartzValuesCatalog.Impact]),
    ];

    public static ValuesDeepReport Build(
        IReadOnlyList<DeepAnalysisDomainScore> domainScores,
        IReadOnlyDictionary<string, double>? normMeans,
        DateTime utcNow)
    {
        var byDomain = domainScores.ToDictionary(d => d.Domain, d => d.Percent, StringComparer.OrdinalIgnoreCase);
        var domains = SchwartzValuesCatalog.CategoryCodes
            .Select(code =>
            {
                byDomain.TryGetValue(code, out var score);
                return new DeepDomainScore
                {
                    Domain = code,
                    Score = score,
                    LevelKey = DeepReportCatalog.LevelKey(score),
                    NormMean = normMeans is not null && normMeans.TryGetValue(code, out var m) ? m : null
                };
            })
            .OrderByDescending(d => d.Score)
            .ThenBy(d => d.Domain, StringComparer.Ordinal)
            .ToList();

        var top2 = domains.Take(2).ToList();
        var employers = OrgProfiles
            .Select(p =>
            {
                var scores = p.Axes.Select(a => byDomain.TryGetValue(a, out var s) ? s : 50).ToList();
                var match = (int)Math.Clamp(Math.Round(scores.Average()), 0, 100);
                return new DeepEmployerFit { OrgTypeKey = p.Key, MatchPercent = match };
            })
            .OrderByDescending(e => e.MatchPercent)
            .ToList();

        return new ValuesDeepReport
        {
            ReportVersion = ValuesDeepReportJson.CurrentReportVersion,
            GeneratedAtUtc = utcNow,
            Summary = LocalizedReportText.FromPair(
                "Dit waardenrapport rangschikt wat voor jou het zwaarst weegt op het werk — en wat minder.",
                "This work-values report ranks what weighs heaviest for you at work — and what weighs less."),
            Domains = domains,
            Employers = employers,
            ActionPlan = BuildPlan(top2),
            StrengthKeys = top2.Select(d => $"v.strength.{d.Domain}").ToList(),
            PitfallKeys = domains.TakeLast(2).Select(d => $"v.pitfall.{d.Domain}").ToList(),
            ComparisonAvailable = normMeans is { Count: > 0 }
        };
    }

    private static List<DeepActionStep> BuildPlan(List<DeepDomainScore> top2)
    {
        var steps = top2.Select(d =>
        {
            var nl = DeepReportCatalog.ValueLabel(d.Domain, "nl");
            var en = DeepReportCatalog.ValueLabel(d.Domain, "en");
            return new DeepActionStep
            {
                Title = LocalizedReportText.FromPair($"Kies werk dat {nl} voelbaar maakt", $"Choose work that makes {en} tangible"),
                Body = LocalizedReportText.FromPair(
                    ChooseBody(d.Domain, "nl"),
                    ChooseBody(d.Domain, "en"))
            };
        }).ToList();

        steps.Add(new DeepActionStep
        {
            Title = LocalizedReportText.FromPair("Toets je top-2 in gesprekken", "Test your top two in conversations"),
            Body = LocalizedReportText.FromPair(
                "Vraag hoe deze waarden zichtbaar zijn in een gewone werkweek.",
                "Ask how these values show up in an ordinary work week.")
        });

        return steps.Take(3).ToList();
    }

    private static string ChooseBody(string domain, string lang)
    {
        var specific = $"values.choose.{domain}";
        var text = DeepReportCatalog.Get(specific, lang);
        return text == specific ? DeepReportCatalog.Get("values.choose", lang) : text;
    }
}
