using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// Time-limited, reasoned break-glass access for one admin to one subject (prompt 06).
/// </summary>
public class SupportAccessGrant
{
    public Guid Id { get; set; }
    public Guid AdminUserId { get; set; }
    public Guid? SubjectUserId { get; set; }
    public Guid? SubjectCompanyId { get; set; }
    public SupportAccessScope Scope { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? TicketReference { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? RevokedByUserId { get; set; }
}
