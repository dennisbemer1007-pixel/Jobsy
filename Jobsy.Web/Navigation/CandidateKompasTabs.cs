namespace Jobsy.Web.Navigation;

/// <summary>
/// Tab ids for Mijn Lobsy Kompas (home + candidate profile).
/// Query <c>?tab=</c> and legacy hashes map onto these values.
/// </summary>
public static class CandidateKompasTabs
{
    public const string Profile = "profile";
    public const string Competencies = "competencies";
    public const string Career = "career";
    public const string Fit = "fit";

    public static readonly string[] All = [Profile, Competencies, Career, Fit];

    public static string Normalize(string? raw)
    {
        var value = (raw ?? "").Trim().TrimStart('#').ToLowerInvariant();
        var slash = value.LastIndexOf('/');
        if (slash >= 0 && slash < value.Length - 1)
        {
            value = value[(slash + 1)..];
        }

        return value switch
        {
            Profile or "profiel" or "mijn-profiel" or "mijn profiel" or "criteria"
                or "kompas-criteria-title" or "kompas-panel-profile" or "kompas-tab-profile"
                => Profile,
            Competencies or "competences" or "competenties" or "competence" or "competency"
                or "mijn-competenties" or "mijn competenties"
                or "competency-profile-title" or "kompas-competence-title"
                or "kompas-panel-competencies" or "kompas-tab-competencies"
                => Competencies,
            Career or "careers" or "beroepen" or "beroep" or "mijn-beroepen" or "mijn beroepen"
                or "occupations" or "career-profile-title" or "kompas-career-title"
                or "kompas-panel-career" or "kompas-tab-career"
                => Career,
            Fit or "role-fit" or "past-dit" or "past dit bij mij" or "functie-fit"
                or "kompas-panel-fit" or "kompas-tab-fit"
                => Fit,
            _ => Profile
        };
    }

    public static string Neighbor(string current, int delta)
    {
        var index = Array.IndexOf(All, Normalize(current));
        if (index < 0)
        {
            index = 0;
        }

        var next = (index + delta) % All.Length;
        if (next < 0)
        {
            next += All.Length;
        }

        return All[next];
    }
}
