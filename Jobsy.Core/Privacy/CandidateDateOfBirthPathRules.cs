namespace Jobsy.Core.Privacy;

/// <summary>Which candidate routes stay reachable before a date of birth is stored.</summary>
public static class CandidateDateOfBirthPathRules
{
    public const string DobCapturePath = "/candidate/geboortedatum";

    /// <summary>Candidate shell routes outside <c>/candidate/*</c> that still need a stored DOB.</summary>
    private static readonly HashSet<string> CandidateLandingPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/home",
        "/profiel",
    };

    public static bool IsExempt(string? path, string? tabQueryValue)
    {
        var p = Normalize(path);
        if (string.IsNullOrEmpty(p))
        {
            return false;
        }

        if (p.Equals(DobCapturePath, StringComparison.OrdinalIgnoreCase)
            || p.Equals("/candidate/settings", StringComparison.OrdinalIgnoreCase)
            || p.Equals("/candidate/privacy", StringComparison.OrdinalIgnoreCase)
            || p.Equals("/logout", StringComparison.OrdinalIgnoreCase)
            || p.Equals("/account/logout", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (p.StartsWith("/privacy", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (p.StartsWith("/candidate/start", StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("/candidate/ontdekkingsreis", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (p.StartsWith("/candidate/paspoort", StringComparison.OrdinalIgnoreCase)
            && string.Equals(tabQueryValue, "data", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public static bool ShouldRedirect(string? path, string? tabQueryValue, bool hasDateOfBirth)
    {
        if (hasDateOfBirth)
        {
            return false;
        }

        var p = Normalize(path);
        if (string.IsNullOrEmpty(p) || IsExempt(path, tabQueryValue))
        {
            return false;
        }

        if (p.StartsWith("/candidate", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return CandidateLandingPaths.Contains(p);
    }

    private static string Normalize(string? path)
    {
        path = (path ?? "").Trim();
        if (path.Length > 1 && path.EndsWith('/'))
        {
            path = path.TrimEnd('/');
        }

        return path;
    }
}
