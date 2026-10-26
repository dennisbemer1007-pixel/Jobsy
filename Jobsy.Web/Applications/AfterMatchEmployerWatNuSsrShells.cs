using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerWatNuSsrShells
{
    internal sealed record PageData(
        AfterMatchThreadDto Thread,
        string ProposeHref,
        string MessagesHref,
        string ApplicationDetailHref,
        string? StatusNote);

    internal static string BuildBody(CultureState culture, PageData data, Guid applicationId)
    {
        var panelData = new AfterMatchEmployerPanelSsrShells.PageData(
            data.Thread,
            data.ApplicationDetailHref,
            data.StatusNote,
            AfterMatchEmployerDemoSlots.InitialUtc(),
            InterviewFormat.OnSite,
            "Loosduinseweg 120, Den Haag · ingang naast de winkel",
            "Linda de Wit, eigenaar",
            "Je hoeft niets voor te bereiden. Kom gerust in je gewone kleren.",
            "",
            null);
        return AfterMatchEmployerPanelSsrShells.BuildBody(culture, panelData, applicationId);
    }
}
