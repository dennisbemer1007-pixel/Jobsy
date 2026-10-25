using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerProposeSsrShells
{
    internal sealed record PageData(
        AfterMatchThreadDto Thread,
        string CancelHref,
        string ApplicationDetailHref,
        IReadOnlyList<DateTime> InitialSelectedUtc,
        InterviewFormat InitialFormat,
        string InitialLocation,
        string InitialWithWhom,
        string InitialNote);

    internal static string BuildBody(CultureState culture, PageData data, Guid applicationId)
    {
        var panelData = new AfterMatchEmployerPanelSsrShells.PageData(
            data.Thread,
            data.ApplicationDetailHref,
            null,
            data.InitialSelectedUtc,
            data.InitialFormat,
            data.InitialLocation,
            data.InitialWithWhom,
            data.InitialNote,
            "propose");
        return AfterMatchEmployerPanelSsrShells.BuildBody(culture, panelData, applicationId);
    }
}
