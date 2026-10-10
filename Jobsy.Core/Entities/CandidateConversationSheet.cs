namespace Jobsy.Core.Entities;

public class CandidateConversationSheet
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string StrengthsText { get; set; } = "";
    public string MotivationText { get; set; } = "";
    public string CustomText { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
}
