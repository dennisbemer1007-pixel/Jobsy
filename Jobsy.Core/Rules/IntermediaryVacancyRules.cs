using Jobsy.Core.Entities;
using Jobsy.Core.Rules.KandidaatBanen;

namespace Jobsy.Core.Rules;

/// <summary>
/// Intermediary vacancy rules: mandatory end-client KVK/establishment and address display.
/// </summary>
public static class IntermediaryVacancyRules
{
    /// <summary>
    /// For Intermediary role: end-client company must have KVK number + establishment id.
    /// </summary>
    public static string? ValidateEndClientKvk(Company? endClient, bool callerIsIntermediary)
    {
        if (!callerIsIntermediary)
        {
            return null;
        }

        if (endClient is null)
        {
            return "Selecteer het inhuurende bedrijf (KVK + vestiging).";
        }

        if (string.IsNullOrWhiteSpace(endClient.KvkNumber))
        {
            return "KVK-nummer van het inhuurende bedrijf is verplicht voor intermediairs.";
        }

        if (string.IsNullOrWhiteSpace(endClient.KvkEstablishmentId))
        {
            return "Vestiging (KVK-vestigingsnummer) van het inhuurende bedrijf is verplicht voor intermediairs.";
        }

        return null;
    }

    /// <summary>
    /// Public map/list display: masked intermediary identity vs open end-client identity.
    /// Always keep end-client <see cref="Vacancy.CompanyId"/> for admin / travel / SROI.
    /// When <see cref="Vacancy.ShowClientAddressOnMap"/> is false, show intermediary name/address
    /// and pin the <b>bureau</b> vestiging (D3 / KB-FALLBACK(A) — never the workplace).
    /// </summary>
    public static (
        string DisplayName,
        string DisplayAddress,
        string? DisplayLogoUrl,
        double Latitude,
        double Longitude,
        string? OfferedByLabel) ResolvePublicDisplay(
        Vacancy vacancy,
        Company? endClient,
        Company? intermediary)
    {
        endClient ??= vacancy.Company;

        // Pin for open / non-intermediary vacancies follows the vacancy workplace when set.
        var workplaceLat = vacancy.Location?.Latitude;
        var workplaceLng = vacancy.Location?.Longitude;

        if (intermediary is not null && KbHiddenIntermediaryMask.IsHidden(vacancy))
        {
            // D3: pin / travel use the bureau. No fallback to workplace or end-client coords.
            // OfferedByLabel is null — candidate UI formats Kb.Via.Bureau from DisplayName.
            var bureauLoc = KbHiddenIntermediaryMask.ResolveBureauLocation(intermediary);
            return (
                intermediary.Name,
                intermediary.Address,
                intermediary.LogoUrl,
                bureauLoc?.Latitude ?? 0,
                bureauLoc?.Longitude ?? 0,
                OfferedByLabel: null);
        }

        var offeredBy = intermediary is not null
            ? $"Aangeboden door {intermediary.Name}"
            : null;

        return (
            endClient?.Name ?? "Onbekend bedrijf",
            endClient?.Address ?? string.Empty,
            endClient?.LogoUrl,
            workplaceLat ?? endClient?.Location?.Latitude ?? 0,
            workplaceLng ?? endClient?.Location?.Longitude ?? 0,
            offeredBy);
    }

    /// <summary>
    /// Intermediaries may only place vacancies as type Uitzendbureau.
    /// Client-supplied category ids are ignored when <paramref name="isIntermediary"/> is true.
    /// </summary>
    public static Guid? ResolveCategoryId(bool isIntermediary, Guid? requestedCategoryId)
        => isIntermediary ? VacancyCategoryDefaults.UitzendbureauId : requestedCategoryId;
}
