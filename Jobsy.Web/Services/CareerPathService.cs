using Jobsy.Web.Models;

namespace Jobsy.Web.Services;

/// <summary>
/// Provides career-path dashboard data (current role → free-text horizon) with mock steps
/// so the /carriere UI can be exercised without live talent-profile APIs.
/// </summary>
public sealed class CareerPathService
{
    public const string DefaultDreamId = "teamleider-logistiek";
    public const string DefaultDreamTitle = "Teamleider logistiek";

    private static readonly CareerDreamOption[] DreamSuggestions =
    [
        new() { Id = "teamleider-logistiek", Title = "Teamleider logistiek" },
        new() { Id = "filiaalmanager", Title = "Filiaalmanager" },
        new() { Id = "hr-adviseur", Title = "HR-adviseur" },
        new() { Id = "assistent-manager", Title = "Assistent-manager" },
        new() { Id = "planner", Title = "Planner" },
        new() { Id = "coach", Title = "Teamcoach" }
    ];

    private static readonly Dictionary<string, (string DreamTitle, int MatchPercent, string Summary, CareerPathDashboardStep[] Steps)> Paths =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["teamleider-logistiek"] = (
                "Teamleider logistiek",
                32,
                "Je hebt al een stevige basis in de operatie. Leidinggeven en plannen vormen de grootste stap.",
                BuildPresetSteps(
                    ("tl-1", "Stevige basis", CareerStepStatus.Completed, "Je kent de processen en samenwerking op de werkvloer.", [], ["Samenwerken", "Resultaatgerichtheid"], "Bekijk vacatures", "/?q=magazijn", 100),
                    ("tl-2", "Leidinggeven", CareerStepStatus.Active, "Coachen, briefen en bijsturen van een klein team.", ["Feedback geven", "Werkverdeling"], ["Leiderschap", "Communicatie"], "Bekijk cursussen", "/candidate/profile?tab=fit", 48),
                    ("tl-3", "Planning", CareerStepStatus.Open, "Shiftplanning en prioriteiten onder tijdsdruk.", ["Shiftplanning", "KPI-rapportage"], ["Organiseren", "Prioriteren"], "Zoek stap-vacatures", "/?q=planner+logistiek", 12),
                    ("tl-4", "Teamleider", CareerStepStatus.Open, "Eindverantwoordelijkheid voor teamresultaat.", ["Functioneringsgesprekken"], ["Besluitvaardigheid", "Eigenaarschap"], "Zoek droomvacatures", "/?q=teamleider+logistiek", 0))),
            ["filiaalmanager"] = (
                "Filiaalmanager",
                24,
                "Service zit al goed. Commercieel inzicht en teamsturing groeien nog.",
                BuildPresetSteps(
                    ("fm-1", "Klantgericht", CareerStepStatus.Completed, "Sterk in service en omgaan met klantvragen.", [], ["Klantgerichtheid", "Communicatie"], "Bekijk vacatures", "/?q=verkoop", 100),
                    ("fm-2", "Commercie", CareerStepStatus.Active, "Omzetdoelen en winkelprestaties volgen.", ["Omzetsturing", "Promotieplannen"], ["Resultaatgerichtheid", "Initiatief"], "Bekijk cursussen", "/candidate/profile?tab=fit", 40),
                    ("fm-3", "Team & roosters", CareerStepStatus.Open, "Collega’s inzetten en werksfeer bewaken.", ["Roosteren", "Coachen"], ["Leiderschap", "Empathie"], "Zoek stap-vacatures", "/?q=assistent+filiaalmanager", 8),
                    ("fm-4", "Filiaal leiden", CareerStepStatus.Open, "Resultaat, personeel en locatie-KPI’s.", ["Budgetbewaking"], ["Eigenaarschap", "Strategisch denken"], "Zoek droomvacatures", "/?q=filiaalmanager", 0))),
            ["hr-adviseur"] = (
                "HR-adviseur",
                18,
                "Mensenkennis uit de praktijk is er. HR vraagt beleidskennis en gesprekstechniek.",
                BuildPresetSteps(
                    ("hr-1", "Mensen & gesprekken", CareerStepStatus.Completed, "Je helpt collega’s en communiceert helder.", [], ["Empathie", "Communicatie"], "Bekijk vacatures", "/?q=hr", 100),
                    ("hr-2", "HR-basis", CareerStepStatus.Active, "Arbeidsrecht, contracten en HR-processen.", ["Arbeidsrecht basis", "CAO-inzicht"], ["Analytisch denken", "Integriteit"], "Bekijk cursussen", "/candidate/profile?tab=fit", 35),
                    ("hr-3", "Werving", CareerStepStatus.Open, "Vacatures, gesprekken en onboarding.", ["Sollicitatiegesprekken", "Onboarding"], ["Organiseren", "Oordeelsvorming"], "Zoek stap-vacatures", "/?q=hr+assistent", 5),
                    ("hr-4", "HR-adviseur", CareerStepStatus.Open, "Managers adviseren over ontwikkeling en verzuim.", ["Adviesvaardigheid"], ["Vertrouwen wekken", "Invloed"], "Zoek droomvacatures", "/?q=hr+adviseur", 0))),
            ["assistent-manager"] = (
                "Assistent-manager",
                28,
                "Je combineert vakkennis met groeiende regie op de werkvloer.",
                BuildPresetSteps(
                    ("am-1", "Vakbasis", CareerStepStatus.Completed, "Je beheerst de dagelijkse uitvoering.", [], ["Betrouwbaarheid", "Samenwerken"], "Bekijk vacatures", "/?q=assistent", 100),
                    ("am-2", "Aansturen", CareerStepStatus.Active, "Collega’s meekrijgen en taken verdelen.", ["Delegeren", "Briefen"], ["Leiderschap", "Communicatie"], "Bekijk cursussen", "/candidate/profile?tab=fit", 42),
                    ("am-3", "Overzicht", CareerStepStatus.Open, "Prioriteiten en kwaliteit bewaken.", ["Prioriteren"], ["Organiseren", "Resultaatgerichtheid"], "Zoek stap-vacatures", "/?q=assistent+manager", 10),
                    ("am-4", "Assistent-manager", CareerStepStatus.Open, "Manager ondersteunen en zelfstandig bijsturen.", ["Eigenaarschap"], ["Besluitvaardigheid"], "Zoek droomvacatures", "/?q=assistent+manager", 0))),
            ["planner"] = (
                "Planner",
                26,
                "Structuur en overzicht passen bij jou. Diepte in planningssystemen groeit nog.",
                BuildPresetSteps(
                    ("pl-1", "Overzicht", CareerStepStatus.Completed, "Je ziet snel wat er speelt op de werkvloer.", [], ["Nauwkeurigheid", "Prioriteren"], "Bekijk vacatures", "/?q=planner", 100),
                    ("pl-2", "Planningstools", CareerStepStatus.Active, "Leren plannen met data en systemen.", ["Excel/planningstool", "Capaciteitsinzicht"], ["Analytisch denken"], "Bekijk cursussen", "/candidate/profile?tab=fit", 44),
                    ("pl-3", "Afstemmen", CareerStepStatus.Open, "Met teams en stakeholders afstemmen.", ["Stakeholdercommunicatie"], ["Samenwerken", "Communicatie"], "Zoek stap-vacatures", "/?q=planner", 14),
                    ("pl-4", "Planner", CareerStepStatus.Open, "Zelfstandig planningen optimaliseren.", ["Optimaliseren"], ["Resultaatgerichtheid"], "Zoek droomvacatures", "/?q=planner", 0))),
            ["coach"] = (
                "Teamcoach",
                22,
                "Je trekt mensen mee. Formele coachvaardigheden maken het verschil.",
                BuildPresetSteps(
                    ("co-1", "Verbinding", CareerStepStatus.Completed, "Je bouwt makkelijk contact met collega’s.", [], ["Empathie", "Communicatie"], "Bekijk vacatures", "/?q=coach", 100),
                    ("co-2", "Coachtechniek", CareerStepStatus.Active, "Luisteren, vragen stellen en feedback geven.", ["Coachende vragen", "Feedback"], ["Luisteren", "Vertrouwen wekken"], "Bekijk cursussen", "/candidate/profile?tab=fit", 38),
                    ("co-3", "Teamdynamiek", CareerStepStatus.Open, "Conflicten en samenwerking begeleiden.", ["Conflictvaardigheden"], ["Oordeelsvorming"], "Zoek stap-vacatures", "/?q=teamcoach", 9),
                    ("co-4", "Teamcoach", CareerStepStatus.Open, "Teams structureel ontwikkelen.", ["Ontwikkeltrajecten"], ["Invloed", "Eigenaarschap"], "Zoek droomvacatures", "/?q=teamcoach", 0)))
        };

    /// <summary>Current role shown in the header (mock talent profile).</summary>
    public string CurrentRoleTitle { get; } = "Magazijnmedewerker";

    public IReadOnlyList<CareerDreamOption> GetDreamSuggestions() => DreamSuggestions;

    /// <summary>
    /// Resolves a dashboard for a free-text horizon (or known suggestion id/title).
    /// </summary>
    public CareerDashboardModel GetDashboard(string? dreamRoleIdOrTitle = null)
    {
        var raw = string.IsNullOrWhiteSpace(dreamRoleIdOrTitle)
            ? DefaultDreamTitle
            : dreamRoleIdOrTitle.Trim();

        if (TryResolvePreset(raw, out var id, out var path))
        {
            return new CareerDashboardModel
            {
                CurrentRoleTitle = CurrentRoleTitle,
                DreamRoleId = id,
                DreamRoleTitle = path.DreamTitle,
                MatchPercent = path.MatchPercent,
                MatchSummary = path.Summary,
                DreamOptions = DreamSuggestions,
                Steps = path.Steps
            };
        }

        var custom = BuildCustomPath(raw);
        return new CareerDashboardModel
        {
            CurrentRoleTitle = CurrentRoleTitle,
            DreamRoleId = "custom",
            DreamRoleTitle = custom.DreamTitle,
            MatchPercent = custom.MatchPercent,
            MatchSummary = custom.Summary,
            DreamOptions = DreamSuggestions,
            Steps = custom.Steps
        };
    }

    private static bool TryResolvePreset(
        string raw,
        out string id,
        out (string DreamTitle, int MatchPercent, string Summary, CareerPathDashboardStep[] Steps) path)
    {
        if (Paths.TryGetValue(raw, out path))
        {
            id = raw;
            return true;
        }

        foreach (var option in DreamSuggestions)
        {
            if (string.Equals(option.Title, raw, StringComparison.OrdinalIgnoreCase)
                && Paths.TryGetValue(option.Id, out path))
            {
                id = option.Id;
                return true;
            }
        }

        id = "";
        path = default;
        return false;
    }

    private static (string DreamTitle, int MatchPercent, string Summary, CareerPathDashboardStep[] Steps) BuildCustomPath(string title)
    {
        var safe = ClampTitle(title);
        var match = 18 + (StableHash(safe) % 17); // 18–34
        var query = Uri.EscapeDataString(safe);
        var steps = BuildPresetSteps(
            ("cu-1", "Huidige basis", CareerStepStatus.Completed,
                $"Je huidige rol als {CurrentRoleTitleStatic} sluit al aan op onderdelen van “{safe}”.",
                [], ["Samenwerken", "Betrouwbaarheid"], "Bekijk vacatures", "/?q=" + query, 100),
            ("cu-2", "Skills aanscherpen", CareerStepStatus.Active,
                "Werk gericht aan de vaardigheden die jouw stip dichterbij brengen.",
                ["Relevante vakkennis", "Zichtbaarheid van je ambities"], ["Initiatief", "Leervermogen"],
                "Bekijk cursussen", "/candidate/profile?tab=fit", Math.Clamp(match + 10, 30, 55)),
            ("cu-3", "Ervaring opbouwen", CareerStepStatus.Open,
                "Zoek tussentijdse rollen of projecten die richting je droombaan wijzen.",
                ["Praktijkervaring in de doelrichting"], ["Resultaatgerichtheid", "Netwerken"],
                "Zoek stap-vacatures", "/?q=" + query, Math.Max(5, match / 3)),
            ("cu-4", safe, CareerStepStatus.Open,
                $"Land bij je stip op de horizon: {safe}.",
                ["Eindcompetenties van de rol"], ["Eigenaarschap", "Besluitvaardigheid"],
                "Zoek droomvacatures", "/?q=" + query, 0));

        return (
            safe,
            match,
            $"Jouw pad naar “{safe}” is persoonlijk. We schetsen rustige stappen op basis van je huidige rol.",
            steps);
    }

    private const string CurrentRoleTitleStatic = "Magazijnmedewerker";

    private static string ClampTitle(string title)
    {
        var t = title.Trim();
        if (t.Length > 80)
        {
            t = t[..80].Trim();
        }

        return string.IsNullOrWhiteSpace(t) ? DefaultDreamTitle : t;
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

    private static CareerPathDashboardStep[] BuildPresetSteps(
        params (string Id, string Title, CareerStepStatus Status, string Summary, string[] Gaps, string[] Competencies, string ActionLabel, string ActionHref, int StepMatch)[] rows)
    {
        var steps = new CareerPathDashboardStep[rows.Length];
        for (var i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            steps[i] = new CareerPathDashboardStep
            {
                Id = row.Id,
                Order = i + 1,
                Title = row.Title,
                Status = row.Status,
                Summary = row.Summary,
                SkillsGap = row.Gaps,
                Competencies = row.Competencies,
                ActionLabel = row.ActionLabel,
                ActionHref = row.ActionHref,
                StepMatchPercent = row.StepMatch
            };
        }

        return steps;
    }
}
