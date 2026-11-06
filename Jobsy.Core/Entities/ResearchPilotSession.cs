namespace Jobsy.Core.Entities;

/// <summary>Pilot participant without a full Lobsy account.</summary>
public class ResearchPilotSession
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public ResearchPilotCampaign? Campaign { get; set; }

    public string RespondentHash { get; set; } = string.Empty;

    public string? AgeBand { get; set; }
    public string? Gender { get; set; }
    public string? EducationLevel { get; set; }
    public string? NativeLanguage { get; set; }
    public string TestLanguage { get; set; } = "nl";

    public DateTime CreatedAtUtc { get; set; }
}
