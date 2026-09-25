using System.Text;
using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

/// <summary>
/// Matches plan course names against profile certificate names for career-step auto-completion.
/// </summary>
public static class CareerCourseMatcher
{
    private static readonly HashSet<string> FillerWords = new(StringComparer.Ordinal)
    {
        "cursus", "training", "opleiding", "basis", "module", "workshop"
    };

    public static string Normalize(string? value)
    {
        var stripped = CareerStepKey.StripDiacritics(value ?? "");
        var baseNormalized = VacancyTextSearch.Normalize(stripped);
        if (string.IsNullOrWhiteSpace(baseNormalized))
        {
            return "";
        }

        var tokens = baseNormalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => !FillerWords.Contains(t))
            .ToList();
        return string.Join(' ', tokens).Trim();
    }

    /// <summary>
    /// True when a profile certificate covers a plan course (equal after normalize, or containment with min length 4).
    /// </summary>
    public static bool IsOnProfile(string? planCourse, string? certificateName)
    {
        var course = Normalize(planCourse);
        var cert = Normalize(certificateName);
        if (course.Length == 0 || cert.Length == 0)
        {
            return false;
        }

        if (string.Equals(course, cert, StringComparison.Ordinal))
        {
            return true;
        }

        if (course.Length < 4 || cert.Length < 4)
        {
            return false;
        }

        return course.Contains(cert, StringComparison.Ordinal)
               || cert.Contains(course, StringComparison.Ordinal);
    }

    public static bool IsOnProfile(string? planCourse, IEnumerable<string?> certificateNames)
        => certificateNames.Any(c => IsOnProfile(planCourse, c));

    public static int MatchedCourseCount(IReadOnlyList<string> planCourses, IEnumerable<string?> certificateNames)
    {
        var certs = certificateNames.ToList();
        return planCourses.Count(course => IsOnProfile(course, certs));
    }

    public static int StepMatchPercent(IReadOnlyList<string> planCourses, IEnumerable<string?> certificateNames, bool stepCompleted)
    {
        if (stepCompleted)
        {
            return 100;
        }

        if (planCourses.Count == 0)
        {
            return 0;
        }

        var matched = MatchedCourseCount(planCourses, certificateNames);
        return (int)Math.Round(100.0 * matched / planCourses.Count, MidpointRounding.AwayFromZero);
    }
}
