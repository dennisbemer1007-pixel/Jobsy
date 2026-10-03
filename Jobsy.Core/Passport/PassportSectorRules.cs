using System.Security.Cryptography;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Passport;

public static class PassportSectorCatalog
{
    public static readonly string[] Codes =
    [
        "zorg", "onderwijs", "horeca", "logistiek", "techniek", "groen",
        "retail", "it", "admin", "lab", "dieren", "luchtvaart"
    ];

    public static bool IsKnown(string? code)
        => Codes.Contains(code?.Trim() ?? "", StringComparer.OrdinalIgnoreCase);

    public static string Label(string code) => code.ToLowerInvariant() switch
    {
        "groen" => "Groen & (glas)tuinbouw",
        "zorg" => "Zorg",
        "onderwijs" => "Onderwijs",
        "horeca" => "Horeca",
        "logistiek" => "Logistiek",
        "techniek" => "Techniek",
        "retail" => "Retail",
        "it" => "IT",
        "admin" => "Administratie",
        "lab" => "Laboratorium",
        "dieren" => "Dieren",
        "luchtvaart" => "Luchtvaart",
        _ => code
    };
}

public sealed record PassportSectorChoice(
    string Code,
    IReadOnlyList<string> ReasonCodes,
    string? OwnReason,
    IReadOnlyList<string> ExampleRoles);

public static class PassportSectorSuggestions
{
    public const int MaxSectors = 3;
    public const int MaxSuggestions = 5;
    public const int MaxReasons = 3;
    public const int MaxExampleRoles = 3;
    public const int MaxOwnReasonLength = 80;

    public static IReadOnlyList<string> Suggest(IEnumerable<string?> titles)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var title in titles)
        {
            var node = OccupationTaxonomy.Resolve(title);
            if (node is null || !PassportSectorCatalog.IsKnown(node.Sector))
            {
                continue;
            }

            counts.TryGetValue(node.Sector, out var n);
            counts[node.Sector] = n + 1;
        }

        return counts
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Take(MaxSuggestions)
            .Select(pair => pair.Key)
            .ToList();
    }

    public static IReadOnlyList<PassportSectorReason> ReasonsFor(
        string sector,
        IEnumerable<CandidateEmployerHistoryDto>? employers,
        IEnumerable<CandidateCertificateDto>? certificates,
        SharedWorkPreferences? work)
    {
        var reasons = new List<PassportSectorReason>();
        foreach (var job in employers ?? [])
        {
            var node = OccupationTaxonomy.Resolve(job.Role);
            if (node is null || !string.Equals(node.Sector, sector, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var years = job.Years is int y && y > 0 ? y.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
            if (years is null)
            {
                continue;
            }

            reasons.Add(new PassportSectorReason($"experience-{years}", node.Title));
        }

        foreach (var certificate in certificates ?? [])
        {
            if (CertificateMatches(sector, certificate.Name))
            {
                var slug = Slug(certificate.Name);
                reasons.Add(new PassportSectorReason($"certificate-{slug}", certificate.Name));
            }
        }

        if (HasPref(work?.Indoor))
        {
            reasons.Add(new PassportSectorReason("pref-indoor", "Binnen"));
        }

        if (HasPref(work?.Outdoor))
        {
            reasons.Add(new PassportSectorReason("pref-outdoor", "Buiten"));
        }

        if (HasPref(work?.PhysicalWork))
        {
            reasons.Add(new PassportSectorReason("pref-physical", "Lichamelijk werk"));
        }

        return reasons
            .Where(reason => !reason.Code.StartsWith("holland-", StringComparison.OrdinalIgnoreCase)
                             && !reason.Code.StartsWith("strength-", StringComparison.OrdinalIgnoreCase))
            .DistinctBy(reason => reason.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<PassportSectorChoice> Sanitize(IEnumerable<PassportSectorChoice>? raw)
    {
        var result = new List<PassportSectorChoice>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in raw ?? [])
        {
            if (result.Count >= MaxSectors || !PassportSectorCatalog.IsKnown(item.Code) || !seen.Add(item.Code))
            {
                continue;
            }

            var reasons = (item.ReasonCodes ?? [])
                .Where(code => !string.IsNullOrWhiteSpace(code)
                               && !code.StartsWith("holland-", StringComparison.OrdinalIgnoreCase)
                               && !code.StartsWith("strength-", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxReasons)
                .ToList();
            var own = string.IsNullOrWhiteSpace(item.OwnReason) ? null : item.OwnReason.Trim();
            if (own is { Length: > MaxOwnReasonLength })
            {
                own = own[..MaxOwnReasonLength];
            }

            var roles = (item.ExampleRoles ?? [])
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxExampleRoles)
                .ToList();
            result.Add(new PassportSectorChoice(item.Code.ToLowerInvariant(), reasons, own, roles));
        }

        return result;
    }

    private static bool CertificateMatches(string sector, string? name)
    {
        var text = (name ?? "").ToLowerInvariant();
        if (text.Contains("bhv", StringComparison.Ordinal))
        {
            return true;
        }

        if (text.Contains("haccp", StringComparison.Ordinal))
        {
            return sector.Equals("horeca", StringComparison.OrdinalIgnoreCase);
        }

        var logistics = text.Contains("vca", StringComparison.Ordinal)
                        || text.Contains("heftruck", StringComparison.Ordinal)
                        || text.Contains("reachtruck", StringComparison.Ordinal);
        return logistics && sector is "logistiek" or "techniek";
    }

    private static bool HasPref(string? value)
        => !string.IsNullOrWhiteSpace(value);

    private static string Slug(string name)
    {
        var chars = name.Trim().ToLowerInvariant().Where(ch => char.IsLetterOrDigit(ch) || ch is ' ' or '-').ToArray();
        return new string(chars).Replace(' ', '-');
    }
}

public sealed record PassportSectorReason(string Code, string Label);

public static class PassportCvConfirmation
{
    public static IReadOnlyList<string> Unconfirmed(string? filledFieldsJson, string? confirmedFieldsJson)
    {
        var filled = Read(filledFieldsJson);
        var confirmed = Read(confirmedFieldsJson).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return filled.Where(key => !confirmed.Contains(key)).ToList();
    }

    public static string Confirm(string? confirmedFieldsJson, string key)
    {
        var keys = Read(confirmedFieldsJson).ToList();
        if (!keys.Contains(key, StringComparer.OrdinalIgnoreCase))
        {
            keys.Add(key);
        }

        return System.Text.Json.JsonSerializer.Serialize(keys);
    }

    public static string? ResetFilled(string? confirmedFieldsJson, string? newFilledFieldsJson)
    {
        var filled = Read(newFilledFieldsJson).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var kept = Read(confirmedFieldsJson).Where(key => !filled.Contains(key)).ToList();
        return kept.Count == 0 ? null : System.Text.Json.JsonSerializer.Serialize(kept);
    }

    public static bool IsDropped(string? filledFieldsJson, string? confirmedFieldsJson, string key)
        => Unconfirmed(filledFieldsJson, confirmedFieldsJson).Contains(key, StringComparer.OrdinalIgnoreCase);

    private static List<string> Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }
}

public static class PassportShiftRules
{
    public const string Yes = "ja";
    public const string Consult = "in overleg";
    public const string No = "nee";

    public static string Derive(string dayPart, IReadOnlyDictionary<string, string[]>? availability, bool flexibleTimes)
    {
        if (flexibleTimes)
        {
            return Consult;
        }

        var days = 0;
        foreach (var slot in availability ?? new Dictionary<string, string[]>())
        {
            if (slot.Value.Any(part => string.Equals(part, dayPart, StringComparison.OrdinalIgnoreCase)))
            {
                days++;
            }
        }

        return days switch
        {
            >= 2 => Yes,
            1 => Consult,
            _ => No
        };
    }
}

public static class PassportCompletenessRules
{
    public static bool IsShareReady(
        bool hasHours,
        bool hasAvailability,
        bool hasLanguage,
        bool hasDutchLevel,
        bool hasTransport,
        bool hasWorkRegion,
        int completedTests)
        => hasHours && hasAvailability && hasLanguage && hasDutchLevel && hasTransport && hasWorkRegion && completedTests >= 1;

    public static bool IsShareReady(CandidatePreferencesDto prefs, int completedTests)
    {
        var hasHours = prefs.MinHoursPerWeek is not null || prefs.MaxHoursPerWeek is not null;
        var hasAvailability = prefs.FlexibleTimes == true
                              || (prefs.Availability?.Any(slot => slot.Value is { Length: > 0 }) ?? false);
        var hasLanguage = prefs.SpokenLanguages is { Count: > 0 };
        var hasDutch = !string.IsNullOrWhiteSpace(prefs.DutchLevel);
        var hasTransport = !string.IsNullOrWhiteSpace(prefs.PreferredTransport)
                           || prefs.DrivingLicenses is { Count: > 0 };
        var hasRegion = !string.IsNullOrWhiteSpace(prefs.WorkRegion);
        return IsShareReady(hasHours, hasAvailability, hasLanguage, hasDutch, hasTransport, hasRegion, completedTests);
    }
}

public static class PassportVerificationRules
{
    public const string BadgeText = "Lobsy-geverifieerd";

    public static PassportVerification Evaluate(
        IReadOnlyList<DateTime?> testCompletedAtUtc,
        DateTime? emailVerifiedAtUtc,
        DateTime? phoneVerifiedAtUtc,
        bool phoneVerificationEnabled)
    {
        var dates = (testCompletedAtUtc ?? []).Take(4).ToList();
        while (dates.Count < 4)
        {
            dates.Add(null);
        }

        var done = dates.Count(date => date is not null);
        var phoneRequired = phoneVerificationEnabled;
        var phoneOk = !phoneRequired || phoneVerifiedAtUtc is not null;
        var verified = done == 4 && emailVerifiedAtUtc is not null && phoneOk;
        DateTime? last = dates.Where(date => date is not null).Select(date => date!.Value).DefaultIfEmpty().Max();
        if (done == 0)
        {
            last = null;
        }

        return new PassportVerification(verified, dates, emailVerifiedAtUtc is not null, phoneVerifiedAtUtc is not null, phoneRequired, last);
    }
}

public sealed record PassportVerification(
    bool IsVerified,
    IReadOnlyList<DateTime?> TestDates,
    bool EmailVerified,
    bool PhoneVerified,
    bool PhoneRequired,
    DateTime? LastTestAtUtc);

public static class PassportTranslationRules
{
    public static string Hash(string source)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(source ?? ""));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static bool CanUse(string source, string? storedHash, DateTime? approvedAtUtc, string? translatedText)
        => approvedAtUtc is not null
           && !string.IsNullOrWhiteSpace(translatedText)
           && string.Equals(storedHash, Hash(source), StringComparison.Ordinal);
}

public static class PassportPublicId
{
    public const int Length = 12;

    public static string Create()
    {
        var alphabet = ShortCodeFormat.Alphabet.ToCharArray();
        Span<char> chars = stackalloc char[Length];
        RandomNumberGenerator.GetItems(alphabet, chars);
        return new string(chars);
    }

    public static string Display(string id)
    {
        if (id.Length != Length || id.Any(ch => ShortCodeFormat.Alphabet.IndexOf(ch) < 0))
        {
            throw new ArgumentException("PublicId is ongeldig.", nameof(id));
        }

        return $"{id[..4]}-{id[4..8]}-{id[8..]}";
    }
}
