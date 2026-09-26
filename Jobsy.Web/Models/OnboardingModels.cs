namespace Jobsy.Web.Models;

public sealed class OnboardingState
{
    public int CurrentStep { get; set; } = 1;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public bool IsComplete { get; set; }
    public bool ShouldShow { get; set; }
    public string? Source { get; set; }
    public List<OnboardingStepEvent> Steps { get; set; } = [];
    public OnboardingImpression? Impression { get; set; }
}

public sealed class OnboardingStepEvent
{
    public int Step { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? SkippedAtUtc { get; set; }
}

public sealed class OnboardingImpression
{
    public string Label { get; set; } = "";
    public List<OnboardingImpressionItem> Strengths { get; set; } = [];
    public List<OnboardingImpressionItem> Riasec { get; set; } = [];
    public OnboardingImpressionItem? CultureHighlight { get; set; }
    public OnboardingImpressionItem? TopValue { get; set; }
    public int MatchingVacancyCount { get; set; }
    public List<OnboardingMatchCard> MatchCards { get; set; } = [];
    public List<string> DreamJobSuggestions { get; set; } = [];
    public bool CompetencyProvisional { get; set; }
    public bool CareerProvisional { get; set; }
    public bool CultureProvisional { get; set; }
    public bool ValuesProvisional { get; set; }
}

public sealed class OnboardingImpressionItem
{
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public string Sentence { get; set; } = "";
    public int? Percent { get; set; }
}

public sealed class OnboardingMatchCard
{
    public Guid VacancyId { get; set; }
    public string Title { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public int MatchPercent { get; set; }
    public string WhyLine { get; set; } = "";
    public bool IsProvisional { get; set; }
}
