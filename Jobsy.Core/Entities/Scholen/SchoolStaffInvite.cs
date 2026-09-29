namespace Jobsy.Core.Entities.Scholen;

/// <summary>
/// Pending school staff invite (SchoolAdmin or Teacher). Token hash + expiry; set-password then forced 2FA.
/// </summary>
public class SchoolStaffInvite
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public School? School { get; set; }
    public Guid? InvitedUserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    /// <summary><c>SchoolAdmin</c> or <c>Teacher</c>.</summary>
    public string Role { get; set; } = string.Empty;
    /// <summary>JSON array of class ids for Teacher invites.</summary>
    public string ClassIdsJson { get; set; } = "[]";
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}
