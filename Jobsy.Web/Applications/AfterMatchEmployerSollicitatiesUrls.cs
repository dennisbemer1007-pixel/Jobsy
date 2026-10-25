namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerSollicitatiesUrls
{
    internal static string AfterMatchBase(Guid applicationId)
        => $"/werkgever/sollicitaties/{applicationId:D}/after-match";

    internal static string Messages(Guid applicationId)
        => $"{AfterMatchBase(applicationId)}?panel=messages";

    internal static string MessagesThread(Guid contextApplicationId, Guid threadApplicationId)
        => $"{AfterMatchBase(contextApplicationId)}?panel=messages&thread={threadApplicationId:D}";

    internal static string Reschedule(Guid applicationId)
        => $"{AfterMatchBase(applicationId)}?panel=reschedule";

    internal static string ApplicationDetail(Guid applicationId)
        => $"/werkgever/sollicitaties/{applicationId:D}";
}
