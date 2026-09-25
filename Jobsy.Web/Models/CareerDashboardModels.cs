namespace Jobsy.Web.Models;

/// <summary>Status of a career-path step on the /carriere dashboard.</summary>
public enum CareerStepStatus
{
    Completed = 0,
    Active = 1,
    Open = 2
}

/// <summary>Full career dashboard snapshot for the current → dream role path.</summary>
public sealed class CareerDashboardModel
{
    /// <summary>Deprecated — left empty; path is horizon-first without a fixed current job title.</summary>
    public string CurrentRoleTitle { get; set; } = "";
    public string DreamRoleTitle { get; set; } = "";
    public string DreamRoleId { get; set; } = "";
    public int MatchPercent { get; set; }
    public string MatchSummary { get; set; } = "";
    public IReadOnlyList<CareerDreamOption> DreamOptions { get; set; } = [];
    public IReadOnlyList<CareerPathDashboardStep> Steps { get; set; } = [];
}

public sealed class CareerDreamOption
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
}

public sealed class CareerPathDashboardStep
{
    public string Id { get; set; } = "";
    public int Order { get; set; }
    public string Title { get; set; } = "";
    public CareerStepStatus Status { get; set; }
    public string Summary { get; set; } = "";
    public IReadOnlyList<string> SkillsGap { get; set; } = [];
    public IReadOnlyList<string> Courses { get; set; } = [];
    public IReadOnlyList<string> MinRequirements { get; set; } = [];
    public int YearsExperienceNeeded { get; set; }
    public IReadOnlyList<string> Competencies { get; set; } = [];
    public string ActionLabel { get; set; } = "";
    public string ActionHref { get; set; } = "";
    public int StepMatchPercent { get; set; }
}

public sealed class CareerPathPlanApiModel
{
    public string DreamTitle { get; set; } = "";
    public int MatchPercent { get; set; }
    public string MatchSummary { get; set; } = "";
    public List<CareerPathStepApiModel> Steps { get; set; } = [];
}

public sealed class CareerPathStepApiModel
{
    public string Id { get; set; } = "";
    public int Order { get; set; }
    public string Title { get; set; } = "";
    public string Status { get; set; } = "Open";
    public string Summary { get; set; } = "";
    public List<string> SkillsGap { get; set; } = [];
    public List<string> Courses { get; set; } = [];
    public List<string> MinRequirements { get; set; } = [];
    public int YearsExperienceNeeded { get; set; }
    public string ActionLabel { get; set; } = "";
    public string ActionHref { get; set; } = "";
    public int StepMatchPercent { get; set; }
}
