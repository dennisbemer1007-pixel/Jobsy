using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// City-only workplace label for the candidate applications list.
/// Uses the same public display masking as the banenkaart, then fail-closes
/// when a city cannot be extracted (never return a full street address).
/// </summary>
public static class CandidateApplicationLocation
{
    public static (string CompanyName, string? LocationLabel) ForPublicCard(
        bool hasIntermediary,
        bool showClientAddressOnMap,
        string endClientName,
        string? endClientAddress,
        string? intermediaryName,
        string? intermediaryAddress)
    {
        var vacancy = new Vacancy { ShowClientAddressOnMap = showClientAddressOnMap };
        var client = new Company
        {
            Name = endClientName,
            Address = endClientAddress ?? string.Empty
        };
        Company? intermediary = hasIntermediary
            ? new Company
            {
                Name = intermediaryName ?? string.Empty,
                Address = intermediaryAddress ?? string.Empty
            }
            : null;

        var display = IntermediaryVacancyRules.ResolvePublicDisplay(vacancy, client, intermediary);
        return (display.DisplayName, ToCityOrNull(display.DisplayAddress));
    }

    public static string? ToCityOrNull(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        var city = LobsyCvModelFactory.ExtractCity(address);
        if (string.IsNullOrWhiteSpace(city))
        {
            return null;
        }

        var trimmed = address.Trim();
        var cityTrim = city.Trim();
        if (string.Equals(cityTrim, trimmed, StringComparison.OrdinalIgnoreCase)
            && trimmed.Any(char.IsDigit))
        {
            return null;
        }

        return cityTrim;
    }
}
