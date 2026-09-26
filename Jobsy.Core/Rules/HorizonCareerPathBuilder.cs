namespace Jobsy.Core.Rules;

/// <summary>Local career path steps toward a free-text horizon (content only; status resolved on read).</summary>
public static class HorizonCareerPathBuilder
{
    public const int MaxSteps = 4;

    public static HorizonCareerPathPlan BuildLocal(string dreamTitle, HorizonCareerProfileSnapshot? profile = null)
    {
        var dream = Clamp(dreamTitle);
        var match = 20 + (StableHash(dream) % 21);
        if (profile?.HasDnaSignal == true)
        {
            match = Math.Clamp(match + 8 + Math.Min(12, profile.StrengthHints.Count * 2), 24, 58);
        }

        var strength = profile?.StrengthHints is { Count: > 0 }
            ? profile.StrengthHints.Take(4).ToList()
            : ["Samenwerken", "Betrouwbaarheid", "Leervermogen"];
        var gaps = profile?.GapHints is { Count: > 0 }
            ? profile.GapHints.Take(4).ToList()
            : [$"Vakkennis voor {dream}", "Zichtbaar maken van resultaten", "Communicatie in de doelrol"];
        var query = Uri.EscapeDataString(dream);

        var steps = new List<HorizonCareerPathStep>
        {
            ContentStep(
                1,
                "Profiel & DNA als basis",
                "Je Lobsy-profiel en DNA-tests vormen het startpunt. We wegen wat je al meeneemt richting je stip — zonder vaste huidige functietitel.",
                strength,
                [],
                ["DNA/Kompas bijgewerkt zodat matching scherper wordt"],
                YearsExperienceNeeded: 0,
                "Open Mijn Kompas",
                "/candidate/profile"),
            ContentStep(
                2,
                "Skills & competenties dichten",
                $"Gap-analyse naar “{dream}”: wat je nog mist op skills en competenties, plus gerichte opleidingen.",
                gaps,
                DreamCourses(dream),
                ["Aantoonbare basisvaardigheden uit je DNA/competentiescan"],
                YearsExperienceNeeded: 0,
                "Bekijk opleidingen & fit",
                "/candidate/profile?tab=fit"),
            ContentStep(
                3,
                "Ervaring opbouwen",
                "Concrete praktijk: tussentijdse rollen of projecten die richting je horizon wijzen.",
                ["Verantwoordelijkheid in team of proces", "Meetbare resultaten in een verwante rol"],
                ["On-the-job learning / interne stage", "Branchegerichte cursus met praktijkopdracht"],
                ["Minimaal aantoonbare inzet in een verwante functie of project"],
                YearsExperienceNeeded: EstimateYears(dream, mid: true),
                "Zoek stap-vacatures",
                "/?q=" + query),
            ContentStep(
                4,
                dream,
                $"Land bij je stip op de horizon: {dream}. Minimale eisen en ervaring hieronder zijn richtinggevend.",
                ["Eindcompetenties van de droomrol", "Eigenaarschap en besluitvaardigheid"],
                ["Optioneel: vervolgopleiding of branchecertificaat"],
                [$"Passende opleiding of gelijkwaardige ervaring voor {dream}", "Betrouwbare referenties of aantoonbare inzet"],
                YearsExperienceNeeded: EstimateYears(dream, mid: false),
                "Zoek droomvacatures",
                "/?q=" + query)
        };

        var dnaNote = profile?.HasDnaSignal == true
            ? "op basis van je profiel en DNA"
            : "op basis van je stip; vul DNA in voor een scherpere gap-analyse";
        return CareerPlanJson.WithStableKeys(new HorizonCareerPathPlan(
            dream,
            match,
            $"Pad naar “{dream}” {dnaNote} — rustige, diepe stappen zonder vaste huidige rol.",
            steps));
    }

    private static HorizonCareerPathStep ContentStep(
        int order,
        string title,
        string summary,
        IReadOnlyList<string> skillsGap,
        IReadOnlyList<string> courses,
        IReadOnlyList<string> minRequirements,
        int YearsExperienceNeeded,
        string actionLabel,
        string actionHref)
        => new(
            CareerStepKey.Create(order, title),
            order,
            title,
            HorizonCareerStepKind.Open,
            summary,
            skillsGap,
            courses,
            minRequirements,
            YearsExperienceNeeded,
            actionLabel,
            actionHref,
            StepMatchPercent: 0);

    private static IReadOnlyList<string> DreamCourses(string dream)
    {
        var blob = dream.ToLowerInvariant();
        if (blob.Contains("hr") || blob.Contains("personeel"))
        {
            return ["Basis arbeidsrecht / HR-processen", "Gesprekstechniek & feedback"];
        }

        if (blob.Contains("manager") || blob.Contains("leider") || blob.Contains("coach"))
        {
            return ["Praktijkgericht leiderschap / coachmodule", "Korte workshop roosteren of teamsturing"];
        }

        if (blob.Contains("planner") || blob.Contains("logistiek"))
        {
            return ["Planningstools / Excel voor planning", "Capaciteit & prioriteiten op de werkvloer"];
        }

        return ["Praktijkgericht vakcertificaat of module", "Korte workshop leiderschap of vaktechniek"];
    }

    private static int EstimateYears(string dream, bool mid)
    {
        var blob = dream.ToLowerInvariant();
        var senior = blob.Contains("manager")
                     || blob.Contains("leider")
                     || blob.Contains("hoofd")
                     || blob.Contains("adviseur");
        if (mid)
        {
            return senior ? 1 : 0;
        }

        return senior ? 3 : 1;
    }

    private static string Clamp(string title)
    {
        var t = (title ?? "").Trim();
        if (t.Length > 80)
        {
            t = t[..80].Trim();
        }

        return string.IsNullOrWhiteSpace(t) ? "Jouw droombaan" : t;
    }

    private static int StableHash(string value)
    {
        unchecked
        {
            var hash = 23;
            foreach (var ch in value.ToLowerInvariant())
            {
                hash = (hash * 31) + ch;
            }

            return Math.Abs(hash);
        }
    }
}

public enum HorizonCareerStepKind
{
    Completed = 0,
    Active = 1,
    Open = 2
}

public sealed record HorizonCareerProfileSnapshot(
    IReadOnlyList<string> StrengthHints,
    IReadOnlyList<string> GapHints,
    bool HasDnaSignal);

public sealed record HorizonCareerPathStep(
    string Id,
    int Order,
    string Title,
    HorizonCareerStepKind Status,
    string Summary,
    IReadOnlyList<string> SkillsGap,
    IReadOnlyList<string> Courses,
    IReadOnlyList<string> MinRequirements,
    int YearsExperienceNeeded,
    string ActionLabel,
    string ActionHref,
    int StepMatchPercent);

public sealed record HorizonCareerPathPlan(
    string DreamTitle,
    int MatchPercent,
    string MatchSummary,
    IReadOnlyList<HorizonCareerPathStep> Steps);
