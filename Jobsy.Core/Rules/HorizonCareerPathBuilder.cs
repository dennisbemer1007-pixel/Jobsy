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
        var blob = dream.ToLowerInvariant();
        if (blob.Contains("timmerman") || blob.Contains("timmer") || blob.Contains("carpenter"))
        {
            return CareerPlanJson.WithStableKeys(BuildTimmermanPlan(dream, match, profileNote: profile?.HasDnaSignal == true
                ? "op basis van je profiel"
                : "op basis van je droombaan; vul je profiel in voor een scherper stappenplan"));
        }

        var steps = new List<HorizonCareerPathStep>
        {
            ContentStep(
                1,
                "Profiel als basis",
                "Je Lobsy-profiel vormt het startpunt. We wegen wat je al meeneemt richting je droombaan, zonder vaste huidige functietitel.",
                strength,
                [],
                ["Profiel bijgewerkt zodat matching scherper wordt"],
                YearsExperienceNeeded: 0,
                "Open Mijn Kompas",
                "/candidate/profile"),
            ContentStep(
                2,
                "Skills & competenties dichten",
                $"Op weg naar “{dream}”: wat je nog mist op skills en competenties, plus gerichte opleidingen.",
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
                "/banenkaart?q=" + query),
            ContentStep(
                4,
                dream,
                $"Land bij je stip op de horizon: {dream}. Minimale eisen en ervaring hieronder zijn richtinggevend.",
                ["Eindcompetenties van de droomrol", "Eigenaarschap en besluitvaardigheid"],
                ["Optioneel: vervolgopleiding of branchecertificaat"],
                [$"Passende opleiding of gelijkwaardige ervaring voor {dream}", "Betrouwbare referenties of aantoonbare inzet"],
                YearsExperienceNeeded: EstimateYears(dream, mid: false),
                "Zoek droomvacatures",
                "/banenkaart?q=" + query)
        };

        var profileNote = profile?.HasDnaSignal == true
            ? "op basis van je profiel"
            : "op basis van je droombaan; vul je profiel in voor een scherper stappenplan";
        return CareerPlanJson.WithStableKeys(new HorizonCareerPathPlan(
            dream,
            match,
            $"Pad naar “{dream}” {profileNote} — rustige, duidelijke stappen zonder vaste huidige rol.",
            steps));
    }

    private static HorizonCareerPathPlan BuildTimmermanPlan(string dream, int match, string profileNote)
    {
        var steps = new List<HorizonCareerPathStep>
        {
            ContentStep(
                1,
                "MBO Timmeren (niveau 2–3)",
                "Start met een praktijkgerichte timmeropleiding: werken met hout, tekening lezen en veilig werken op de bouw.",
                ["Basis meten en zagen", "Tekening lezen", "Werken volgens veiligheidsregels"],
                ["MBO Timmeren", "Entree bouw & infra"],
                ["Startkwalificatie of instroom via BBL"],
                YearsExperienceNeeded: 0,
                "Zoek timmer-opleidingen",
                "/candidate/paspoort?tab=fit"),
            ContentStep(
                2,
                "BBL / leerbedrijf",
                "Leer het vak op de werkvloer: meelopen met een timmerman, opdrachten uitvoeren en je portfolio opbouwen.",
                ["Praktijkervaring op bouwplaats", "Werken in team met vakmensen", "Kwaliteit en netheid"],
                ["BBL timmeren", "Leer-werkplek bouw"],
                ["Leerbedrijf met begeleiding"],
                YearsExperienceNeeded: 0,
                "Bekijk stage & BBL-vacatures",
                "/banenkaart?q=timmerman"),
            ContentStep(
                3,
                "VCA & bouwveiligheid",
                "Voor veel bouwplaatsen heb je VCA nodig. Combineer dat met werken volgens tekening en klantafspraken.",
                ["VCA Basis", "Werken volgens tekening", "Materiaalkeuze hout"],
                ["VCA Basis", "BHV op de bouw"],
                ["VCA-diploma"],
                YearsExperienceNeeded: 0,
                "Plan VCA",
                "/candidate/paspoort?tab=proof"),
            ContentStep(
                4,
                dream,
                $"Richt je op vakmanschap als {dream}: zelfstandig opmeten, maken en monteren met vaste kwaliteit.",
                ["Zelfstandig opmeten en maken", "Afwerking en montage", "Klantcontact op de bouw"],
                ["Aansluitende timmer-cursus"],
                ["Aantoonbare praktijk als timmerman", "VCA en portfolio met projecten"],
                YearsExperienceNeeded: 1,
                "Zoek timmerman-vacatures",
                "/banenkaart?q=timmerman")
        };

        return new HorizonCareerPathPlan(
            dream,
            match,
            $"Pad naar “{dream}” {profileNote} — timmeren via opleiding, BBL en VCA.",
            steps);
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
