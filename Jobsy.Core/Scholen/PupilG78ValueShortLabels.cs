namespace Jobsy.Core.Scholen;

/// <summary>Short kid-language labels from G78 value items (9031–9045).</summary>
public static class PupilG78ValueShortLabels
{
    public static string? ForItem(int itemId)
        => itemId switch
        {
            9031 or 9036 or 9041 or 9046 or 9052 or 9058 => "Zelf kiezen",
            9032 or 9037 or 9042 or 9047 or 9053 or 9059 => "Samen met anderen",
            9033 or 9038 or 9043 => "Heel goed worden",
            9034 or 9039 or 9044 or 9055 => "Rust en duidelijkheid",
            9035 or 9040 or 9045 or 9060 => "Iets betekenen voor anderen",
            9048 => "Dat iedereen zich welkom voelt",
            9054 => "Dat iedereen zich goed voelt in de groep",
            _ => null
        };

    public static string? FromClause(string? clauseText)
    {
        if (string.IsNullOrWhiteSpace(clauseText))
        {
            return null;
        }

        var t = clauseText.ToLowerInvariant();
        if (t.Contains("vriend", StringComparison.Ordinal) || t.Contains("mensen horen", StringComparison.Ordinal))
        {
            return "Samen met anderen";
        }

        if (t.Contains("zelf", StringComparison.Ordinal) && t.Contains("kiez", StringComparison.Ordinal))
        {
            return "Zelf kiezen";
        }

        if (t.Contains("goed word", StringComparison.Ordinal) || t.Contains("trots", StringComparison.Ordinal))
        {
            return "Heel goed worden";
        }

        if (t.Contains("rust", StringComparison.Ordinal) || t.Contains("duidelijk", StringComparison.Ordinal))
        {
            return "Rust en duidelijkheid";
        }

        if (t.Contains("beter", StringComparison.Ordinal) && t.Contains("wereld", StringComparison.Ordinal))
        {
            return "Iets goeds voor iedereen";
        }

        return null;
    }
}
