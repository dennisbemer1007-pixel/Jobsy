namespace Jobsy.Web.Help;

/// <summary>Server-rendered tab of <c>/hoe-werkt-lobsy</c> (public-pages 08).</summary>
public enum HowLobsyAudience
{
    You = 0,
    Employers = 1,
    Schools = 2,
}

/// <summary>Reads and writes the <c>?voor=</c> tab of <c>/hoe-werkt-lobsy</c>.</summary>
public static class HowLobsyAudiences
{
    public const string QueryName = "voor";
    public const string BasePath = HowLobsyRoleGuides.SharedPath;

    private const string YouValue = "jou";
    private const string EmployersValue = "werkgevers";
    private const string SchoolsValue = "scholen";

    /// <summary>Unknown values and the employer tab with werkgevers OFF fall back to "Voor jou".</summary>
    public static HowLobsyAudience Parse(string? raw, bool employersEnabled)
    {
        var value = raw?.Trim();
        if (string.Equals(value, SchoolsValue, StringComparison.OrdinalIgnoreCase))
        {
            return HowLobsyAudience.Schools;
        }

        if (employersEnabled && string.Equals(value, EmployersValue, StringComparison.OrdinalIgnoreCase))
        {
            return HowLobsyAudience.Employers;
        }

        return HowLobsyAudience.You;
    }

    public static string Href(HowLobsyAudience audience) => audience switch
    {
        HowLobsyAudience.Employers => $"{BasePath}?{QueryName}={EmployersValue}",
        HowLobsyAudience.Schools => $"{BasePath}?{QueryName}={SchoolsValue}",
        _ => $"{BasePath}?{QueryName}={YouValue}",
    };

    public static IReadOnlyList<HowLobsyAudience> Visible(bool employersEnabled)
        => employersEnabled
            ? [HowLobsyAudience.You, HowLobsyAudience.Employers, HowLobsyAudience.Schools]
            : [HowLobsyAudience.You, HowLobsyAudience.Schools];
}
