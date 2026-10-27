using Jobsy.Core.Scholen;

namespace Jobsy.Core.Scholen.PupilPassport;

/// <summary>Passport UI/PDF copy by language (mirrors UiStringsLeerlingPaspoort EN map).</summary>
public static class PupilPassportLocalizedCopy
{
    public static string Get(string key, string language)
    {
        if (!PupilVerhaalLocalized.IsEnglish(language))
        {
            return PupilPassportCopy.Get(key);
        }

        return En(key, PupilPassportCopy.Get(key));
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
        _ when key.StartsWith("LeerlingPassport.Tip.", StringComparison.Ordinal) && EnTips.TryGetValue(key, out var tip) => tip,
        _ => nl
    };

    private static readonly Dictionary<string, string> EnTips = new(StringComparer.Ordinal)
    {
        ["LeerlingPassport.Tip.Letter.R.1"] =
            "Look together at something you made or fixed with your hands. What did you enjoy most?",
        ["LeerlingPassport.Tip.Letter.I.1"] =
            "Pick a topic you are curious about and look up one fact together. No test, just exploring.",
        ["LeerlingPassport.Tip.Letter.A.1"] =
            "Draw or make something small at home and say why you chose it. It does not have to be perfect.",
        ["LeerlingPassport.Tip.Letter.S.1"] =
            "Talk at home about times you helped someone. What did you do exactly?",
        ["LeerlingPassport.Tip.Letter.E.1"] =
            "Think of a mini plan together: what would you organise at school or home? Who helps?",
        ["LeerlingPassport.Tip.Letter.C.1"] =
            "Sort something messy (books, stuff) and say what you like about working neatly.",
        ["LeerlingPassport.Tip.dierenarts.1"] =
            "Visit a petting zoo or animal shelter together and ask what the carers do.",
        ["LeerlingPassport.Tip.dierenarts.2"] =
            "Ask what they liked most. There is no right or wrong answer.",
        ["LeerlingPassport.Tip.Generic.2"] = "There is no right or wrong answer; it is about exploring.",
    };

    public static IReadOnlyList<string> HomeClassTips(string? dreamJobKey, char riasecLetter, string language)
    {
        if (!PupilVerhaalLocalized.IsEnglish(language))
        {
            return PupilPassportCopy.HomeClassTips(dreamJobKey, riasecLetter);
        }

        if (!string.IsNullOrWhiteSpace(dreamJobKey)
            && EnTips.TryGetValue($"LeerlingPassport.Tip.{dreamJobKey}.1", out var t1))
        {
            var tips = new List<string> { t1 };
            if (EnTips.TryGetValue($"LeerlingPassport.Tip.{dreamJobKey}.2", out var t2))
            {
                tips.Add(t2);
            }

            return tips;
        }

        var letterKey = $"LeerlingPassport.Tip.Letter.{riasecLetter}.1";
        return EnTips.TryGetValue(letterKey, out var letterTip)
            ? [letterTip, EnTips["LeerlingPassport.Tip.Generic.2"]]
            : PupilPassportCopy.HomeClassTips(dreamJobKey, riasecLetter);
    }

    public static IReadOnlyList<string> DiscoveryQuestions(string? dreamJobKey, string? hollandCode, string language)
    {
        if (!PupilVerhaalLocalized.IsEnglish(language))
        {
            return PupilPassportCopy.DiscoveryQuestions(dreamJobKey, hollandCode);
        }

        var nl = PupilPassportCopy.DiscoveryQuestions(dreamJobKey, hollandCode);
        return nl.Select(q => q
            .Replace("Wat ", "What ", StringComparison.Ordinal)
            .Replace("welke ", "which ", StringComparison.OrdinalIgnoreCase)
            .Replace("hoe ", "how ", StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
