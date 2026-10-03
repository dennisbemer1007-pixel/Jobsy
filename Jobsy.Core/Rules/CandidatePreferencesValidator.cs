using Jobsy.Core.Contracts;
using Jobsy.Core.Passport;

namespace Jobsy.Core.Rules;

/// <summary>
/// Soft validation for discovery profile fields in PreferencesJson.
/// Unknown codes are dropped (logged via callback); never hard-fails a save so old clients keep working.
/// </summary>
public static class CandidatePreferencesValidator
{
    public static CandidatePreferencesDto Sanitize(
        CandidatePreferencesDto prefs,
        Action<string>? onDropped = null)
    {
        var spoken = SanitizeLanguages(prefs.SpokenLanguages, onDropped);
        var dutch = SanitizeDutchLevel(prefs.DutchLevel, onDropped);
        var employers = SanitizeCodeList(
            prefs.EmployerPreferences,
            DiscoveryCatalogs.IsKnownEmployerPreference,
            DiscoveryCatalogs.CanonicalEmployer,
            max: DiscoveryCatalogs.EmployerPreferenceCodes.Length,
            label: "EmployerPreferences",
            onDropped);
        var goals = SanitizeFreeTextList(
            prefs.LearningGoals,
            DiscoveryCatalogs.MaxLearningGoals,
            DiscoveryCatalogs.MaxLearningGoalLength,
            truncate: true,
            onDropped,
            "LearningGoals");
        var hobbies = SanitizeHobbies(prefs.Hobbies, onDropped);

        return prefs with
        {
            SpokenLanguages = spoken,
            DutchLevel = dutch,
            EmployerPreferences = employers,
            LearningGoals = goals,
            Hobbies = hobbies,
            PassportSectors = PassportSectorSuggestions.Sanitize(prefs.PassportSectors)
        };
    }

    public static IReadOnlyList<CandidateLanguageDto>? SanitizeLanguages(
        IReadOnlyList<CandidateLanguageDto>? raw,
        Action<string>? onDropped = null)
    {
        if (raw is null || raw.Count == 0)
        {
            return raw is null ? null : [];
        }

        var result = new List<CandidateLanguageDto>(Math.Min(raw.Count, DiscoveryCatalogs.MaxSpokenLanguages));
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in raw)
        {
            if (result.Count >= DiscoveryCatalogs.MaxSpokenLanguages)
            {
                break;
            }

            var code = DiscoveryCatalogs.CanonicalLanguage(item.Code);
            if (code is null)
            {
                onDropped?.Invoke($"SpokenLanguages:{item.Code}");
                continue;
            }

            if (!seen.Add(code))
            {
                continue;
            }

            string? level = null;
            if (!string.IsNullOrWhiteSpace(item.Level))
            {
                level = DiscoveryCatalogs.CanonicalDutch(item.Level);
                if (level is null)
                {
                    onDropped?.Invoke($"SpokenLanguages.Level:{item.Level}");
                }
            }

            result.Add(new CandidateLanguageDto(code, level));
        }

        return result;
    }

    public static string? SanitizeDutchLevel(string? raw, Action<string>? onDropped = null)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var canonical = DiscoveryCatalogs.CanonicalDutch(raw);
        if (canonical is null)
        {
            onDropped?.Invoke($"DutchLevel:{raw}");
        }

        return canonical;
    }

    public static IReadOnlyList<string>? SanitizeHobbies(
        IReadOnlyList<string>? raw,
        Action<string>? onDropped = null)
    {
        // Catalog + free-text hobbies are kept (truncated); callback reserved for API parity with other sanitizers.
        _ = onDropped;
        if (raw is null || raw.Count == 0)
        {
            return raw is null ? null : [];
        }

        var result = new List<string>(Math.Min(raw.Count, DiscoveryCatalogs.MaxHobbies));
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in raw)
        {
            if (result.Count >= DiscoveryCatalogs.MaxHobbies)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(item))
            {
                continue;
            }

            var trimmed = item.Trim();
            var catalog = DiscoveryCatalogs.CanonicalHobby(trimmed);
            if (catalog is not null)
            {
                if (seen.Add(catalog))
                {
                    result.Add(catalog);
                }

                continue;
            }

            if (trimmed.Length > DiscoveryCatalogs.MaxHobbyFreeTextLength)
            {
                trimmed = trimmed[..DiscoveryCatalogs.MaxHobbyFreeTextLength].TrimEnd();
            }

            if (trimmed.Length == 0 || !seen.Add(trimmed))
            {
                continue;
            }

            result.Add(trimmed);
        }

        return result;
    }

    private static List<string>? SanitizeCodeList(
        IReadOnlyList<string>? raw,
        Func<string?, bool> isKnown,
        Func<string?, string?> canonical,
        int max,
        string label,
        Action<string>? onDropped)
    {
        if (raw is null || raw.Count == 0)
        {
            return raw is null ? null : [];
        }

        var result = new List<string>(Math.Min(raw.Count, max));
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in raw)
        {
            if (result.Count >= max)
            {
                break;
            }

            if (!isKnown(item))
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    onDropped?.Invoke($"{label}:{item}");
                }

                continue;
            }

            var code = canonical(item)!;
            if (seen.Add(code))
            {
                result.Add(code);
            }
        }

        return result;
    }

    private static List<string>? SanitizeFreeTextList(
        IReadOnlyList<string>? raw,
        int maxItems,
        int maxLength,
        bool truncate,
        Action<string>? onDropped,
        string label)
    {
        if (raw is null || raw.Count == 0)
        {
            return raw is null ? null : [];
        }

        var result = new List<string>(Math.Min(raw.Count, maxItems));
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in raw)
        {
            if (result.Count >= maxItems)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(item))
            {
                continue;
            }

            var trimmed = item.Trim();
            if (trimmed.Length > maxLength)
            {
                if (!truncate)
                {
                    onDropped?.Invoke($"{label}:overlong");
                    continue;
                }

                trimmed = trimmed[..maxLength].TrimEnd();
            }

            if (trimmed.Length == 0 || !seen.Add(trimmed))
            {
                continue;
            }

            result.Add(trimmed);
        }

        return result;
    }
}
