namespace Jobsy.Core.Rules;

/// <summary>Split display names for mail greetings (first name only).</summary>
public static class NameParts
{
    public static string? FirstName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return null;
        }

        var trimmed = fullName.Trim();
        var space = trimmed.IndexOf(' ');
        return space < 0 ? trimmed : trimmed[..space].Trim();
    }
}
