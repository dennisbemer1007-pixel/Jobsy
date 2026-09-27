namespace Jobsy.Web.Services;

/// <summary>Raised after anonymous DNA answers were merged into the profile (§6.3).</summary>
public static class GratisDnaMergeNotifier
{
    public static event Action? Merged;

    public static void NotifyMerged() => Merged?.Invoke();
}
