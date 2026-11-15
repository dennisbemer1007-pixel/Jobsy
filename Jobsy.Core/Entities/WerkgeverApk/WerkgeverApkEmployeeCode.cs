using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities.WerkgeverApk;

/// <summary>
/// Pseudonymous employee login code. Lobsy never stores an employee name on the code row.
/// </summary>
public class WerkgeverApkEmployeeCode
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public WerkgeverApkTeam? Team { get; set; }

    /// <summary>1..n for the printed code list order.</summary>
    public int Number { get; set; }

    /// <summary>Manager-only nickname (never shown on the employee session).</summary>
    public string? Label { get; set; }

    /// <summary>Official ESCO occupation id for functie comparison (manager-assigned, no codes in UI).</summary>
    public string? EscoOccupationId { get; set; }

    /// <summary>HMAC-SHA256 of the normalized code (lookup key).</summary>
    public string CodeLookupHash { get; set; } = string.Empty;

    /// <summary>Data Protection payload for reprint.</summary>
    public string CodeProtected { get; set; } = string.Empty;

    public WerkgeverApkEmployeeCodeStatus Status { get; set; } = WerkgeverApkEmployeeCodeStatus.NotStarted;
    public int SessionVersion { get; set; }

    public bool SharedWithManager { get; set; }
    public DateTime? SharedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>Optional link to an existing Lobsy user after voluntary connect.</summary>
    public Guid? LinkedUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }

    public WerkgeverApkEmployeeProgress? Progress { get; set; }
    public WerkgeverApkManagerSheet? ManagerSheet { get; set; }
}
