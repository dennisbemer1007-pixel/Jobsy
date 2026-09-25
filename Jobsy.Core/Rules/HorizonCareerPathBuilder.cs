namespace Jobsy.Core.Rules;

/// <summary>Local + OpenAI career path steps toward a free-text horizon (no hardcoded current job title).</summary>
public static class HorizonCareerPathBuilder
{
    public const int MaxSteps = 4;

    public static HorizonCareerPathPlan BuildLocal(string dreamTitle, HorizonCareerProfileSnapshot? profile = null)
    {
        var dream = Clamp(dreamTitle);
        var match = 20 + (StableHash(dream) % 21); // 20–40 until live DNA blend
        if (profile?.HasDnaSignal == true)
        {
            match = Math.Clamp(match + 8, 24, 48);
        }

        var query = Uri.EscapeDataString(dream);
        var steps = new List<HorizonCareerPathStep>
        {
            new(
                "base",
                1,
                "Profiel & DNA als basis",
                HorizonCareerStepKind.Completed,
                "Je Lobsy-profiel en DNA-tests vormen het startpunt. We kijken wat je al meeneemt richting je stip.",
                profile?.StrengthHints.Count > 0
                    ? profile.StrengthHints.Take(3).ToList()
                    : ["Samenwerken", "Betrouwbaarheid", "Leervermogen"],
                [],
                ["Vul Mijn DNA / Kompas bij zodat matching scherper wordt"],
                YearsExperienceNeeded: 0,
                "Open Mijn Kompas",
                "/candidate/profile",
                100),
            new(
                "skills",
                2,
                "Skills & competenties dichten",
                HorizonCareerStepKind.Active,
                $"Gap-analyse naar “{dream}”: welke vaardigheden en competenties nog ontbreken.",
                [$"Vakkennis voor {dream}", "Zichtbaar maken van resultaten", "Communicatie in de doelrol"],
                ["Praktijkgericht vakcertificaat of module", "Korte workshop leiderschap of vaktechniek"],
                ["Aantoonbare basisvaardigheden uit je DNA/competentiescan"],
                YearsExperienceNeeded: 0,
                "Bekijk opleidingen & fit",
                "/candidate/profile?tab=fit",
                Math.Clamp(match + 8, 32, 58)),
            new(
                "experience",
                3,
                "Ervaring opbouwen",
                HorizonCareerStepKind.Open,
                "Concrete praktijk: tussentijdse rollen, projecten of verantwoordelijkheden die richting je horizon wijzen.",
                ["Verantwoordelijkheid in team of proces", "Meetbare resultaten in een verwante rol"],
                ["On-the-job learning / interne stage", "Branchegerichte cursus met praktijkopdracht"],
                ["Minimaal aantoonbare inzet in een verwante functie of project"],
                YearsExperienceNeeded: EstimateYears(dream, mid: true),
                "Zoek stap-vacatures",
                "/?q=" + query,
                Math.Max(8, match / 2)),
            new(
                "land",
                4,
                dream,
                HorizonCareerStepKind.Open,
                $"Land bij je stip op de horizon: {dream}. Minimale eisen en ervaring hieronder zijn richtinggevend.",
                ["Eindcompetenties van de droomrol", "Eigenaarschap en besluitvaardigheid"],
                ["Optioneel: vervolgopleiding of branchecertificaat"],
                [$"Passende opleiding of gelijkwaardige ervaring voor {dream}", "Betrouwbare referenties of aantoonbare inzet"],
                YearsExperienceNeeded: EstimateYears(dream, mid: false),
                "Zoek droomvacatures",
                "/?q=" + query,
                0)
        };

        return new HorizonCareerPathPlan(
            dream,
            match,
            $"Pad naar “{dream}” op basis van je profiel en DNA — rustige stappen, geen vaste huidige functietitel.",
            steps);
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
