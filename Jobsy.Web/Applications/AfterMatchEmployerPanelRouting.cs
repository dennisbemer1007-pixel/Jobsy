using Microsoft.AspNetCore.Http;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerPanelRouting
{
    internal static string ResolvePanel(IQueryCollection query)
        => AfterMatchPanelRouting.ResolvePanel(query);

    internal static string PathSuffixForPanel(string panel)
    {
        panel = panel.Trim().ToLowerInvariant();
        if (panel is "slots")
        {
            panel = "propose";
        }

        return panel switch
        {
            "reschedule" => "/reschedule-ssr",
            "" or "propose" or "messages" or "cancel" or "outcome" => "/panel-ssr",
            _ => "/blazor",
        };
    }
}
