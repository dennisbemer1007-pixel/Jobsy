using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;

namespace Jobsy.Core.Scholen.PupilPassport;

/// <summary>Passport UI/PDF copy by language (mirrors UiStringsLeerlingPaspoort EN map).</summary>
public static class PupilPassportLocalizedCopy
{
    public static string Get(string key, string language)
    {
        if (!PupilVerhaalLocalized.IsEnglish(language))
        {
            var nl = PupilPassportCopy.Get(key);
            if (!string.Equals(nl, key, StringComparison.Ordinal))
            {
                return nl;
            }

            return PupilVerhaalCopy.TryGet(key, out var fromVerhaal) ? fromVerhaal : key;
        }

        return En(key, PupilPassportCopy.Get(key));
    }

    public static string WorldName(int worldNumber, string language)
    {
        if (!PupilVerhaalLocalized.IsEnglish(language))
        {
            return worldNumber switch
            {
                1 => "Koraalrif",
                2 => "Schatgrot",
                3 => "Vuurtoren",
                4 => "Lagune",
                _ => $"Wereld {worldNumber}"
            };
        }

        return worldNumber switch
        {
            1 => "Coral reef",
            2 => "Treasure cave",
            3 => "Lighthouse",
            4 => "Lagoon",
            _ => $"World {worldNumber}"
        };
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
        "LeerlingPassport.Strength.Samenwerken.Title" => "Working together",
        "LeerlingPassport.Strength.Samenwerken.Sub" => "I listen and think along",
        "LeerlingPassport.Strength.Resultaat.Title" => "Getting things done",
        "LeerlingPassport.Strength.Resultaat.Sub" => "I work towards a goal",
        "LeerlingPassport.Strength.Stress.Title" => "Staying calm",
        "LeerlingPassport.Strength.Stress.Sub" => "I stay calm when it is busy",
        "LeerlingPassport.Strength.Innovatie.Title" => "New ideas",
        "LeerlingPassport.Strength.Innovatie.Sub" => "I like coming up with new things",
        "LeerlingPassport.Strength.Extraversie.Title" => "With people",
        "LeerlingPassport.Strength.Extraversie.Sub" => "I like talking with others",
        "LeerlingPassport.Strength.Zorgvuldig.Title" => "Careful work",
        "LeerlingPassport.Strength.Zorgvuldig.Sub" => "I do things neatly",
        "LeerlingPassport.Strength.Helpen.Title" => "Helping",
        "LeerlingPassport.Strength.Helpen.Sub" => "I notice when someone is stuck",
        "LeerlingPassport.Strength.Onderzoek.Title" => "Exploring",
        "LeerlingPassport.Strength.Onderzoek.Sub" => "I want to know how things work",
        "LeerlingPassport.Strength.Praktisch.Title" => "Hands-on",
        "LeerlingPassport.Strength.Praktisch.Sub" => "I like to get stuck in myself",
        "LeerlingPassport.Strength.Verbinding.Title" => "Connection",
        "LeerlingPassport.Strength.Verbinding.Sub" => "I prefer working together",
        "LeerlingPassport.Strength.Doelen.Title" => "Goals",
        "LeerlingPassport.Strength.Doelen.Sub" => "I like a clear goal",
        "LeerlingPassport.Strength.Impact.Title" => "Making a difference",
        "LeerlingPassport.Strength.Impact.Sub" => "I want to mean something to others",
        "LeerlingPassport.Strength.Betrouwbaar.Title" => "Reliable",
        "LeerlingPassport.Strength.Betrouwbaar.Sub" => "You can count on me to keep promises",
        "LeerlingDroom.HaveTitle" => "What you already have",
        "LeerlingDroom.NeedTitle" => "What do you still need?",
        "LeerlingPassport.Footer.PageLabel" => "page 1/1",
        "LeerlingPassport.Discover.dierenarts.1" => "What does a day as a vet look like?",
        "LeerlingPassport.Discover.dierenarts.2" => "Which subjects fit biology in secondary school?",
        "LeerlingPassport.Discover.dierenarts.3" => "Am I also good at making things with my hands?",
        "LeerlingPassport.Discover.kok.1" => "What does a chef do in a kitchen every day?",
        "LeerlingPassport.Discover.kok.2" => "Which subjects help with cooking and hospitality?",
        "LeerlingPassport.Discover.kok.3" => "Do I like working with my hands and with people?",
        "LeerlingPassport.Discover.Letter.I.1" => "Which question would you like to investigate?",
        "LeerlingPassport.Discover.Letter.I.2" => "Which experiment or research task did you enjoy?",
        "LeerlingPassport.Discover.Letter.I.3" => "Which subject gives you the most “knowing how it works”?",
        "LeerlingPassport.Discover.Letter.S.1" => "In which job do you help others the most?",
        "LeerlingPassport.Discover.Letter.S.2" => "When do you feel most useful?",
        "LeerlingPassport.Discover.Letter.S.3" => "Which outing or internship fits care or animals?",
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

        if (!string.IsNullOrWhiteSpace(dreamJobKey)
            && !string.Equals(dreamJobKey, PupilDreamJobFit.UndecidedKey, StringComparison.OrdinalIgnoreCase))
        {
            var list = new List<string>();
            for (var i = 1; i <= 3; i++)
            {
                var key = $"LeerlingPassport.Discover.{dreamJobKey}.{i}";
                var q = Get(key, language);
                if (!string.Equals(q, PupilPassportCopy.Get(key), StringComparison.Ordinal) || i == 1)
                {
                    list.Add(q);
                }
            }

            if (list.Count >= 2)
            {
                return list;
            }
        }

        var letter = string.IsNullOrWhiteSpace(hollandCode) ? 'S' : char.ToUpperInvariant(hollandCode[0]);
        return
        [
            Get($"LeerlingPassport.Discover.Letter.{letter}.1", language),
            Get($"LeerlingPassport.Discover.Letter.{letter}.2", language),
            Get($"LeerlingPassport.Discover.Letter.{letter}.3", language)
        ];
    }

    public static string JobTitle(string jobKey, string language)
    {
        var catalog = DreamJobCatalog.All.FirstOrDefault(j =>
            string.Equals(j.Key, jobKey, StringComparison.OrdinalIgnoreCase));
        if (catalog is null)
        {
            return PupilRiasecJobIdeas.TitleFor(jobKey);
        }

        if (!PupilVerhaalLocalized.IsEnglish(language))
        {
            return catalog.TitleNl;
        }

        return EnJobTitles.TryGetValue(jobKey, out var en) ? en : catalog.TitleNl;
    }

    public static string JobDomain(string jobKey, string language)
    {
        var letter = PupilPassportCopy.JobRiasecLetter(jobKey);
        return Get($"LeerlingPassport.Card.Domain.{letter}", language);
    }

    /// <summary>Route step headline for web/PDF (e.g. groep 8 → Group 8 in EN).</summary>
    public static string RouteStepTitle(string title, string language)
    {
        if (!PupilVerhaalLocalized.IsEnglish(language))
        {
            return title;
        }

        if (title.Contains("groep 8", StringComparison.OrdinalIgnoreCase))
        {
            return "Group 8";
        }

        if (title.Contains("groep 7", StringComparison.OrdinalIgnoreCase))
        {
            return "Group 7";
        }

        if (title.Contains("Diergeneeskunde", StringComparison.Ordinal))
        {
            return "Study";
        }

        if (title.Contains("havo", StringComparison.OrdinalIgnoreCase)
            || title.Contains("vwo", StringComparison.OrdinalIgnoreCase))
        {
            return "Havo/vwo";
        }

        return title;
    }

    public static string JobBlurb(string jobKey, PupilQuestionSet set, string language)
    {
        if (!PupilVerhaalLocalized.IsEnglish(language))
        {
            return PupilPassportCopy.JobBlurb(jobKey, set);
        }

        return Get(set == PupilQuestionSet.Groep78
            ? "LeerlingPassport.Card.Blurb.Generic.G78"
            : "LeerlingPassport.Card.Blurb.Generic.Vo", language);
    }

    private static readonly Dictionary<string, string> EnJobTitles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["elektricien"] = "Electrician",
        ["programmeur"] = "Programmer",
        ["ingenieur"] = "Engineer",
        ["kok"] = "Chef",
        ["dierenarts"] = "Vet",
        ["leraar"] = "Teacher",
        ["politie"] = "Police officer",
        ["brandweer"] = "Firefighter",
        ["verpleegkundige"] = "Nurse",
    };
}
