namespace Jobsy.Core.Enums;

/// <summary>What a visitor reported (DSA notice and action, public-pages 06).</summary>
public enum ContentReportTargetType
{
    Vacancy = 1,
    Company = 2
}

/// <summary>Why the visitor reported it. The labels are B1 Dutch in <c>UiStringsPublicInfo</c>.</summary>
public enum ContentReportReason
{
    Fake = 1,
    Discriminating = 2,
    Illegal = 3,
    WrongInfo = 4,
    Unsafe = 5,
    Other = 6
}

/// <summary>Admin decision on a report. Everything but <see cref="Open"/> is decided.</summary>
public enum ContentReportStatus
{
    Open = 0,
    NoAction = 1,
    Restricted = 2,
    Removed = 3
}
