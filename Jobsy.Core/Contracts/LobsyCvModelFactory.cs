using System.Text;
using System.Text.Json;
using Jobsy.Core.Entities;
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
        int? matchPercent = null,
        DateOnly? dateOfBirth = null,
        double? workplaceLatitude = null,
        double? workplaceLongitude = null,
        string? workplaceAddress = null,
        double? distanceKm = null,
        bool hasUploadedOwnCv = false)
    {
        // Home lat/lng retained in signature for call-site compatibility; never written to CV.
        _ = latitude;
        _ = longitude;

        var employers = (preferences.Employers ?? Array.Empty<CandidateEmployerHistoryDto>())
            .Where(e => !string.IsNullOrWhiteSpace(e.EmployerName))
            .Select(e => new LobsyCvEmployerEntry(
                e.EmployerName.Trim(),
                string.IsNullOrWhiteSpace(e.Role) ? null : e.Role.Trim(),
                e.Years,
                string.IsNullOrWhiteSpace(e.Description) ? null : e.Description.Trim(),
                NormalizeMonth(e.StartMonth),
                NormalizeMonth(e.EndMonth)))
            .ToList();

        var certificates = (preferences.Certificates ?? Array.Empty<CandidateCertificateDto>())
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .Select(c => new LobsyCvCertificateEntry(c.Name.Trim(), c.Year is >= 1950 and <= 2100 ? c.Year : null))
            .ToList();

        var flexible = preferences.FlexibleTimes == true;
        var slots = NormalizeSlots(preferences.Availability);
        var ageYears = AgeRules.AgeYearsFromDateOfBirth(dateOfBirth);
        var hasWorkplace = workplaceLatitude is not null && workplaceLongitude is not null;
        // Reach circle / "afstand tot werkgever" only when an employer location is known.
        var reachMinutes = hasWorkplace || distanceKm is > 0
            ? (estimatedTravelMinutes ?? preferences.MaxTravelMinutes)
            : null;

        // Candidate home address/coords are NEVER placed on the CV (privacy).
        return new LobsyCvModel(
            FullName: fullName,
            Email: email,
            PhoneNumber: phoneNumber,
            WhatsAppContactAllowed: whatsAppContactAllowed,
            City: null,
            Address: null,
            Latitude: null,
            Longitude: null,
            AboutMe: preferences.AboutMe,
            Motivation: motivation ?? preferences.DefaultMotivation,
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
            Certificates: certificates,
            MatchPercent: matchPercent,
            VacancyTitle: vacancyTitle,
            CompanyName: companyName,
            DateOfBirth: dateOfBirth,
            AgeYears: ageYears,
            GeneratedAtUtc: generatedAtUtc,
            ConsentVersion: consentVersion ?? PrivacyConstants.CurrentConsentVersion,
            IncludeFullAddress: false,
            IncludeContactDetails: true,
            WorkplaceLatitude: workplaceLatitude,
            WorkplaceLongitude: workplaceLongitude,
            WorkplaceAddress: string.IsNullOrWhiteSpace(workplaceAddress) ? null : workplaceAddress.Trim(),
            ReachTravelMinutes: reachMinutes,
            DistanceKm: distanceKm is > 0 ? distanceKm : null,
            HasUploadedOwnCv: hasUploadedOwnCv);
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
        string? certificatesJson,
        int employerCount,
        int? matchPercent,
        string? vacancyTitle,
        string? companyName,
        string? consentVersion,
        DateTime generatedAtUtc,
        bool includeFullAddress,
        bool includeContactDetails,
        DateOnly? dateOfBirth = null,
        int? ageYears = null,
        double? workplaceLatitude = null,
        double? workplaceLongitude = null,
        string? workplaceAddress = null,
        int? maxTravelMinutes = null,
        double? distanceKm = null,
        bool hasUploadedOwnCv = false)
    {
        // Candidate home fields kept for API compatibility; never rendered on CV.
        _ = city;
        _ = address;
        _ = latitude;
        _ = longitude;
        _ = includeFullAddress;

        var licenses = SplitCsv(drivingLicensesCsv);
        var educations = SplitCsv(educationsCsv);
        var availability = ParseAvailabilityPayload(availabilityJson);
        var certificates = ParseCertificatesJson(certificatesJson);

        var about = aboutMe;
        if (employerCount > 0)
        {
            var note = $"{employerCount} eerdere werkgever{(employerCount == 1 ? "" : "s")}";
            about = string.IsNullOrWhiteSpace(about) ? note : $"{about}\n\n{note}";
        }

        var resolvedAge = ageYears ?? AgeRules.AgeYearsFromDateOfBirth(dateOfBirth);
        var reachMinutes = estimatedTravelMinutes > 0
            ? estimatedTravelMinutes
            : maxTravelMinutes;

        return new LobsyCvModel(
            FullName: fullName,
            Email: email,
            PhoneNumber: phoneNumber,
            WhatsAppContactAllowed: whatsAppContactAllowed,
            City: null,
            Address: null,
            Latitude: null,
            Longitude: null,
            AboutMe: about,
            Motivation: motivation,
            PreferredTransport: preferredTransport,
            MaxTravelMinutes: maxTravelMinutes,
            EstimatedTravelMinutes: estimatedTravelMinutes,
            MinHoursPerWeek: availability.MinHours,
            MaxHoursPerWeek: availability.MaxHours,
            FlexibleTimes: availability.FlexibleTimes,
            AvailabilitySummary: FormatAvailability(availability.Slots, availability.FlexibleTimes),
            AvailabilitySlots: availability.Slots,
            DrivingLicenses: licenses,
            Educations: educations,
            Employers: [],
            Certificates: certificates,
            MatchPercent: matchPercent,
            VacancyTitle: vacancyTitle,
            CompanyName: companyName,
            DateOfBirth: dateOfBirth,
            AgeYears: resolvedAge,
            GeneratedAtUtc: generatedAtUtc,
            ConsentVersion: consentVersion ?? PrivacyConstants.CurrentConsentVersion,
            IncludeFullAddress: false,
            IncludeContactDetails: includeContactDetails,
            WorkplaceLatitude: workplaceLatitude,
            WorkplaceLongitude: workplaceLongitude,
            WorkplaceAddress: string.IsNullOrWhiteSpace(workplaceAddress) ? null : workplaceAddress.Trim(),
            ReachTravelMinutes: reachMinutes,
            DistanceKm: distanceKm is > 0 ? distanceKm : null,
            HasUploadedOwnCv: hasUploadedOwnCv);
    }

    /// <summary>
    /// Application snapshot PDF: name/CV after Accept; e-mail/phone only when
    /// <paramref name="includeDirectContact"/> is true (Hired for employers; always for the candidate).
    /// </summary>
    public static LobsyCvModel FromApplicationForDownload(
        Application application,
        bool includePii,
        bool includeDirectContact)
    {
        var vacancy = application.Vacancy
            ?? throw new InvalidOperationException("Application.Vacancy is required to build a Lobsy-CV.");
        var display = IntermediaryVacancyRules.ResolvePublicDisplay(
            vacancy,
            vacancy.Company,
            vacancy.IntermediaryCompany);
        var workplaceLat = display.Latitude != 0 ? display.Latitude : vacancy.Location?.Latitude;
        var workplaceLng = display.Longitude != 0 ? display.Longitude : vacancy.Location?.Longitude;
        var contact = includePii && includeDirectContact;

        return FromApplicationSnapshot(
            application.CandidateName,
            contact ? application.CandidateEmail : null,
            contact ? application.SnapshotPhoneNumber : null,
            contact && application.SnapshotWhatsAppAllowed,
            city: null,
            address: null,
            latitude: null,
            longitude: null,
            application.SnapshotAboutMe,
            application.Motivation,
            application.PreferredTransport,
            application.EstimatedTravelMinutes,
            application.SnapshotAvailabilityJson,
            application.SnapshotDrivingLicenses,
            application.SnapshotEducations,
            application.SnapshotCertificatesJson,
            application.CandidateEmployerCount,
            application.MatchPercent,
            vacancy.Title,
            display.DisplayName,
            application.ConsentVersion,
            DateTime.UtcNow,
            includeFullAddress: false,
            includeContactDetails: contact,
            dateOfBirth: contact ? application.SnapshotDateOfBirth : null,
            ageYears: application.CandidateAgeYears,
            workplaceLatitude: workplaceLat,
            workplaceLongitude: workplaceLng,
            workplaceAddress: display.DisplayAddress,
            maxTravelMinutes: null,
            distanceKm: application.DistanceKm,
            hasUploadedOwnCv: application.HasUploadedCv);
    }

    public static string SerializeCertificatesSnapshot(
        IEnumerable<CandidateCertificateDto>? certificates,
        int maxLength = 4000)
    {
        var items = (certificates ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .Select(c =>
            {
                var name = c.Name.Trim();
                if (name.Length > 200)
                {
                    name = name[..200];
                }

                return new
                {
                    name,
                    year = c.Year is >= 1950 and <= 2100 ? c.Year : null
                };
            })
            .Take(30)
            .ToList();

        // Drop trailing entries until JSON fits the DB column — never mid-string truncate.
        while (true)
        {
            var json = JsonSerializer.Serialize(items);
            if (json.Length <= maxLength || items.Count == 0)
            {
                return json;
            }

            items.RemoveAt(items.Count - 1);
        }
    }

    public static List<LobsyCvCertificateEntry> ParseCertificatesJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var list = new List<LobsyCvCertificateEntry>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var name = item.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
                    ? nameEl.GetString()?.Trim()
                    : null;
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                int? year = null;
                if (item.TryGetProperty("year", out var yearEl)
                    && yearEl.ValueKind == JsonValueKind.Number
                    && yearEl.TryGetInt32(out var y)
                    && y is >= 1950 and <= 2100)
                {
                    year = y;
                }

                list.Add(new LobsyCvCertificateEntry(name, year));
            }

            return list;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string? FormatEmployerPeriod(string? startMonth, string? endMonth, int? yearsFallback = null)
    {
        var start = NormalizeMonth(startMonth);
        var end = NormalizeMonth(endMonth);

        if (start is null && end is null)
        {
            return yearsFallback is > 0 ? $"{yearsFallback} jr" : null;
        }

        var startLabel = start is null ? "—" : FormatMonthLabel(start);
        if (end is null)
        {
            return $"{startLabel} – heden";
        }

        return $"{startLabel} – {FormatMonthLabel(end)}";
    }

    /// <summary>Normalizes yyyy-MM or yyyy-MM-dd to yyyy-MM; returns null when invalid.</summary>
    public static string? NormalizeMonth(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length >= 7
            && trimmed[4] == '-'
            && int.TryParse(trimmed.AsSpan(0, 4), out var year)
            && int.TryParse(trimmed.AsSpan(5, 2), out var month)
            && year is >= 1950 and <= 2100
            && month is >= 1 and <= 12)
        {
            return $"{year:D4}-{month:D2}";
        }

        return null;
    }

    private static string FormatMonthLabel(string yyyyMm)
    {
        var year = yyyyMm[..4];
        var month = int.Parse(yyyyMm.AsSpan(5, 2));
        var label = month switch
        {
            1 => "jan",
            2 => "feb",
            3 => "mrt",
            4 => "apr",
            5 => "mei",
            6 => "jun",
            7 => "jul",
            8 => "aug",
            9 => "sep",
            10 => "okt",
            11 => "nov",
            12 => "dec",
            _ => yyyyMm[5..]
        };
        return $"{label} {year}";
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
