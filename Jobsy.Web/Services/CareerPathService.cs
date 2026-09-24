using Jobsy.Web.Models;

namespace Jobsy.Web.Services;

/// <summary>
/// Provides career-path dashboard data (current role → dream role) with mock steps
/// so the /carriere UI can be exercised without live talent-profile APIs.
/// </summary>
public sealed class CareerPathService
{
    public const string DefaultDreamId = "teamleider-logistiek";

    private static readonly CareerDreamOption[] DreamCatalog =
    [
        new() { Id = "teamleider-logistiek", Title = "Teamleider logistiek" },
        new() { Id = "filiaalmanager", Title = "Filiaalmanager" },
        new() { Id = "hr-adviseur", Title = "HR-adviseur" }
    ];

    private static readonly Dictionary<string, (string DreamTitle, int MatchPercent, string Summary, CareerPathDashboardStep[] Steps)> Paths =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["teamleider-logistiek"] = (
                "Teamleider logistiek",
                32,
                "Op basis van je talentprofiel heb je al een stevige basis in de operatie. Leidinggeven en plannen vormen de grootste stap.",
                [
                    new CareerPathDashboardStep
                    {
                        Id = "tl-1",
                        Order = 1,
                        Title = "Stevige basis in het magazijn",
                        Status = CareerStepStatus.Completed,
                        Summary = "Je kent de processen, veiligheid en samenwerking op de werkvloer.",
                        SkillsGap = [],
                        Competencies = ["Samenwerken", "Resultaatgerichtheid", "Nauwkeurigheid"],
                        ActionLabel = "Bekijk passende vacatures",
                        ActionHref = "/?q=magazijn",
                        StepMatchPercent = 100
                    },
                    new CareerPathDashboardStep
                    {
                        Id = "tl-2",
                        Order = 2,
                        Title = "Leidinggeven op de vloer",
                        Status = CareerStepStatus.Active,
                        Summary = "Leren coachen, briefen en bijsturen van een klein team tijdens de shift.",
                        SkillsGap = ["Feedback geven", "Werkverdeling", "Conflictvaardigheden"],
                        Competencies = ["Leiderschap", "Communicatie", "Stressbestendigheid"],
                        ActionLabel = "Bekijk passende cursussen",
                        ActionHref = "/candidate/profile?tab=fit",
                        StepMatchPercent = 48
                    },
                    new CareerPathDashboardStep
                    {
                        Id = "tl-3",
                        Order = 3,
                        Title = "Planning & voorraad",
                        Status = CareerStepStatus.Open,
                        Summary = "Shiftplanning, voorraadinzicht en prioriteiten stellen onder tijdsdruk.",
                        SkillsGap = ["Shiftplanning", "Voorraadanalyse", "KPI-rapportage"],
                        Competencies = ["Analytisch denken", "Organiseren", "Prioriteren"],
                        ActionLabel = "Zoek vacatures voor deze stap",
                        ActionHref = "/?q=planner+logistiek",
                        StepMatchPercent = 12
                    },
                    new CareerPathDashboardStep
                    {
                        Id = "tl-4",
                        Order = 4,
                        Title = "Doorgroeien naar teamleider",
                        Status = CareerStepStatus.Open,
                        Summary = "Eindverantwoordelijkheid voor teamresultaat, kwaliteit en veiligheid.",
                        SkillsGap = ["Verantwoordelijkheid dragen", "Functioneringsgesprekken", "Verbetertrajecten"],
                        Competencies = ["Leiderschap", "Besluitvaardigheid", "Eigenaarschap"],
                        ActionLabel = "Zoek vacatures voor deze stap",
                        ActionHref = "/?q=teamleider+logistiek",
                        StepMatchPercent = 0
                    }
                ]),
            ["filiaalmanager"] = (
                "Filiaalmanager",
                24,
                "Je kunt klanten helpen en processen volgen. Om filiaalmanager te worden groeit vooral commercieel inzicht en teamsturing.",
                [
                    new CareerPathDashboardStep
                    {
                        Id = "fm-1",
                        Order = 1,
                        Title = "Klantgericht werken",
                        Status = CareerStepStatus.Completed,
                        Summary = "Je bent sterk in service, presentatie en omgaan met klantvragen.",
                        SkillsGap = [],
                        Competencies = ["Klantgerichtheid", "Communicatie", "Flexibiliteit"],
                        ActionLabel = "Bekijk passende vacatures",
                        ActionHref = "/?q=verkoop",
                        StepMatchPercent = 100
                    },
                    new CareerPathDashboardStep
                    {
                        Id = "fm-2",
                        Order = 2,
                        Title = "Commercieel denken",
                        Status = CareerStepStatus.Active,
                        Summary = "Omzetdoelen begrijpen, upsell en winkelprestaties volgen.",
                        SkillsGap = ["Omzetsturing", "Assortimentskennis", "Promotieplannen"],
                        Competencies = ["Resultaatgerichtheid", "Initiatief", "Analytisch denken"],
                        ActionLabel = "Bekijk passende cursussen",
                        ActionHref = "/candidate/profile?tab=fit",
                        StepMatchPercent = 40
                    },
                    new CareerPathDashboardStep
                    {
                        Id = "fm-3",
                        Order = 3,
                        Title = "Team & roosters",
                        Status = CareerStepStatus.Open,
                        Summary = "Collega’s inzetten, roosteren en een fijne werksfeer bewaken.",
                        SkillsGap = ["Roosteren", "Coachen", "Verzuim signaleren"],
                        Competencies = ["Leiderschap", "Organiseren", "Empathie"],
                        ActionLabel = "Zoek vacatures voor deze stap",
                        ActionHref = "/?q=assistent+filiaalmanager",
                        StepMatchPercent = 8
                    },
                    new CareerPathDashboardStep
                    {
                        Id = "fm-4",
                        Order = 4,
                        Title = "Filiaal volledige verantwoordelijkheid",
                        Status = CareerStepStatus.Open,
                        Summary = "Resultaatdenken, personeelszaken en locatie-KPI’s.",
                        SkillsGap = ["Budgetbewaking", "Recruitment", "Locatie-KPI’s"],
                        Competencies = ["Besluitvaardigheid", "Eigenaarschap", "Strategisch denken"],
                        ActionLabel = "Zoek vacatures voor deze stap",
                        ActionHref = "/?q=filiaalmanager",
                        StepMatchPercent = 0
                    }
                ]),
            ["hr-adviseur"] = (
                "HR-adviseur",
                18,
                "Je hebt mensenkennis uit de praktijk. De overstap naar HR vraagt vooral beleidskennis, gesprekstechniek en administratieve precisie.",
                [
                    new CareerPathDashboardStep
                    {
                        Id = "hr-1",
                        Order = 1,
                        Title = "Mensen & gesprekken",
                        Status = CareerStepStatus.Completed,
                        Summary = "Je bent gewend om collega’s te helpen en duidelijk te communiceren.",
                        SkillsGap = [],
                        Competencies = ["Empathie", "Communicatie", "Betrouwbaarheid"],
                        ActionLabel = "Bekijk passende vacatures",
                        ActionHref = "/?q=hr",
                        StepMatchPercent = 100
                    },
                    new CareerPathDashboardStep
                    {
                        Id = "hr-2",
                        Order = 2,
                        Title = "HR-basis & wetgeving",
                        Status = CareerStepStatus.Active,
                        Summary = "Kennismaken met arbeidsrecht, contracten en HR-processen.",
                        SkillsGap = ["Arbeidsrecht basis", "CAO-inzicht", "HR-systemen"],
                        Competencies = ["Analytisch denken", "Nauwkeurigheid", "Integriteit"],
                        ActionLabel = "Bekijk passende cursussen",
                        ActionHref = "/candidate/profile?tab=fit",
                        StepMatchPercent = 35
                    },
                    new CareerPathDashboardStep
                    {
                        Id = "hr-3",
                        Order = 3,
                        Title = "Werving & onboarding",
                        Status = CareerStepStatus.Open,
                        Summary = "Vacatures uitzetten, gesprekken voeren en nieuwe collega’s inwerken.",
                        SkillsGap = ["Sollicitatiegesprekken", "Onboarding", "Employer branding"],
                        Competencies = ["Organiseren", "Oordeelsvorming", "Samenwerken"],
                        ActionLabel = "Zoek vacatures voor deze stap",
                        ActionHref = "/?q=hr+assistent",
                        StepMatchPercent = 5
                    },
                    new CareerPathDashboardStep
                    {
                        Id = "hr-4",
                        Order = 4,
                        Title = "Doorstroom naar HR-adviseur",
                        Status = CareerStepStatus.Open,
                        Summary = "Managers adviseren over ontwikkeling, conflict en verzuim.",
                        SkillsGap = ["Adviesvaardigheid", "Verzuimbeleid", "Talentontwikkeling"],
                        Competencies = ["Oordeelsvorming", "Vertrouwen wekken", "Invloed"],
                        ActionLabel = "Zoek vacatures voor deze stap",
                        ActionHref = "/?q=hr+adviseur",
                        StepMatchPercent = 0
                    }
                ])
        };

    /// <summary>Current role shown in the header (mock talent profile).</summary>
    public string CurrentRoleTitle { get; } = "Magazijnmedewerker";

    public IReadOnlyList<CareerDreamOption> GetDreamOptions() => DreamCatalog;

    public CareerDashboardModel GetDashboard(string? dreamRoleId = null)
    {
        var id = string.IsNullOrWhiteSpace(dreamRoleId) ? DefaultDreamId : dreamRoleId.Trim();
        if (!Paths.TryGetValue(id, out var path))
        {
            id = DefaultDreamId;
            path = Paths[id];
        }

        return new CareerDashboardModel
        {
            CurrentRoleTitle = CurrentRoleTitle,
            DreamRoleId = id,
            DreamRoleTitle = path.DreamTitle,
            MatchPercent = path.MatchPercent,
            MatchSummary = path.Summary,
            DreamOptions = DreamCatalog,
            Steps = path.Steps
        };
    }
}
