using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;

namespace Jobsy.Core.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }

    /// <summary>
    /// Primary / home company. Required for BranchManager; optional home org for other employer roles.
    /// Null for Admin and Candidate.
    /// </summary>
    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }

    public DateOnly? DateOfBirth { get; set; }
    public bool OpenForWork { get; set; }

    /// <summary>
    /// Candidate home location for PushBom radius matching (PostGIS). Null until set.
    /// </summary>
    public GeoPoint? HomeLocation { get; set; }

    public string? PreferencesJson { get; set; }
    public bool IsEarlyAdapter { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>When the user accepted terms/privacy (registration or profile).</summary>
    public DateTime? TermsAcceptedAt { get; set; }
    public string? ConsentVersion { get; set; }

    /// <summary>
    /// When the candidate completed (or dismissed) the "Hoe werkt Lobsy" onboarding page.
    /// </summary>
    public DateTime? CandidateHowToCompletedAt { get; set; }

    /// <summary>Last successful login (local or external). Null = never logged in before.</summary>
    public DateTime? LastLoginAtUtc { get; set; }

    /// <summary>
    /// Extra company memberships for RegionalManager, EnterpriseManager and Intermediary.
    /// </summary>
    public ICollection<UserCompany> CompanyMemberships { get; set; } = new List<UserCompany>();
}
