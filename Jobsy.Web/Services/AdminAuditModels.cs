namespace Jobsy.Web.Services;

public sealed class AdminAuditItem
{
    public Guid Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public Guid? ActorUserId { get; set; }
    public string ActorRole { get; set; } = "";
    public string ActorKind { get; set; } = "";
    public string ActorDisplayName { get; set; } = "";
    public string Action { get; set; } = "";
    public string TargetType { get; set; } = "";
    public string TargetId { get; set; } = "";
    public string TargetLabel { get; set; } = "";
    public string? Reason { get; set; }
    public string? DetailsJson { get; set; }
    public string Result { get; set; } = "";
    public string CorrelationId { get; set; } = "";
}

public sealed class AdminAuditPage
{
    public List<AdminAuditItem> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public sealed class AdminAuditSummary
{
    public bool MaskingEnabled { get; set; } = true;
    public int AdminAuditRetentionDays { get; set; }
    public int PersonalDataAccessLogRetentionDays { get; set; }
    public int PlatformLogRetentionDays { get; set; }
    public DateTime? LastRetentionRunUtc { get; set; }
    public string? LastRetentionDetailsJson { get; set; }
    public int AdminsWithMfa { get; set; }
    public int AdminsTotal { get; set; }
    public int? FailedAdminLogins24h { get; set; }
}

public sealed class AdminMfaRoleCount
{
    public string Role { get; set; } = "";
    public int Enrolled { get; set; }
    public int NotEnrolled { get; set; }
    public int ViaIdp { get; set; }
    public int Total { get; set; }
}

public sealed class AdminMfaMissing
{
    public Guid UserId { get; set; }
    public string MaskedName { get; set; } = "";
    public string MaskedEmail { get; set; } = "";
    public string Role { get; set; } = "";
    public DateTime? LastActiveUtc { get; set; }
}

public sealed class AdminMfaOverview
{
    public List<AdminMfaRoleCount> ByRole { get; set; } = [];
    public List<AdminMfaMissing> WithoutMfa { get; set; } = [];
    public List<AdminAuditItem> RecentEvents { get; set; } = [];
}
