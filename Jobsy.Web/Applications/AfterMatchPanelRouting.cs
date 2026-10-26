using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Web.Applications;

internal static class AfterMatchPanelRouting
{
    internal const string ConfirmedSsrSuffix = "/confirmed-ssr";
    internal const string PickSlotSsrSuffix = "/pick-slot-ssr";
    internal const string MessagesSsrSuffix = "/messages-ssr";
    internal const string RescheduleSsrSuffix = "/reschedule-ssr";
    internal const string CancelledSsrSuffix = "/cancelled-ssr";
    internal const string PanelSsrSuffix = "/panel-ssr";
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
        "pick-slot" or "propose-slot" or "propose" => PickSlotSsrSuffix,
        "messages" => MessagesSsrSuffix,
        "reschedule" => RescheduleSsrSuffix,
        "cancel" => RescheduleSsrSuffix,
        "cancelled" => CancelledSsrSuffix,
        "outcome" or "" => PanelSsrSuffix,
        _ => "/blazor",
    };

    /// <summary>
    /// Server-side dispatch for candidate <c>/after-match</c> without an explicit panel (panel-ssr).
    /// </summary>
    internal static string? DispatchSuffixForThread(AfterMatchThreadDto? thread)
    {
        if (thread is null)
        {
            return null;
        }

        if (AfterMatchSsrRouting.ShouldUseOutcomeScreen(thread))
        {
            return null;
        }

        if (thread.InterviewStatus == InterviewStatus.Scheduled
            && thread.Outcome != AfterMatchOutcomeKind.NotNow)
        {
            return ConfirmedSsrSuffix;
        }

        if (thread.InterviewStatus is InterviewStatus.Proposing or InterviewStatus.RescheduleRequested
            && thread.OpenSlots.Count > 0)
        {
            return PickSlotSsrSuffix;
        }

        return null;
    }
}
