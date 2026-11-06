using Jobsy.Web.Applications;

namespace Jobsy.Web.Navigation;

/// <summary>
/// Notification deep links often target <c>[NoBlazorRuntime]</c> SSR pages — enhanced nav leaves an empty shell.
/// </summary>
internal static class NotificationNavigation
{
    internal static bool RequiresForceLoad(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var path = url.Split('?', '#')[0];
        if (!path.StartsWith('/'))
        {
            return false;
        }

        if (path.Contains("-ssr", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (AfterMatchWerkgeverSsrPaths.IsCanonicalOrInternalPath(path))
        {
            return true;
        }

        if (path.Contains("/candidate/applications/", StringComparison.OrdinalIgnoreCase)
            && path.Contains("/after-match", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return path.Equals("/candidate/applications", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/candidate/paspoort", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/candidate/liked", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/candidate/kompas", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/candidate/ontdekkingsreis", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("/candidate/start", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/banenkaart", StringComparison.OrdinalIgnoreCase);
    }

    internal static void Go(Microsoft.AspNetCore.Components.NavigationManager navigation, string url)
    {
        navigation.NavigateTo(url, forceLoad: RequiresForceLoad(url));
    }
}
