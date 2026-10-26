using System.Diagnostics;
using System.Net;

namespace Jobsy.Web.Services;

/// <summary>Trace-only diagnostics for activity log BFF/API (no PII).</summary>
internal static class ActivityLogApiTrace
{
    internal static void LogDisabledOrMissing(HttpStatusCode status)
        => Trace.WriteLine($"job_search_activities status={(int)status} hint=feature_disabled_or_route");

    internal static void LogAuthBlocked(HttpStatusCode status)
        => Trace.WriteLine($"job_search_activities status={(int)status} hint=auth");

    internal static void LogUnavailable(HttpStatusCode status)
        => Trace.WriteLine($"job_search_activities status={(int)status} hint=upstream_error");

    internal static void LogPdfFailed(HttpStatusCode status)
        => Trace.WriteLine($"job_search_activities_pdf status={(int)status}");
}
