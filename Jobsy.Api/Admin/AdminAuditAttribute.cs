namespace Jobsy.Api.Admin;

/// <summary>
/// Marks an admin write action for automatic audit-log persistence by <see cref="AdminAuditFilter"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AdminAuditAttribute : Attribute
{
    public AdminAuditAttribute(string action)
    {
        Action = action;
    }

    public string Action { get; }
    public string TargetType { get; set; } = "user";
    /// <summary>Route value key for TargetId (e.g. "userId", "id").</summary>
    public string? TargetRouteKey { get; set; }
}

/// <summary>
/// Exempts a non-GET admin-reachable action from the reflection guard.
/// Provide a short reason (tested / documented).
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AdminAuditExemptAttribute : Attribute
{
    public AdminAuditExemptAttribute(string reason)
    {
        Reason = reason;
    }

    public string Reason { get; }
}
