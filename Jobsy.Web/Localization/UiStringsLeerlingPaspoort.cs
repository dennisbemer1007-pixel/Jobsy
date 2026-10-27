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
        "LeerlingPassport.Brand.Sub" => "Expedition · DNA passport",
        "LeerlingPassport.Kicker" => "Expedition complete",
        "LeerlingPassport.Badge.Title" => "Expedition complete",
        "LeerlingPassport.Badge.Sub" => "Your route is complete",
        "LeerlingPassport.NameIntro" => "This passport belongs to",
        "LeerlingPassport.NamePrivacy" => "Write your name yourself. Lobsy does not store names.",
        "LeerlingPassport.Section.DitBenIk" => "This is you",
        "LeerlingPassport.Section.DitBenIk.Who" => "What Lobsy learned along the way",
        "LeerlingPassport.Section.Droom" => "This is my dream…",
        "LeerlingPassport.Dream.Bubble" => "{0}? A strong choice. This is your route.",
        "LeerlingPassport.Section.Likes" => "I enjoy this",
        "LeerlingPassport.Section.Dislikes" => "Rather not",
        "LeerlingPassport.OwnWord" => "my own word:",
        "LeerlingPassport.Section.Strengths" => "What I am good at",
        "LeerlingPassport.Strengths.Sub" => "how I see myself",
        "LeerlingPassport.Section.Cards" => "My job cards",
        "LeerlingPassport.Cards.Sub" => "to explore · no vacancies",
        "LeerlingPassport.Cards.Fit" => "Fits with",
        "LeerlingPassport.Section.Discover" => "What I still want to explore",
        "LeerlingPassport.Section.Tip" => "Tip for home and class",
        "LeerlingPassport.Route.Phase.Now" => "Now",
        "LeerlingPassport.Route.Phase.Next" => "Next",
        "LeerlingPassport.Route.Phase.Later" => "Later",
        "LeerlingPassport.Route.Phase.Goal" => "Goal",
        "LeerlingPassport.Page.Title" => "My DNA passport",
        "LeerlingPassport.Page.Back" => "Back to This is you",
        "LeerlingPassport.Page.Pdf" => "Download PDF",
        "LeerlingPassport.Page.NamePlaceholder" => "Name (optional, on screen and PDF only)",
        "LeerlingPassport.Footer.G78" =>
            "No grade and no school advice. This passport is for exploring, for you, your parents and your teacher.\nLobsy does not store names. Your answers are deleted after this school year.",
        "LeerlingPassport.Footer.Vo" =>
            "No grade and no school advice. This passport is for exploring.\nLobsy does not store names. Your answers are deleted after this school year.",
        "LeerlingPassport.Card.Blurb.Generic.G78" =>
            "A job to think about. Not a vacancy, but an idea to explore further.",
        "LeerlingPassport.Card.Blurb.Generic.Vo" =>
            "Fits your interests. To explore further, not a vacancy.",
        "LeerlingPassport.Card.Domain.R" => "Outdoors & tech",
        "LeerlingPassport.Card.Domain.I" => "Research",
        "LeerlingPassport.Card.Domain.A" => "Creative",
        "LeerlingPassport.Card.Domain.S" => "Care & animals",
        "LeerlingPassport.Card.Domain.E" => "Business",
        "LeerlingPassport.Card.Domain.C" => "Precise & tidy",
        "LeerlingPassport.Header.Vo" => "DNA passport · secondary school",
        "LeerlingPassport.Vo.Form.Title" => "Extra for your DNA passport",
        "LeerlingPassport.Vo.Form.Lead" => "Optional · shown on your PDF when filled in",
        "LeerlingPassport.Vo.Form.Save" => "Save and continue",
        "LeerlingPassport.Vo.Form.Skip" => "Skip",
        _ when key.StartsWith("LeerlingPassport.Strength.", StringComparison.Ordinal) && key.EndsWith(".Sub", StringComparison.Ordinal)
            => EnStrengthSubtitle(nl),
        _ when key.StartsWith("LeerlingPassport.", StringComparison.Ordinal) && nl.Length < 160 => TranslateLite(nl, "en"),
        _ => nl
    };

    private static string EnStrengthSubtitle(string nl) => nl switch
    {
        var s when s.Contains("luister", StringComparison.OrdinalIgnoreCase) => "I listen and think along",
        var s when s.Contains("netjes", StringComparison.OrdinalIgnoreCase) => "I do things carefully",
        var s when s.Contains("vastzit", StringComparison.OrdinalIgnoreCase) => "I notice when someone is stuck",
        var s when s.Contains("weten hoe", StringComparison.OrdinalIgnoreCase) => "I want to know how things work",
        var s when s.Contains("zelf aan", StringComparison.OrdinalIgnoreCase) => "I like to do things myself",
        var s when s.Contains("doelgericht", StringComparison.OrdinalIgnoreCase) => "I work toward a goal",
        var s when s.Contains("kalm", StringComparison.OrdinalIgnoreCase) => "I stay calm when it is busy",
        var s when s.Contains("nieuwe dingen", StringComparison.OrdinalIgnoreCase) => "I like to come up with new ideas",
        var s when s.Contains("praat graag", StringComparison.OrdinalIgnoreCase) => "I like talking with others",
        var s when s.Contains("samen", StringComparison.OrdinalIgnoreCase) => "I prefer working together",
        var s when s.Contains("duidelijk doel", StringComparison.OrdinalIgnoreCase) => "I like a clear goal",
        var s when s.Contains("betekenen", StringComparison.OrdinalIgnoreCase) => "I want to matter to others",
        var s when s.Contains("rekenen", StringComparison.OrdinalIgnoreCase) => "you can count on me",
        _ => TranslateLite(nl, "en")
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
                .Replace("Overslaan", "Skip", StringComparison.OrdinalIgnoreCase)
                .Replace("Liever niet", "Rather not", StringComparison.OrdinalIgnoreCase)
                .Replace("Dit vind ik leuk", "I enjoy this", StringComparison.OrdinalIgnoreCase)
                .Replace("Geen cijfer", "No grade", StringComparison.OrdinalIgnoreCase)
                .Replace("schooladvies", "school advice", StringComparison.OrdinalIgnoreCase)
                .Replace("Lobsy bewaart geen namen", "Lobsy does not store names", StringComparison.OrdinalIgnoreCase);
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
