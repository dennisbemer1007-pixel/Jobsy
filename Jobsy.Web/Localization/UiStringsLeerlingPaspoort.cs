namespace Jobsy.Web.Localization;

public static class UiStringsLeerlingPaspoort
{
    public static void MergeNl(IDictionary<string, string> nl)
    {
        ArgumentNullException.ThrowIfNull(nl);
        foreach (var (key, value) in Jobsy.Core.Scholen.PupilPassport.PupilPassportCopy.All)
        {
            nl[key] = value;
        }
    }

    public static void MergeAll(
        IDictionary<string, string> nl,
        IDictionary<string, string> en,
        IDictionary<string, string> pl,
        IDictionary<string, string> ro,
        IDictionary<string, string> ar)
    {
        MergeNl(nl);
        foreach (var (key, nlValue) in Jobsy.Core.Scholen.PupilPassport.PupilPassportCopy.All)
        {
            en[key] = En(key, nlValue);
            pl[key] = Pl(key, nlValue);
            ro[key] = Ro(key, nlValue);
            ar[key] = Ar(key, nlValue);
        }
    }

    private static string En(string key, string nl) => key switch
    {
        "LeerlingPassport.Header.G78" => "MY DNA PASSPORT",
        "LeerlingPassport.Header.Vo" => "DNA passport · secondary school",
        "LeerlingPassport.Vo.Form.Title" => "Extra for your DNA passport",
        "LeerlingPassport.Vo.Form.Lead" => "Optional · shown on your PDF when filled in",
        "LeerlingPassport.Vo.Form.Save" => "Save and continue",
        "LeerlingPassport.Vo.Form.Skip" => "Skip",
        _ when key.Contains(".Vo.", StringComparison.Ordinal) && nl.Length < 120 => TranslateLite(nl, "en"),
        _ => nl
    };

    private static string Pl(string key, string nl) => key.StartsWith("LeerlingPassport.", StringComparison.Ordinal)
        ? TranslateLite(nl, "pl")
        : nl;

    private static string Ro(string key, string nl) => key.StartsWith("LeerlingPassport.", StringComparison.Ordinal)
        ? TranslateLite(nl, "ro")
        : nl;

    private static string Ar(string key, string nl) => key.StartsWith("LeerlingPassport.", StringComparison.Ordinal)
        ? TranslateLite(nl, "ar")
        : nl;

    /// <summary>Lightweight NL→target phrasing for passport strings (UI + PDF when locale switches later).</summary>
    private static string TranslateLite(string nl, string lang)
    {
        if (lang == "en")
        {
            return nl
                .Replace("Geen vacatures", "No vacancies", StringComparison.OrdinalIgnoreCase)
                .Replace("geen vacatures", "no vacancies", StringComparison.OrdinalIgnoreCase)
                .Replace("Leukste vakken", "Favourite subjects", StringComparison.OrdinalIgnoreCase)
                .Replace("Bijbaan", "Part-time job", StringComparison.OrdinalIgnoreCase)
                .Replace("Vervoer", "Transport", StringComparison.OrdinalIgnoreCase)
                .Replace("Optioneel", "Optional", StringComparison.OrdinalIgnoreCase)
                .Replace("Opslaan", "Save", StringComparison.OrdinalIgnoreCase)
                .Replace("Overslaan", "Skip", StringComparison.OrdinalIgnoreCase);
        }

        if (lang == "pl")
        {
            return nl
                .Replace("Leukste vakken", "Ulubione przedmioty", StringComparison.OrdinalIgnoreCase)
                .Replace("Bijbaan", "Praca dorywcza", StringComparison.OrdinalIgnoreCase)
                .Replace("Vervoer", "Dojazd", StringComparison.OrdinalIgnoreCase)
                .Replace("Optioneel", "Opcjonalnie", StringComparison.OrdinalIgnoreCase);
        }

        if (lang == "ro")
        {
            return nl
                .Replace("Leukste vakken", "Materii preferate", StringComparison.OrdinalIgnoreCase)
                .Replace("Bijbaan", "Job part-time", StringComparison.OrdinalIgnoreCase)
                .Replace("Vervoer", "Transport", StringComparison.OrdinalIgnoreCase)
                .Replace("Optioneel", "Opțional", StringComparison.OrdinalIgnoreCase);
        }

        return nl
            .Replace("Leukste vakken", "المواد المفضلة", StringComparison.OrdinalIgnoreCase)
            .Replace("Bijbaan", "عمل بدوام جزئي", StringComparison.OrdinalIgnoreCase)
            .Replace("Vervoer", "المواصلات", StringComparison.OrdinalIgnoreCase)
            .Replace("Optioneel", "اختياري", StringComparison.OrdinalIgnoreCase);
    }
}
