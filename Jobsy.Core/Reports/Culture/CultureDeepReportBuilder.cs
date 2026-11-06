using Jobsy.Core.Reports.Career;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Reports.Culture;

public static class CultureDeepReportBuilder
{
    private static readonly (string Key, string[] Axes)[] OrgProfiles =
    [
        ("startup", [CulturePersonalityCatalog.Autonomy, CulturePersonalityCatalog.Innovation, CulturePersonalityCatalog.Informal]),
        ("family", [CulturePersonalityCatalog.PeopleFirst, CulturePersonalityCatalog.Collaboration, CulturePersonalityCatalog.Agreeableness]),
        ("project", [CulturePersonalityCatalog.Flexibility, CulturePersonalityCatalog.Collaboration, CulturePersonalityCatalog.Conscientiousness]),
        ("corporate", [CulturePersonalityCatalog.Conscientiousness, CulturePersonalityCatalog.EmotionalStability, CulturePersonalityCatalog.Autonomy]),
        ("government", [CulturePersonalityCatalog.EmotionalStability, CulturePersonalityCatalog.PeopleFirst, CulturePersonalityCatalog.Conscientiousness]),
        ("care", [CulturePersonalityCatalog.PeopleFirst, CulturePersonalityCatalog.Collaboration, CulturePersonalityCatalog.EmotionalStability]),
    ];

    public static CultureDeepReport Build(
        IReadOnlyList<DeepAnalysisDomainScore> domainScores,
        IReadOnlyDictionary<string, double>? normMeans,
        DateTime utcNow)
    {
        var byDomain = domainScores.ToDictionary(d => d.Domain, d => d.Percent, StringComparer.OrdinalIgnoreCase);
        var domains = CulturePersonalityCatalog.CategoryCodes
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
            .ToList();

        var cultureAxes = domains
            .Where(d => CulturePersonalityCatalog.CultureDimensionCodes.Contains(d.Domain, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(d => d.Score)
            .ToList();
        var top = cultureAxes.Take(2).ToList();
        var bottom = cultureAxes.TakeLast(1).ToList();

        var employers = OrgProfiles
            .Select(p =>
            {
                var scores = p.Axes.Select(a => byDomain.TryGetValue(a, out var s) ? s : 50).ToList();
                var match = (int)Math.Clamp(Math.Round(scores.Average()), 0, 100);
                return new DeepEmployerFit { OrgTypeKey = p.Key, MatchPercent = match };
            })
            .OrderByDescending(e => e.MatchPercent)
            .ToList();

        return new CultureDeepReport
        {
            ReportVersion = CultureDeepReportJson.CurrentReportVersion,
            GeneratedAtUtc = utcNow,
            Summary = LocalizedReportText.FromPair(
                "Dit rapport laat zien hoe jij graag werkt (werkcultuur) en hoe jij in een team past (persoonlijkheid op het werk).",
                "This report shows how you like to work (culture fit) and how you show up in a team (workplace personality)."),
            Domains = domains,
            Employers = employers,
            ActionPlan = BuildPlan(top, bottom),
            StrengthKeys = top.Select(d => $"c.strength.{d.Domain}").ToList(),
            PitfallKeys = bottom.Select(d => $"c.pitfall.{d.Domain}").ToList(),
            ComparisonAvailable = normMeans is { Count: > 0 }
        };
    }

    private static List<DeepActionStep> BuildPlan(List<DeepDomainScore> top, List<DeepDomainScore> bottom)
    {
        var steps = new List<DeepActionStep>();
        foreach (var d in top.Take(2))
        {
            var nl = DeepReportCatalog.CultureLabel(d.Domain, "nl");
            var en = DeepReportCatalog.CultureLabel(d.Domain, "en");
            steps.Add(new DeepActionStep
            {
                Title = LocalizedReportText.FromPair($"Zoek organisaties met {nl}", $"Look for organisations with {en}"),
                Body = LocalizedReportText.FromPair(
                    "Stel in gesprekken één vraag die dit zichtbaar maakt in het dagelijks werk.",
                    "In conversations, ask one question that makes this visible in day-to-day work.")
            });
        }

        if (bottom.Count > 0)
        {
            var d = bottom[0];
            var nl = DeepReportCatalog.CultureLabel(d.Domain, "nl");
            var en = DeepReportCatalog.CultureLabel(d.Domain, "en");
            steps.Add(new DeepActionStep
            {
                Title = LocalizedReportText.FromPair($"Wees scherp op {nl}", $"Be clear-eyed about {en}"),
                Body = LocalizedReportText.FromPair(
                    "Dit scoort lager — check of dat in een rol een probleem wordt, of juist ruimte geeft.",
                    "This scores lower — check whether that would be a problem in a role, or actually give you room.")
            });
        }

        while (steps.Count < 3)
        {
            steps.Add(new DeepActionStep
            {
                Title = LocalizedReportText.FromPair("Vergelijk twee werkgevers", "Compare two employers"),
                Body = LocalizedReportText.FromPair(
                    "Zet sfeer, autonomie en samenwerking naast elkaar vóór je kiest.",
                    "Line up atmosphere, autonomy and collaboration before you choose.")
            });
        }

        return steps;
    }
}
