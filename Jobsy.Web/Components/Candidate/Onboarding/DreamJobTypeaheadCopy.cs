using System.Globalization;

namespace Jobsy.Web.Components.Candidate.Onboarding;

internal static class DreamJobTypeaheadCopy
{
    private static readonly CultureInfo Nl = CultureInfo.GetCultureInfo("nl-NL");

    public static string DisplayTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "";
        }

        var trimmed = title.Trim();
        return Nl.TextInfo.ToTitleCase(trimmed.ToLower(Nl));
    }

    public static string? DisplayDescription(string? description, int maxChars = 60)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();
        if (trimmed.Length <= maxChars)
        {
            return trimmed;
        }

        if (maxChars <= 3)
        {
            return trimmed[..maxChars];
        }

        return trimmed[..(maxChars - 1)].TrimEnd() + "…";
    }
}
