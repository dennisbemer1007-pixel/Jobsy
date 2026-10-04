using System.Text.RegularExpressions;

namespace Jobsy.Web.Admin;

public static partial class SupportCodeDisplay
{
    [GeneratedRegex(@"LB-[2-9A-HJKMNP-TV-Z]{4}", RegexOptions.CultureInvariant)]
    private static partial Regex Shape();

    public static string? Find(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var match = Shape().Match(message);
        return match.Success ? match.Value : null;
    }
}
