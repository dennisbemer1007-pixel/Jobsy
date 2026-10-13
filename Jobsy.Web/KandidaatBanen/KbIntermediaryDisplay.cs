namespace Jobsy.Web.KandidaatBanen;

using Jobsy.Core.Rules.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;

/// <summary>Candidate-facing uitzendbureau / hidden-mode display helpers (Dep A fallback).</summary>
public static class KbIntermediaryDisplay
{
    public static bool IsHiddenMode(VacancyListItem vacancy) =>
        KbHiddenIntermediaryMask.IsHidden(vacancy.IntermediaryCompanyId, vacancy.ShowClientAddressOnMap);

    public static bool IsHiddenMode(Guid? intermediaryCompanyId, bool showClientAddressOnMap) =>
        KbHiddenIntermediaryMask.IsHidden(intermediaryCompanyId, showClientAddressOnMap);

    /// <summary>
    /// Company line on cards/detail: "via uitzendbureau {bureau}" in hidden mode, else the public name
    /// (or legacy OfferedByLabel when the workplace is open).
    /// </summary>
    public static string CompanyLine(CultureState culture, VacancyListItem vacancy)
    {
        if (IsHiddenMode(vacancy))
        {
            return string.Format(culture["Kb.Via.Bureau"], vacancy.CompanyName);
        }

        return string.IsNullOrWhiteSpace(vacancy.OfferedByLabel)
            ? vacancy.CompanyName
            : vacancy.OfferedByLabel;
    }

    public static bool HideRouteAndStreetView(VacancyListItem vacancy) =>
        KbHiddenIntermediaryMask.HideRouteAndStreetView(
            vacancy.IntermediaryCompanyId,
            vacancy.ShowClientAddressOnMap);
}
