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

public sealed class CandidateProfileTestCard
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public bool Completed { get; set; }
    public string StatusBadge { get; set; } = "";
    public string ActionLabel { get; set; } = "";
    public string ActionHref { get; set; } = "";
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
