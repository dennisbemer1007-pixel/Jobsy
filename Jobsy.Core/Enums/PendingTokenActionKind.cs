namespace Jobsy.Core.Enums;

/// <summary>Vacancy product action deferred until a prepaid Mollie top-up completes.</summary>
public enum PendingTokenActionKind
{
    Publish = 1,
    Highlight = 2,
    PushBom = 3,
    Extend = 4
}

public enum PendingTokenActionStatus
{
    Pending = 0,
    Executed = 1,
    Failed = 2,
    Cancelled = 3,
    /// <summary>Claimed by a worker; intermediate between Pending and Executed/Failed.</summary>
    Executing = 4
}
