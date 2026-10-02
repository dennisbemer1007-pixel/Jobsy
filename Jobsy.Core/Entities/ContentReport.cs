using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// One report of a vacancy or a company page (DSA art. 16/17, public-pages 06).
/// Data minimisation: an optional reporter e-mail, never an IP address. Retention:
/// <see cref="Privacy.PrivacyConstants.ContentReportRetentionDays"/> after the decision, with the
/// e-mail cleared <see cref="Privacy.PrivacyConstants.ContentReportEmailRetentionDays"/> days earlier.
/// </summary>
public class ContentReport
{
    public Guid Id { get; set; }

    public ContentReportTargetType TargetType { get; set; }

    /// <summary>Vacancy id, or the company id the KVK number resolved to. Always server-resolved.</summary>
    public Guid TargetId { get; set; }

    /// <summary>KVK number of a reported company page, so the admin list can link to <c>/{kvk}</c>.</summary>
    public string? TargetKvk { get; set; }

    /// <summary>Title of the vacancy or name of the company at the moment of the report (admin list).</summary>
    public string? TargetLabel { get; set; }

    public ContentReportReason Reason { get; set; }

    /// <summary>Plain text, trimmed to 1000 characters.</summary>
    public string? Details { get; set; }

    /// <summary>Optional, normalized. Only used to tell the reporter what we decided.</summary>
    public string? ReporterEmail { get; set; }

    /// <summary>Filled when the reporter was signed in.</summary>
    public Guid? ReporterUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ContentReportStatus Status { get; set; } = ContentReportStatus.Open;

    /// <summary>Why the admin decided this, max 1000 characters. Required for Restricted/Removed.</summary>
    public string? DecisionReason { get; set; }

    public DateTime? DecidedAtUtc { get; set; }

    public Guid? DecidedByUserId { get; set; }

    /// <summary>Set once the reporter e-mail has been dropped by the retention job.</summary>
    public DateTime? ReporterEmailClearedAtUtc { get; set; }
}
