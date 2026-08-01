using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

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
    /// Public map/list display: masked (intermediary) vs open (end client).
    /// Always keep end-client <see cref="Vacancy.CompanyId"/> for admin / travel / SROI.
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
        var offeredBy = intermediary is not null
            ? $"Aangeboden door {intermediary.Name}"
            : null;

        if (intermediary is not null && !vacancy.ShowClientAddressOnMap)
        {
            return (
                intermediary.Name,
                intermediary.Address,
                intermediary.LogoUrl,
                intermediary.Location?.Latitude ?? endClient?.Location?.Latitude ?? 0,
                intermediary.Location?.Longitude ?? endClient?.Location?.Longitude ?? 0,
                offeredBy);
        }

        return (
            endClient?.Name ?? "Onbekend bedrijf",
            endClient?.Address ?? string.Empty,
            endClient?.LogoUrl,
            vacancy.Location?.Latitude ?? endClient?.Location?.Latitude ?? 0,
            vacancy.Location?.Longitude ?? endClient?.Location?.Longitude ?? 0,
            offeredBy);
    }

    public static bool IsIntermediaryRole(UserRole role) => role == UserRole.Intermediary;
}
