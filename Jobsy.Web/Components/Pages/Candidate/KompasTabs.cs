namespace Jobsy.Web.Components.Pages.Candidate;

public static class KompasTabs
{
    public const string Profile = "profile";
    public const string Competencies = "competencies";
    public const string Occupations = "occupations";
    public const string Fit = "fit";

    public static readonly (string Id, string LabelKey)[] All =
    [
        (Profile, "Kompas.TabProfile"),
        (Competencies, "Kompas.TabCompetencies"),
        (Occupations, "Kompas.TabCareers"),
        (Fit, "Kompas.TabFit")
    ];

    public static string Normalize(string? tab)
    {
        if (string.IsNullOrWhiteSpace(tab))
        {
            return Profile;
        }

        if (tab.Equals("competencies", StringComparison.OrdinalIgnoreCase)
            || tab.Equals("competence", StringComparison.OrdinalIgnoreCase)
            || tab.Equals("competenties", StringComparison.OrdinalIgnoreCase))
        {
            return Competencies;
        }

        if (tab.Equals("occupations", StringComparison.OrdinalIgnoreCase)
            || tab.Equals("career", StringComparison.OrdinalIgnoreCase)
            || tab.Equals("beroepen", StringComparison.OrdinalIgnoreCase))
        {
            return Occupations;
        }

        if (tab.Equals("fit", StringComparison.OrdinalIgnoreCase)
            || tab.Equals("role-fit", StringComparison.OrdinalIgnoreCase)
            || tab.Equals("past-dit", StringComparison.OrdinalIgnoreCase))
        {
            return Fit;
        }

        return Profile;
    }
}
