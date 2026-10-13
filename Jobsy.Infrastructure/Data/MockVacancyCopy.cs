using System.Text.RegularExpressions;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Rewrites leftover demo sentences in vacancy descriptions that were seeded before the copy change.
/// Safe to run more than once.
/// </summary>
public static partial class MockVacancyCopy
{
    public const string Replacement = "Je kunt direct solliciteren. We reageren doorgaans binnen één werkdag.";

    [GeneratedRegex(
        @"Solliciteer via Jobsy[^.\r\n]{0,180}\.(?:\s*\((?:Mock vacaturetekst|Haaglanden testdata|testdata)[^)]*\))?",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OldSentence();

    public static string Rewrite(string? description)
    {
        if (string.IsNullOrEmpty(description)
            || !description.Contains("Solliciteer via Jobsy", StringComparison.OrdinalIgnoreCase))
        {
            return description ?? "";
        }

        return OldSentence().Replace(description, Replacement).Trim();
    }
}
