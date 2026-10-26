using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.KandidaatBanen;

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

    internal static string Outcome(Guid applicationId, string? returnUrl = null)
    {
        var url = $"/candidate/applications/{applicationId:D}/after-match?panel=outcome";
        return AppendReturn(url, returnUrl);
    }

    internal static string ViewApplicationFromMessages(Guid applicationId, AfterMatchThreadDto thread)
    {
        var returnUrl = Messages(applicationId);
        var openSlots = thread.OpenSlots?.Count ?? 0;
        if (thread.InterviewStatus is InterviewStatus.Proposing or InterviewStatus.RescheduleRequested
            && openSlots > 0)
        {
            return PickSlot(applicationId, returnUrl);
        }

        if (thread.InterviewStatus == InterviewStatus.Scheduled)
        {
            return AfterMatchBase(applicationId, returnUrl);
        }

        if (thread.InterviewStatus == InterviewStatus.Done
            || thread.Outcome == AfterMatchOutcomeKind.NotNow)
        {
            return AfterMatchBase(applicationId, returnUrl);
        }

        var detail = ApplicationsSsrQuery.BuildUrl(
            ApplicationsSsrQuery.TabSollicitaties,
            ApplicationsSsrQuery.FilterAll,
            applicationId,
            null);
        return AppendReturn(detail, returnUrl);
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
        => CandidateVacancyUrls.BanenkaartDetail(vacancyId);

    internal static string LobsyCvPdf(Guid applicationId)
        => $"/api/applications/{applicationId:D}/lobsy-cv.pdf";
}
