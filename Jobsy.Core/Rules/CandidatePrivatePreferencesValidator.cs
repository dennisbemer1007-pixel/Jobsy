namespace Jobsy.Core.Rules;

/// <summary>
/// Soft validation for private dislike chips. Unknown codes dropped; custom text trimmed/capped.
/// </summary>
public static class CandidatePrivatePreferencesValidator
{
    public static (IReadOnlyList<string> Dislikes, IReadOnlyList<string> CustomDislikes) Sanitize(
        IEnumerable<string>? dislikes,
        IEnumerable<string>? customDislikes,
        Action<string>? onDropped = null)
    {
        var codes = SanitizeCodes(dislikes, onDropped);
        var custom = SanitizeCustom(customDislikes, onDropped);
        return (codes, custom);
    }

    public static IReadOnlyList<string> SanitizeCodes(IEnumerable<string>? raw, Action<string>? onDropped = null)
    {
        if (raw is null)
        {
            return [];
        }

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in raw)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                continue;
            }

            var canonical = DiscoveryCatalogs.CanonicalDislike(item);
            if (canonical is null)
            {
                onDropped?.Invoke($"Dislike:{item}");
                continue;
            }

            if (seen.Add(canonical))
            {
                result.Add(canonical);
            }
        }

        return result;
    }

    public static IReadOnlyList<string> SanitizeCustom(IEnumerable<string>? raw, Action<string>? onDropped = null)
    {
        if (raw is null)
        {
            return [];
        }

        var result = new List<string>(DiscoveryCatalogs.MaxCustomDislikes);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in raw)
        {
            if (result.Count >= DiscoveryCatalogs.MaxCustomDislikes)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(item))
            {
                continue;
            }

            var trimmed = item.Trim();
            if (trimmed.Length > DiscoveryCatalogs.MaxCustomDislikeLength)
            {
                trimmed = trimmed[..DiscoveryCatalogs.MaxCustomDislikeLength].TrimEnd();
            }

            if (trimmed.Length == 0 || !seen.Add(trimmed))
            {
                continue;
            }

            if (DiscoveryCatalogs.IsKnownDislike(trimmed))
            {
                onDropped?.Invoke($"CustomDislike:catalog:{trimmed}");
                continue;
            }

            result.Add(trimmed);
        }

        return result;
    }
}
