using Jobsy.Web.Components.Candidate.Discovery;

namespace Jobsy.Web.Discovery;

internal static class DiscoverySsrUrls
{
    public const string Path = "/candidate/ontdekkingsreis";

    public static string Overview(string? fase = null, string? error = null)
    {
        var q = new List<string> { "stap=overview" };
        if (!string.IsNullOrWhiteSpace(fase))
        {
            q.Add($"fase={Uri.EscapeDataString(fase.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            q.Add($"error={Uri.EscapeDataString(error)}");
        }

        return $"{Path}?{string.Join("&", q)}";
    }

    public static string Shed(int step) => WithStap(JourneyTestFlow.ShedQuery(step));

    public static string Step(int step, int? diepte = null, int? q = null)
        => WithStap(step.ToString(System.Globalization.CultureInfo.InvariantCulture), diepte, q);

    public static string WithStap(string stap, int? diepte = null, int? q = null, string? fase = null)
    {
        var parts = new List<string> { $"stap={Uri.EscapeDataString(stap)}" };
        if (diepte is int d)
        {
            parts.Add($"diepte={JourneyTestFlow.DepthQueryValue(d)}");
        }

        if (q is int qq and >= 1)
        {
            parts.Add($"q={qq}");
        }

        if (!string.IsNullOrWhiteSpace(fase))
        {
            parts.Add($"fase={Uri.EscapeDataString(fase.Trim())}");
        }

        return $"{Path}?{string.Join("&", parts)}";
    }

    public static string WithError(string url, string message)
        => url.Contains('?', StringComparison.Ordinal)
            ? $"{url}&error={Uri.EscapeDataString(message)}"
            : $"{url}?error={Uri.EscapeDataString(message)}";
}
