using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities.Scholen;

/// <summary>
/// Pseudonymous pupil login code. Lobsy never stores a pupil name.
/// </summary>
public class PupilCode
{
    public Guid Id { get; set; }
    public Guid SchoolClassId { get; set; }
    public SchoolClass? SchoolClass { get; set; }
    /// <summary>1..n for the printed code list order.</summary>
    public int Number { get; set; }
    /// <summary>HMAC-SHA256 of the normalized code (lookup key).</summary>
    public string CodeLookupHash { get; set; } = string.Empty;
    /// <summary>Data Protection payload (purpose Scholen.PupilCode) for reprint.</summary>
    public string CodeProtected { get; set; } = string.Empty;
    public PupilCodeStatus Status { get; set; } = PupilCodeStatus.NotStarted;
    public int SessionVersion { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public PupilProgress? Progress { get; set; }
    public PupilResult? Result { get; set; }
}
