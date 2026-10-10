namespace Jobsy.Core.Entities;

/// <summary>Post-four-tests pilot feedback. Open text is never exported in pilot CSV.</summary>
public class PassportFourTestsFeedback
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public int HelpfulnessRating { get; set; }
    public string OpenAnswer { get; set; } = "";
    public bool ShareWithPilot { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
