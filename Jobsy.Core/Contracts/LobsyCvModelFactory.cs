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
        string? phoneNumber,
        bool whatsAppContactAllowed,
        CandidatePreferencesDto preferences,
        double? latitude,
        double? longitude,
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

        var flexible = preferences.FlexibleTimes == true;
        var slots = NormalizeSlots(preferences.Availability);

        return new LobsyCvModel(
            FullName: fullName,
            Email: email,
            PhoneNumber: phoneNumber,
            WhatsAppContactAllowed: whatsAppContactAllowed,
            City: ExtractCity(preferences.HomeAddress),
            Address: preferences.HomeAddress,
            Latitude: latitude,
            Longitude: longitude,
            AboutMe: preferences.AboutMe,
            Motivation: motivation,
            PreferredTransport: preferences.PreferredTransport,
            MaxTravelMinutes: preferences.MaxTravelMinutes,
            EstimatedTravelMinutes: estimatedTravelMinutes,
            MinHoursPerWeek: preferences.MinHoursPerWeek,
            MaxHoursPerWeek: preferences.MaxHoursPerWeek,
            FlexibleTimes: flexible,
            AvailabilitySummary: FormatAvailability(slots, flexible),
            AvailabilitySlots: slots,
            DrivingLicenses: preferences.DrivingLicenses?.ToList() ?? [],
            Educations: preferences.Educations?.ToList() ?? [],
            Employers: employers,
            MatchPercent: matchPercent,
            VacancyTitle: vacancyTitle,
            CompanyName: companyName,
            GeneratedAtUtc: generatedAtUtc,
            ConsentVersion: consentVersion ?? PrivacyConstants.CurrentConsentVersion,
            IncludeFullAddress: true,
            IncludeContactDetails: true);
    }

    public static LobsyCvModel FromApplicationSnapshot(
        string fullName,
        string? email,
        string? phoneNumber,
        bool whatsAppContactAllowed,
        string? city,
        string? address,
        double? latitude,
        double? longitude,
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
        bool includeContactDetails)
    {
        var licenses = SplitCsv(drivingLicensesCsv);
        var educations = SplitCsv(educationsCsv);
        var availability = ParseAvailabilityPayload(availabilityJson);

        var about = aboutMe;
        if (employerCount > 0)
        {
            var note = $"{employerCount} eerdere werkgever{(employerCount == 1 ? "" : "s")}";
            about = string.IsNullOrWhiteSpace(about) ? note : $"{about}\n\n{note}";
        }

        return new LobsyCvModel(
            FullName: fullName,
            Email: email,
            PhoneNumber: phoneNumber,
            WhatsAppContactAllowed: whatsAppContactAllowed,
            City: city,
            Address: address,
            Latitude: latitude,
            Longitude: longitude,
            AboutMe: about,
            Motivation: motivation,
            PreferredTransport: preferredTransport,
            MaxTravelMinutes: null,
            EstimatedTravelMinutes: estimatedTravelMinutes,
            MinHoursPerWeek: availability.MinHours,
            MaxHoursPerWeek: availability.MaxHours,
            FlexibleTimes: availability.FlexibleTimes,
            AvailabilitySummary: FormatAvailability(availability.Slots, availability.FlexibleTimes),
            AvailabilitySlots: availability.Slots,
            DrivingLicenses: licenses,
            Educations: educations,
            Employers: [],
            MatchPercent: matchPercent,
            VacancyTitle: vacancyTitle,
            CompanyName: companyName,
            GeneratedAtUtc: generatedAtUtc,
            ConsentVersion: consentVersion ?? PrivacyConstants.CurrentConsentVersion,
            IncludeFullAddress: includeFullAddress,
            IncludeContactDetails: includeContactDetails);
    }

    public static string SerializeAvailabilitySnapshot(
        CandidatePreferencesDto preferences)
    {
        var slots = NormalizeSlots(preferences.Availability);
        return JsonSerializer.Serialize(new
        {
            flexibleTimes = preferences.FlexibleTimes == true,
            minHoursPerWeek = preferences.MinHoursPerWeek,
            maxHoursPerWeek = preferences.MaxHoursPerWeek,
            slots
        });
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

    public static AvailabilityPayload ParseAvailabilityPayload(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return AvailabilityPayload.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var flexible = root.ValueKind == JsonValueKind.Object
                           && root.TryGetProperty("flexibleTimes", out var flex)
                           && flex.ValueKind == JsonValueKind.True;

            decimal? minHours = null;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("minHoursPerWeek", out var minEl)
                && minEl.ValueKind == JsonValueKind.Number
                && minEl.TryGetDecimal(out var minVal))
            {
                minHours = minVal;
            }

            decimal? maxHours = null;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("maxHoursPerWeek", out var maxEl)
                && maxEl.ValueKind == JsonValueKind.Number
                && maxEl.TryGetDecimal(out var maxVal))
            {
                maxHours = maxVal;
            }

            var slotsEl = root.ValueKind == JsonValueKind.Object
                          && root.TryGetProperty("slots", out var s)
                ? s
                : root;

            var map = ReadSlotsObject(slotsEl);
            return new AvailabilityPayload(flexible, map, minHours, maxHours);
        }
        catch (JsonException)
        {
            return AvailabilityPayload.Empty;
        }
    }

    private static IReadOnlyDictionary<string, string[]>? NormalizeSlots(
        IReadOnlyDictionary<string, string[]>? availability)
    {
        if (availability is null || availability.Count == 0)
        {
            return null;
        }

        var map = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var day in DayPartMatrix.DayCodes)
        {
            if (!availability.TryGetValue(day, out var slots) || slots.Length == 0)
            {
                continue;
            }

            map[day] = slots
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(DayPartMatrix.NormalizeDayPartCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return map.Count == 0 ? null : map;
    }

    private static Dictionary<string, string[]> ReadSlotsObject(JsonElement slotsEl)
    {
        var map = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (slotsEl.ValueKind != JsonValueKind.Object)
        {
            return map;
        }

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
                .Select(x => DayPartMatrix.NormalizeDayPartCode(x!.Trim()))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (parts.Length > 0)
            {
                var day = DayPartMatrix.IsValidDayCode(prop.Name)
                    ? DayPartMatrix.NormalizeDayCode(prop.Name)
                    : prop.Name;
                map[day] = parts;
            }
        }

        return map;
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

    public sealed record AvailabilityPayload(
        bool FlexibleTimes,
        IReadOnlyDictionary<string, string[]> Slots,
        decimal? MinHours,
        decimal? MaxHours)
    {
        public static AvailabilityPayload Empty { get; } =
            new(false, new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase), null, null);
    }
}
