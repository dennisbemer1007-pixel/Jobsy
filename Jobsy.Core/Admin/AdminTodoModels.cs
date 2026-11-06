namespace Jobsy.Core.Admin;

public enum AdminTodoSeverity
{
    Info = 0,
    Warn = 1,
    Danger = 2
}

/// <summary>One actionable admin to-do row (dashboard, /admin/te-doen, sidebar counts).</summary>
public sealed record AdminTodoItem(
    string Key,
    AdminTodoSeverity Severity,
    string TitleKey,
    string Subtitle,
    string Area,
    DateTime SinceUtc,
    string ActionLabelKey,
    string Href,
    int Count = 1);

public interface IAdminTodoSource
{
    string Key { get; }

    Task<IReadOnlyList<AdminTodoItem>> GetAsync(CancellationToken cancellationToken = default);
}

public sealed record AdminTodoSnapshot(
    IReadOnlyList<AdminTodoItem> Items,
    IReadOnlyDictionary<string, int> CountsByNavKey);

public interface IAdminTodoService
{
    Task<AdminTodoSnapshot> GetAsync(CancellationToken cancellationToken = default);

    void Invalidate();
}

/// <summary>Nav count keys used by <see cref="AdminTodoSnapshot.CountsByNavKey"/> and AdminNav.CountKey.</summary>
public static class AdminTodoNavKeys
{
    public const string Todo = "todo";
    public const string OrgRequests = "org-requests";
    public const string Moderation = "moderation";
    public const string Payouts = "payouts";
    public const string Feedback = "feedback";
}
