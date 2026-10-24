namespace Jobsy.Web.Applications;

internal static class AfterMatchSollicitatiesUrls
{
    internal static string AfterMatchBase(Guid applicationId)
        => $"/candidate/applications/{applicationId:D}/after-match";

    internal static string PickSlot(Guid applicationId)
        => AfterMatchBase(applicationId);

    internal static string Messages(Guid applicationId)
        => AfterMatchBase(applicationId) + "?panel=messages";

    internal static string Reschedule(Guid applicationId)
        => AfterMatchBase(applicationId) + "?panel=reschedule";

    internal static string Vacancy(Guid vacancyId)
        => $"/vacancies/{vacancyId:D}";

    internal static string LobsyCvPdf(Guid applicationId)
        => $"/api/applications/{applicationId:D}/lobsy-cv.pdf";
}
