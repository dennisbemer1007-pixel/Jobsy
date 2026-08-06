using System.Text;
using System.Text.Json;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Contracts;

public static class LobsyCvModelFactory
{
    public static LobsyCvModel FromLiveProfile(
        string fullName,
        string? email,
        CandidatePreferencesDto preferences,
        DateTime generatedAtUtc,
        string? consentVersion = null,
        string? motivation = null,
        string? vacancyTitle = null,
        string? companyName = null,
        int? estimatedTravelMinutes = null,
        int? matchPercent = null)
    {
        var employers = (preferences.Employers ?? Array.Empty<CandidateEmployerHistoryDto>())
            .Select(e => new LobsyCvEmployerEntry(e.EmployerName, e.Role, e.Years, e.Description))
            .ToList();

        return new LobsyCvModel(
            FullName: fullName,
            Email: email,
            City: ExtractCity(preferences.HomeAddress),
            Address: preferences.HomeAddress,
            AboutMe: preferences.AboutMe,
            Motivation: motivation,
            PreferredTransport: preferences.PreferredTransport,
            MaxTravelMinutes: preferences.MaxTravelMinutes,
            EstimatedTravelMinutes: estimatedTravelMinutes,
            MinHoursPerWeek: preferences.MinHoursPerWeek,
            MaxHoursPerWeek: preferences.MaxHoursPerWeek,
            FlexibleTimes: preferences.FlexibleTimes == true,
            AvailabilitySummary: FormatAvailability(preferences.Availability, preferences.FlexibleTimes == true),
            DrivingLicenses: preferences.DrivingLicenses?.ToList() ?? [],
            Educations: preferences.Educations?.ToList() ?? [],
            Employers: employers,
            MatchPercent: matchPercent,
            VacancyTitle: vacancyTitle,
            CompanyName: companyName,
            GeneratedAtUtc: generatedAtUtc,
            ConsentVersion: consentVersion ?? PrivacyConstants.CurrentConsentVersion,
            IncludeFullAddress: true,
            IncludeContactEmail: true);
    }

    public static LobsyCvModel FromApplicationSnapshot(
        string fullName,
        string? email,
        string? city,
        string? address,
        string? aboutMe,
        string? motivation,
        string preferredTransport,
        int estimatedTravelMinutes,
        string? availabilityJson,
        string? drivingLicensesCsv,
        string? educationsCsv,
        int employerCount,
        int? matchPercent,
        string? vacancyTitle,
        string? companyName,
        string? consentVersion,
        DateTime generatedAtUtc,
        bool includeFullAddress,
        bool includeContactEmail)
    {
        var licenses = SplitCsv(drivingLicensesCsv);
        var educations = SplitCsv(educationsCsv);

        var about = aboutMe;
        if (employerCount > 0)
        {
            var note = $"{employerCount} eerdere werkgever{(employerCount == 1 ? "" : "s")}";
            about = string.IsNullOrWhiteSpace(about) ? note : $"{about}\n\n{note}";
        }

        return new LobsyCvModel(
            FullName: fullName,
            Email: email,
            City: city,
            Address: address,
            AboutMe: about,
            Motivation: motivation,
            PreferredTransport: preferredTransport,
            MaxTravelMinutes: null,
            EstimatedTravelMinutes: estimatedTravelMinutes,
            MinHoursPerWeek: null,
            MaxHoursPerWeek: null,
            FlexibleTimes: false,
            AvailabilitySummary: FormatAvailabilityJson(availabilityJson),
            DrivingLicenses: licenses,
            Educations: educations,
            Employers: [],
            MatchPercent: matchPercent,
            VacancyTitle: vacancyTitle,
            CompanyName: companyName,
            GeneratedAtUtc: generatedAtUtc,
            ConsentVersion: consentVersion ?? PrivacyConstants.CurrentConsentVersion,
            IncludeFullAddress: includeFullAddress,
            IncludeContactEmail: includeContactEmail);
    }

    public static string? ExtractCity(string? homeAddress)
    {
        if (string.IsNullOrWhiteSpace(homeAddress))
        {
            return null;
        }

        var parts = homeAddress.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        // Dutch addresses often end with "1234 AB City" or "City".
        var last = parts[^1];
        var tokens = last.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length >= 3
            && tokens[0].Length == 4
            && tokens[0].All(char.IsDigit)
            && tokens[1].Length is >= 1 and <= 4
            && tokens[1].All(char.IsLetter))
        {
            return string.Join(' ', tokens.Skip(2));
        }

        if (tokens.Length >= 2 && tokens[0].Length >= 4 && char.IsDigit(tokens[0][0]))
        {
            return string.Join(' ', tokens.Skip(1));
        }

        return last;
    }

    public static string? FormatAvailability(
        IReadOnlyDictionary<string, string[]>? availability,
        bool flexibleTimes)
    {
        if (flexibleTimes)
        {
            return "Tijden in overleg";
        }

        if (availability is null || availability.Count == 0)
        {
            return null;
        }

        var sb = new StringBuilder();
        foreach (var day in DayPartMatrix.DayCodes)
        {
            if (!availability.TryGetValue(day, out var slots) || slots.Length == 0)
            {
                continue;
            }

            if (sb.Length > 0)
            {
                sb.Append("; ");
            }

            sb.Append(day).Append(": ").Append(string.Join(", ", slots));
        }

        return sb.Length == 0 ? null : sb.ToString();
    }

    public static string? FormatAvailabilityJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("flexibleTimes", out var flex)
                && flex.ValueKind == JsonValueKind.True)
            {
                return "Tijden in overleg";
            }

            var slotsEl = root.ValueKind == JsonValueKind.Object
                          && root.TryGetProperty("slots", out var s)
                ? s
                : root;

            if (slotsEl.ValueKind != JsonValueKind.Object)
            {
                return json.Length > 200 ? json[..200] : json;
            }

            var map = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in slotsEl.EnumerateObject())
            {
                if (prop.Value.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                var parts = prop.Value.EnumerateArray()
                    .Where(x => x.ValueKind == JsonValueKind.String)
                    .Select(x => x.GetString())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!.Trim())
                    .ToArray();
                if (parts.Length > 0)
                {
                    map[prop.Name] = parts;
                }
            }

            return FormatAvailability(map, flexibleTimes: false) ?? (json.Length > 200 ? json[..200] : json);
        }
        catch (JsonException)
        {
            return json.Length > 200 ? json[..200] : json;
        }
    }

    private static List<string> SplitCsv(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return [];
        }

        return csv.Split([',', ';', '|'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
