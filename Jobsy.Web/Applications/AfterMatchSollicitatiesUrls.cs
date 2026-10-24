using Jobsy.Core.Enums;

namespace Jobsy.Web.Applications;

internal static class AfterMatchSollicitatiesUrls
{
    internal static string AfterMatchBase(Guid applicationId, string? returnUrl = null)
    {
        var url = $"/candidate/applications/{applicationId:D}/after-match";
        return AppendReturn(url, returnUrl);
    }

    internal static string PickSlot(Guid applicationId, string? returnUrl = null)
    {
        var url = $"/candidate/applications/{applicationId:D}/after-match?panel=pick-slot";
        return AppendReturn(url, returnUrl);
    }

    internal static string ProposeSlot(Guid applicationId, string? returnUrl = null)
    {
        var url = $"/candidate/applications/{applicationId:D}/after-match?panel=pick-slot#ndm-propose-form";
        return AppendReturn(url, returnUrl);
    }

    internal static string Messages(Guid applicationId, string? returnUrl = null)
    {
        var url = $"/candidate/applications/{applicationId:D}/after-match?panel=messages";
        return AppendReturn(url, returnUrl);
    }

    internal static string ViewApplicationSsr(Guid applicationId, InterviewStatus status, string? returnUrl = null)
    {
        return status is InterviewStatus.Proposing or InterviewStatus.RescheduleRequested
            ? PickSlot(applicationId, returnUrl)
            : AfterMatchBase(applicationId, returnUrl);
    }

    internal static string Reschedule(Guid applicationId, string? returnUrl = null)
    {
        var url = AfterMatchBase(applicationId) + "?panel=reschedule";
        return AppendReturn(url, returnUrl);
    }

    internal static string Cancel(Guid applicationId, string? returnUrl = null)
    {
        var url = AfterMatchBase(applicationId) + "?panel=cancel";
        return AppendReturn(url, returnUrl);
    }

    private static string AppendReturn(string url, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return url;
        }

        var sep = url.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return url + sep + "return=" + Uri.EscapeDataString(returnUrl);
    }

    internal static string Vacancy(Guid vacancyId)
        => $"/vacancies/{vacancyId:D}";

    internal static string LobsyCvPdf(Guid applicationId)
        => $"/api/applications/{applicationId:D}/lobsy-cv.pdf";
}
