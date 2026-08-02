namespace Jobsy.Web.Dashboard;

/// <summary>
/// Row health for intermediary Bedrijvenoverzicht badges.
/// Priority: action required (red) → low tokens (orange) → healthy (green).
/// </summary>
public enum ClientPerformanceBadge
{
    Healthy = 0,
    LowTokens = 1,
    ActionRequired = 2
}

public static class ClientPerformanceStatus
{
    public static ClientPerformanceBadge Resolve(
        int applicationsPending,
        int expiringWithin5Days,
        decimal tokenBalance,
        decimal lowTokenThreshold = MetricDashboardCatalog.LowTokenBalanceThreshold)
    {
        if (applicationsPending > 0 || expiringWithin5Days > 0)
        {
            return ClientPerformanceBadge.ActionRequired;
        }

        if (tokenBalance < lowTokenThreshold)
        {
            return ClientPerformanceBadge.LowTokens;
        }

        return ClientPerformanceBadge.Healthy;
    }

    public static string CssModifier(ClientPerformanceBadge badge) => badge switch
    {
        ClientPerformanceBadge.ActionRequired => "danger",
        ClientPerformanceBadge.LowTokens => "warn",
        _ => "ok"
    };

    public static string LabelNl(ClientPerformanceBadge badge) => badge switch
    {
        ClientPerformanceBadge.ActionRequired => "Actie nodig",
        ClientPerformanceBadge.LowTokens => "Laag saldo",
        _ => "Actief"
    };
}
