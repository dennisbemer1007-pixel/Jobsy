using Microsoft.AspNetCore.Http;

namespace Jobsy.Web.Applications;

internal static class AfterMatchPanelRouting
{
    /// <summary>
    /// Resolves the after-match panel from the query string. <c>panel=messages</c> always wins when present
    /// (avoids a trailing unencoded <c>return=...?panel=cancel</c> overriding messages).
    /// </summary>
    internal static string ResolvePanel(IQueryCollection query)
    {
        foreach (var value in query["panel"])
        {
            if (string.Equals(value, "messages", StringComparison.OrdinalIgnoreCase))
            {
                return "messages";
            }
        }

        return query["panel"].ToString().Trim().ToLowerInvariant();
    }

    internal static string PathSuffixForPanel(string panel) => panel switch
    {
        "pick-slot" or "propose-slot" => "/pick-slot-ssr",
        "messages" => "/messages-ssr",
        "reschedule" or "cancel" => "/reschedule-ssr",
        "outcome" or "" => "/panel-ssr",
        _ => "/blazor",
    };
}
