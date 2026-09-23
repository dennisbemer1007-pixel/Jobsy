using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

public enum VacancyBarrierKind
{
    Low = 0,
    High = 1
}

public sealed record VacancyBarrierRequirements(
    VacancyBarrierKind Barrier,
    IReadOnlyList<string> Diplomas,
    IReadOnlyList<string> Certifications,
    int? MinExperienceYears,
    int? MinExperienceHours,
    IReadOnlyList<string> HardChecks)
{
    public IReadOnlyList<string> HardCheckLabels
        => HardChecks.Select(VacancyHardCheckCatalog.Label).ToList();
}

public sealed record VacancyBarrierCheckItem(
    string Key,
    string Label,
    bool Met,
    string Note,
    bool Dealbreaker = false)
{
    public bool IsDealbreaker => Dealbreaker || (!Met && IsHardKey(Key));

    public static bool IsHardKey(string key)
        => key.StartsWith("hard:", StringComparison.Ordinal)
           || key.StartsWith("cert:", StringComparison.Ordinal)
           || key.StartsWith("diploma:", StringComparison.Ordinal)
           || key.StartsWith("license:", StringComparison.Ordinal)
           || key.StartsWith("edu-level:", StringComparison.Ordinal);
}

public sealed record VacancyBarrierCheck(
    VacancyBarrierKind Barrier,
    bool HasFormalRequirements,
    IReadOnlyList<VacancyBarrierCheckItem> Items,
    int MetCount,
    int TotalCount);

/// <summary>
/// Optional formal randvoorwaarden on a vacancy (JSON). Low-barrier roles stay empty.
/// </summary>
public static class VacancyBarrierCatalog
{
    public const int MaxList = 8;
    public const int MaxItemLength = 80;
    public const int HoursPerYear = 1600;

    public static readonly string[] SuggestedCertifications =
    [
        "VCA", "BIG", "BHV", "Vliegbrevet", "HACCP", "Heftruck", "CCV"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static VacancyBarrierRequirements Empty { get; } =
        new(VacancyBarrierKind.Low, [], [], null, null, []);

    public static bool HasFormalRequirements(VacancyBarrierRequirements? req)
        => req is not null
           && (req.Diplomas.Count > 0
               || req.Certifications.Count > 0
               || req.HardChecks.Count > 0
               || req.MinExperienceYears is > 0
               || req.MinExperienceHours is > 0);

    public static bool ShowFormalBlock(VacancyBarrierRequirements? req)
        => req is { Barrier: VacancyBarrierKind.High } || HasFormalRequirements(req);

    public static VacancyBarrierRequirements Normalize(
        VacancyBarrierKind? barrier,
        IEnumerable<string>? diplomas,
        IEnumerable<string>? certifications,
        int? minExperienceYears,
        int? minExperienceHours,
        IEnumerable<string>? hardChecks = null)
    {
        var kind = barrier ?? VacancyBarrierKind.Low;
        var dips = CleanList(diplomas);
        var certs = CleanList(certifications);
        var checks = CleanHardChecks(hardChecks);
        var years = Clamp(minExperienceYears, 40);
        var hours = Clamp(minExperienceHours, 80_000);
        if (kind == VacancyBarrierKind.Low
            && dips.Count == 0
            && certs.Count == 0
            && checks.Count == 0
            && years is null
            && hours is null)
        {
            return Empty;
        }

        return new VacancyBarrierRequirements(kind, dips, certs, years, hours, checks);
    }

    public static string? Serialize(VacancyBarrierRequirements? req)
    {
        var normalized = req ?? Empty;
        if (normalized.Barrier == VacancyBarrierKind.Low && !HasFormalRequirements(normalized))
        {
            return null;
        }

        return JsonSerializer.Serialize(new Dto
        {
            Barrier = normalized.Barrier == VacancyBarrierKind.High ? "high" : "low",
            Diplomas = normalized.Diplomas.ToList(),
            Certifications = normalized.Certifications.ToList(),
            MinExperienceYears = normalized.MinExperienceYears,
            MinExperienceHours = normalized.MinExperienceHours,
            HardChecks = normalized.HardChecks.ToList()
        }, JsonOptions);
    }

    public static VacancyBarrierRequirements Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Trim() is "{}" or "null")
        {
            return Empty;
        }

        try
        {
            var dto = JsonSerializer.Deserialize<Dto>(json, JsonOptions);
            if (dto is null)
            {
                return Empty;
            }

            var kind = string.Equals(dto.Barrier, "high", StringComparison.OrdinalIgnoreCase)
                ? VacancyBarrierKind.High
                : VacancyBarrierKind.Low;
            return Normalize(kind, dto.Diplomas, dto.Certifications, dto.MinExperienceYears, dto.MinExperienceHours, dto.HardChecks);
        }
        catch (JsonException)
        {
            return Empty;
        }
    }

    public static VacancyBarrierCheck Evaluate(
        VacancyBarrierRequirements req,
        CandidatePreferencesDto? prefs,
        string? requiredDrivingLicense = null,
        string? requiredEducationLevel = null)
    {
        var educations = prefs?.Educations ?? [];
        var certs = prefs?.Certificates ?? [];
        var years = CandidateExperienceYears(prefs);
        var hours = years is int y ? y * HoursPerYear : 0;
        var evidence = VacancyHardCheckCatalog.CandidateEvidence.From(prefs);
        var items = new List<VacancyBarrierCheckItem>();

        foreach (var kind in req.HardChecks)
        {
            var met = VacancyHardCheckCatalog.CandidateMeets(kind, evidence);
            var label = VacancyHardCheckCatalog.Label(kind);
            items.Add(new VacancyBarrierCheckItem(
                "hard:" + kind,
                label,
                met,
                met
                    ? $"{label} staat in je profiel."
                    : $"Dealbreaker: {label.ToLowerInvariant()} ontbreekt. Zonder deze harde eis kun je niet starten.",
                Dealbreaker: !met));
        }

        if (!string.IsNullOrWhiteSpace(requiredDrivingLicense))
        {
            var met = DrivingLicenseLabels.CandidateMeetsRequirement(prefs?.DrivingLicenses, requiredDrivingLicense);
            items.Add(new VacancyBarrierCheckItem(
                "license:" + requiredDrivingLicense.Trim(),
                "Rijbewijs: " + requiredDrivingLicense.Trim(),
                met,
                met
                    ? "Het gevraagde rijbewijs staat in je profiel."
                    : $"Dealbreaker: rijbewijs {requiredDrivingLicense.Trim()} ontbreekt.",
                Dealbreaker: !met));
        }

        if (!string.IsNullOrWhiteSpace(requiredEducationLevel))
        {
            var met = EducationLevelLabels.CandidateMeetsRequirement(prefs?.Educations, requiredEducationLevel);
            items.Add(new VacancyBarrierCheckItem(
                "edu-level:" + requiredEducationLevel.Trim(),
                "Opleidingsniveau: " + requiredEducationLevel.Trim(),
                met,
                met
                    ? "Je opleidingsniveau dekt deze eis."
                    : $"Dealbreaker: opleidingsniveau {requiredEducationLevel.Trim()} ontbreekt.",
                Dealbreaker: !met));
        }

        foreach (var diploma in req.Diplomas)
        {
            var met = educations.Any(e => TokenHit(e, diploma));
            items.Add(new VacancyBarrierCheckItem(
                "diploma:" + diploma,
                "Diploma: " + diploma,
                met,
                met
                    ? "Dit diploma staat in je profiel."
                    : "Dealbreaker: diploma " + diploma + " ontbreekt nog. Zonder dit diploma kun je niet starten.",
                Dealbreaker: !met));
        }

        foreach (var cert in req.Certifications)
        {
            var met = certs.Any(c => TokenHit(c.Name, cert));
            items.Add(new VacancyBarrierCheckItem(
                "cert:" + cert,
                "Certificaat: " + cert,
                met,
                met
                    ? "Dit certificaat staat in je profiel."
                    : "Dealbreaker: certificaat " + cert + " ontbreekt. Een verplichte licentie of cursus hoort hierbij.",
                Dealbreaker: !met));
        }

        if (req.MinExperienceYears is int needYears and > 0)
        {
            var met = years >= needYears;
            items.Add(new VacancyBarrierCheckItem(
                "years",
                needYears == 1 ? "Minstens 1 jaar ervaring" : $"Minstens {needYears} jaar ervaring",
                met,
                met
                    ? $"Je profiel telt ongeveer {years} jaar mee."
                    : $"De vacature vraagt {needYears} jaar; jouw profiel komt tot {years}."));
        }

        if (req.MinExperienceHours is int needHours and > 0)
        {
            var met = hours >= needHours;
            items.Add(new VacancyBarrierCheckItem(
                "hours",
                $"{needHours.ToString("N0", CultureInfo.GetCultureInfo("nl-NL"))} uur ervaring",
                met,
                met
                    ? "Je werkervaring dekt dit uur-minimum."
                    : "Je hebt dit aantal uren nog niet op je profiel staan."));
        }

        var metCount = items.Count(i => i.Met);
        return new VacancyBarrierCheck(req.Barrier, HasFormalRequirements(req), items, metCount, items.Count);
    }

    public static int CandidateExperienceYears(CandidatePreferencesDto? prefs)
    {
        if (prefs?.Employers is null || prefs.Employers.Count == 0)
        {
            return 0;
        }

        var sum = 0;
        foreach (var job in prefs.Employers)
        {
            if (job.Years is > 0)
            {
                sum += job.Years.Value;
                continue;
            }

            if (TryParseMonth(job.StartMonth, out var start))
            {
                var end = TryParseMonth(job.EndMonth, out var parsedEnd) ? parsedEnd : DateTime.UtcNow;
                var months = ((end.Year - start.Year) * 12) + end.Month - start.Month;
                if (months > 0)
                {
                    sum += Math.Max(1, (int)Math.Round(months / 12d, MidpointRounding.AwayFromZero));
                }
            }
        }

        return sum;
    }

    public static bool AvailabilityLooksOk(
        CandidatePreferencesDto? prefs,
        decimal? vacancyMinHours,
        decimal? vacancyMaxHours,
        string? requiredLicense,
        string? requiredEducationLevel)
    {
        if (!string.IsNullOrWhiteSpace(requiredLicense)
            && !DrivingLicenseLabels.CandidateMeetsRequirement(prefs?.DrivingLicenses, requiredLicense))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(requiredEducationLevel)
            && !EducationLevelLabels.CandidateMeetsRequirement(prefs?.Educations, requiredEducationLevel))
        {
            return false;
        }

        if (vacancyMinHours is decimal vmin && prefs?.MaxHoursPerWeek is decimal cmax && cmax + 0.05m < vmin)
        {
            return false;
        }

        if (vacancyMaxHours is decimal vmax && prefs?.MinHoursPerWeek is decimal cmin && cmin - 0.05m > vmax)
        {
            return false;
        }

        return true;
    }

    private static bool TokenHit(string? haystack, string needle)
    {
        var foldedHay = CareerOccupationKeys.Fold(haystack ?? "");
        var foldedNeedle = CareerOccupationKeys.Fold(needle);
        if (foldedHay.Length == 0 || foldedNeedle.Length == 0)
        {
            return false;
        }

        return foldedHay.Contains(foldedNeedle, StringComparison.Ordinal);
    }

    private static IReadOnlyList<string> CleanList(IEnumerable<string>? values)
    {
        if (values is null)
        {
            return [];
        }

        return values
            .Select(v => (v ?? "").Trim())
            .Where(v => v.Length >= 2)
            .Select(v => v.Length > MaxItemLength ? v[..MaxItemLength].Trim() : v)
            .Where(v => !v.Contains('@', StringComparison.Ordinal))
            .Where(v => !CareerCompassBuilder.ContainsForbiddenJargon(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxList)
            .ToList();
    }

    private static int? Clamp(int? value, int max)
    {
        if (value is null or <= 0)
        {
            return null;
        }

        return Math.Min(max, value.Value);
    }

    private static bool TryParseMonth(string? value, out DateTime month)
    {
        month = default;
        if (string.IsNullOrWhiteSpace(value) || value.Length < 7)
        {
            return false;
        }

        return DateTime.TryParseExact(
            value[..7],
            "yyyy-MM",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out month);
    }

    private sealed class Dto
    {
        public string? Barrier { get; set; }
        public List<string>? Diplomas { get; set; }
        public List<string>? Certifications { get; set; }
        public int? MinExperienceYears { get; set; }
        public int? MinExperienceHours { get; set; }
        public List<string>? HardChecks { get; set; }
    }

    private static IReadOnlyList<string> CleanHardChecks(IEnumerable<string>? values)
    {
        if (values is null)
        {
            return [];
        }

        return values
            .Select(VacancyHardCheckCatalog.NormalizeKind)
            .Where(v => v is not null)
            .Select(v => v!)
            .Distinct(StringComparer.Ordinal)
            .Take(VacancyHardCheckCatalog.Suggested.Length)
            .ToList();
    }
}
