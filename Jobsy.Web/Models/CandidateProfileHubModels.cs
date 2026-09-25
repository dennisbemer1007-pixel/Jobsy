namespace Jobsy.Web.Models;

/// <summary>Snapshot for the candidate hub at <c>/profiel</c>.</summary>
public sealed class CandidateProfileHubModel
{
    public CandidateProfileBasics Basics { get; set; } = new();
    public IReadOnlyList<CandidateProfileTestCard> Tests { get; set; } = [];
    public IReadOnlyList<CandidateProfileScoreBar> ScoreBars { get; set; } = [];
    public CandidateProfileSettings Settings { get; set; } = new();
    public int ProfileCompletenessPercent { get; set; }
}

public sealed class CandidateProfileBasics
{
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string HomeArea { get; set; } = "";
    public int MaxTravelMinutes { get; set; }
    public string TransportLabel { get; set; } = "";
    public bool OpenForWork { get; set; }
    public string CvStatusLabel { get; set; } = "";
    public bool CvReady { get; set; }
    public string CurrentRoleTitle { get; set; } = "";
}

/// <summary>Progress of a DNA test card: free quick-scan and optional paid deep analysis.</summary>
public enum CandidateDnaTestStage
{
    NotStarted = 0,
    FreeCompleted = 1,
    DeepCompleted = 2
}

public sealed class CandidateProfileTestCard
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public CandidateDnaTestStage Stage { get; set; }
    public bool SupportsDeepAnalysis { get; set; } = true;
    public string StatusBadge { get; set; } = "";
    public string FreeTestHref { get; set; } = "";
    public string DeepAnalysisHref { get; set; } = "";
}

public sealed class CandidateProfileScoreBar
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public int Percent { get; set; }
    public string Hint { get; set; } = "";
}

public sealed class CandidateProfileSettings
{
    public bool EmailNotifications { get; set; }
    public bool PushNotifications { get; set; }
    public bool ShareTalentPool { get; set; }
    public bool HideContactUntilMatch { get; set; } = true;
}
