using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Jobsy.Core.Careers;

/// <summary>
/// Facts an honest-advice line may use. Only the occupation catalogue and the sourced outlook.
/// </summary>
public sealed class HonestAdviceFacts
{
    public const int DescriptionLimit = 700;

    public HonestAdviceFacts(
        string escoId,
        string title,
        string description,
        string? demandLine,
        string? aiLine,
        IReadOnlyList<string>? changeTasks,
        IReadOnlyList<string>? humanTasks,
        IReadOnlyList<string>? skills,
        string? peildatum)
    {
        EscoId = (escoId ?? "").Trim();
        Title = (title ?? "").Trim();
        Description = Clip(description, DescriptionLimit);
        var demandOk = IsReal(demandLine, OccupationOutlook.MissingDemand);
        var aiOk = IsReal(aiLine, OccupationOutlook.MissingIlo);
        DemandLine = demandOk ? demandLine!.Trim() : null;
        AiLine = aiOk ? aiLine!.Trim() : null;
        ChangeTasks = Clean(changeTasks);
        HumanTasks = Clean(humanTasks);
        Skills = Clean(skills);
        Peildatum = (peildatum ?? "").Trim();
        EnoughToAdvise = demandOk || aiOk;
        Corpus = BuildCorpus(this);
        SourceHash = Hash(Corpus);
    }

    public string EscoId { get; }

    public string Title { get; }

    public string Description { get; }

    public string? DemandLine { get; }

    public string? AiLine { get; }

    public IReadOnlyList<string> ChangeTasks { get; }

    public IReadOnlyList<string> HumanTasks { get; }

    public IReadOnlyList<string> Skills { get; }

    public string Peildatum { get; }

    /// <summary>False when the outlook has neither a demand line nor an AI line.</summary>
    public bool EnoughToAdvise { get; }

    public string Corpus { get; }

    public string SourceHash { get; }

    public static HonestAdviceFacts? TryFor(string? escoId)
    {
        if (string.IsNullOrWhiteSpace(escoId))
        {
            return null;
        }

        var occupation = OccupationCatalog.Shared.Get(escoId);
        if (occupation is null)
        {
            return null;
        }

        var outlook = OccupationOutlook.Shared.Get(occupation.Id);
        var skills = new List<string>();
        foreach (var index in OccupationSkills.Shared.Essential(occupation.Id).Take(4))
        {
            var label = OccupationSkills.Shared.Label(index);
            if (!string.IsNullOrWhiteSpace(label))
            {
                skills.Add(label.Trim());
            }
        }

        return new HonestAdviceFacts(
            occupation.Id,
            occupation.Nl,
            occupation.Desc,
            outlook.DemandLine,
            outlook.AiLine,
            outlook.ChangeTasks,
            outlook.HumanTasks,
            skills,
            outlook.Peildatum);
    }

    public static string Hash(string corpus)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(corpus ?? ""));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string BuildCorpus(HonestAdviceFacts facts)
    {
        var sb = new StringBuilder();
        sb.AppendLine(facts.Title);
        sb.AppendLine(facts.Description);
        sb.AppendLine(facts.DemandLine ?? "");
        sb.AppendLine(facts.AiLine ?? "");
        foreach (var task in facts.ChangeTasks)
        {
            sb.AppendLine(task);
        }

        foreach (var task in facts.HumanTasks)
        {
            sb.AppendLine(task);
        }

        foreach (var skill in facts.Skills)
        {
            sb.AppendLine(skill);
        }

        return sb.ToString();
    }

    private static bool IsReal(string? line, string missing)
        => !string.IsNullOrWhiteSpace(line)
           && !string.Equals(line.Trim(), missing, StringComparison.Ordinal);

    private static IReadOnlyList<string> Clean(IReadOnlyList<string>? lines)
    {
        if (lines is null || lines.Count == 0)
        {
            return [];
        }

        var list = new List<string>();
        foreach (var line in lines)
        {
            var trimmed = (line ?? "").Trim();
            if (trimmed.Length > 0 && !list.Contains(trimmed, StringComparer.Ordinal))
            {
                list.Add(trimmed);
            }
        }

        return list;
    }

    private static string Clip(string? text, int max)
    {
        var value = (text ?? "").Trim().Replace("\r\n", "\n", StringComparison.Ordinal);
        if (value.Length <= max)
        {
            return value;
        }

        return value[..max].TrimEnd() + "…";
    }
}

/// <summary>Dutch B1 prompt for the offline generator. The candidate app never calls this.</summary>
public static class HonestAdvicePrompt
{
    public const string System = """
        Je schrijft een eerlijk advies voor één beroep, in het Nederlands, niveau B1 of lager.
        Vriendelijk en duidelijk. Niet kinderachtig. Niet angstig.
        Regels:
        - Gebruik alleen de feiten in het bericht. Verzin geen cijfers, werkgevers, diploma's, salarissen of plaatsen.
        - Twee tot vier korte zinnen. Geen markdown. Geen opsomming. Geen titel.
        - Als AI taken kan veranderen: zeg dat het werk verandert en welke taken mensenwerk blijven. Zeg dat het nuttig blijft als je leert met AI te werken.
        - Als de vraag ruim of krap is: zeg dat rustig, in gewone woorden.
        - Zeg nooit dat het beroep verdwijnt, geen toekomst heeft, overbodig is, of tot ontslag of werkloosheid leidt.
        - Gebruik deze woorden niet: verdwijnt, verdwijnen, geen toekomst, overbodig, ontslag, werkloos.
        - Ontbreekt een feit, laat het weg. Zeg niet dat je het weet.
        - Sluit af met één praktische tip alleen als er een vaardigheid in de feiten staat.
        - Antwoord alleen met de adviesalinea.
        """;

    /// <summary>
    /// Same provider and temperature as <c>OpenAiTranslationService</c>: translate the stored Dutch line, do not add facts.
    /// </summary>
    public static string TranslateSystem(string? language)
    {
        var name = Jobsy.Core.Localization.JobsyLanguages.Get(language).NativeName;
        return
            "You are a professional translator from Nederlands to " + name + ". " +
            "Translate only. Keep the meaning. No markdown. " +
            "Do not add numbers, employers, diplomas, salaries, or places that are not in the Dutch text. " +
            "Keep two to four short sentences. Plain and calm, not childish. " +
            "Never say the job disappears, has no future, is obsolete, or leads to dismissal or unemployment. " +
            "Do not use: disappears, no future, obsolete, redundant, unemployment, verdwijnt, verdwijnen, geen toekomst, overbodig, ontslag, werkloos. " +
            "Return only the translated paragraph.";
    }

    public static string User(HonestAdviceFacts facts)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Feiten. Gebruik alleen dit.");
        sb.Append("Beroep: ").AppendLine(facts.Title);
        sb.Append("Omschrijving: ").AppendLine(facts.Description);
        sb.Append("Vraag: ").AppendLine(facts.DemandLine ?? "geen cijfers");
        sb.Append("AI: ").AppendLine(facts.AiLine ?? "geen cijfers");
        sb.Append("Taken die kunnen veranderen: ")
            .AppendLine(facts.ChangeTasks.Count == 0 ? "geen" : string.Join("; ", facts.ChangeTasks));
        sb.Append("Taken die mensenwerk blijven: ")
            .AppendLine(facts.HumanTasks.Count == 0 ? "geen" : string.Join("; ", facts.HumanTasks));
        sb.Append("Vaardigheden: ")
            .AppendLine(facts.Skills.Count == 0 ? "geen" : string.Join("; ", facts.Skills));
        return sb.ToString();
    }
}

/// <summary>Rejects advice that is empty, too long, uses a forbidden word, or adds numbers.</summary>
public static partial class HonestAdviceValidator
{
    public const int MaxChars = 480;
    public const int MaxSentences = 4;

    public const string TooLittle =
        "Dat weten we niet. We hebben te weinig gegevens voor een advies over dit werk.";

    public static string TooLittleFor(string? language) => Jobsy.Core.Localization.JobsyLanguages.Normalize(language) switch
    {
        "en" => "We do not know this. We have too little information for advice about this work.",
        "pl" => "Tego nie wiemy. Mamy za mało danych na radę o tej pracy.",
        "ro" => "Nu știm asta. Avem prea puține date pentru un sfat despre această muncă.",
        "ar" => "لا نعرف ذلك. لدينا معلومات قليلة جداً لنصيحة عن هذا العمل.",
        _ => TooLittle
    };

    public static bool ContainsForbiddenWord(string? text, string? language)
    {
        if (OccupationOutlook.ContainsForbiddenWord(text))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var fold = text.ToLowerInvariant();
        foreach (var word in Forbidden(language))
        {
            if (fold.Contains(word, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static string? StructuralReason(string? text, string? language = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "empty";
        }

        var trimmed = text.Trim();
        if (trimmed.Length > MaxChars)
        {
            return "too-long";
        }

        var sentences = Sentences(trimmed);
        if (sentences.Count < 2)
        {
            return "too-few-sentences";
        }

        if (sentences.Count > MaxSentences)
        {
            return "too-many-sentences";
        }

        if (ContainsForbiddenWord(trimmed, language))
        {
            return "forbidden-word";
        }

        if (trimmed.Contains("**", StringComparison.Ordinal)
            || trimmed.Contains("__", StringComparison.Ordinal)
            || trimmed.Contains('`')
            || trimmed.Contains("##", StringComparison.Ordinal))
        {
            return "markdown";
        }

        return null;
    }

    public static string? RejectionReason(string? text, HonestAdviceFacts facts, string? language = null)
    {
        var lang = Jobsy.Core.Localization.JobsyLanguages.Normalize(language);
        var structural = StructuralReason(text, lang);
        if (structural is not null)
        {
            return structural;
        }

        var trimmed = text!.Trim();
        var thin = TooLittleFor(lang);
        if (!facts.EnoughToAdvise)
        {
            return string.Equals(trimmed, thin, StringComparison.Ordinal) ? null : "ungrounded";
        }

        if (string.Equals(trimmed, thin, StringComparison.Ordinal)
            || string.Equals(trimmed, TooLittle, StringComparison.Ordinal))
        {
            return "ungrounded";
        }

        foreach (Match match in Numbers().Matches(trimmed))
        {
            if (!facts.Corpus.Contains(match.Value, StringComparison.Ordinal))
            {
                return "invented-number";
            }
        }

        if (trimmed.Contains('€', StringComparison.Ordinal) && !facts.Corpus.Contains('€', StringComparison.Ordinal))
        {
            return "invented-number";
        }

        if (trimmed.Contains('%') && !facts.Corpus.Contains('%'))
        {
            return "invented-number";
        }

        foreach (var token in ClaimTokens(lang))
        {
            if (HasToken(trimmed, token) && !HasToken(facts.Corpus, token))
            {
                return "invented-claim";
            }
        }

        return null;
    }

    public static string ReasonSentence(string? reason) => reason switch
    {
        "empty" => "De tekst is leeg. Schrijf 2 tot 4 zinnen.",
        "too-long" => "De tekst is te lang. Maak hem korter dan 480 tekens.",
        "too-few-sentences" => "Schrijf minstens 2 zinnen.",
        "too-many-sentences" => "Schrijf hoogstens 4 zinnen.",
        "forbidden-word" => "Gebruik niet: verdwijnt, verdwijnen, geen toekomst, overbodig, ontslag, werkloos.",
        "markdown" => "Geen markdown. Alleen gewone zinnen.",
        "invented-number" => "Gebruik alleen een cijfer dat letterlijk in de feiten staat.",
        "invented-claim" => "Noem geen diploma, salaris of euro dat niet in de feiten staat.",
        "ungrounded" => "Er zijn te weinig feiten. Gebruik alleen de korte zin dat we te weinig weten.",
        _ => "Gebruik alleen de feiten. Verzin niets."
    };

    private static IEnumerable<string> Forbidden(string? language) => Jobsy.Core.Localization.JobsyLanguages.Normalize(language) switch
    {
        "en" => ["disappear", "no future", "obsolete", "redundant", "laid off", "unemployment", "unemployed"],
        "pl" => ["znika", "znikają", "zniknie", "bez przyszłości", "zbędn", "zwolnieni", "bezrobot"],
        "ro" => ["dispare", "fără viitor", "fara viitor", "nu are viitor", "concediere", "șomaj", "somaj", "de prisos"],
        "ar" => ["يختفي", "تختفي", "لا مستقبل", "زائد عن الحاجة", "بطالة", "فصل من العمل"],
        _ => []
    };

    private static IEnumerable<string> ClaimTokens(string language)
    {
        yield return "diploma";
        yield return "mbo";
        yield return "hbo";
        yield return "salaris";
        yield return "euro";
        switch (language)
        {
            case "en":
                yield return "salary";
                yield return "wage";
                break;
            case "pl":
                yield return "dyplom";
                yield return "pensja";
                yield return "wynagrodzenie";
                break;
            case "ro":
                yield return "salariu";
                break;
            case "ar":
                yield return "راتب";
                yield return "دبلوم";
                break;
        }
    }

    private static bool HasToken(string text, string token)
        => Regex.IsMatch(text, $@"(?<!\p{{L}}){Regex.Escape(token)}(?!\p{{L}})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static List<string> Sentences(string text)
        => Regex.Split(text, @"(?<=[\.!\?\u061F])")
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .ToList();

    [GeneratedRegex(@"\d+(?:[.,]\d+)?", RegexOptions.CultureInvariant)]
    private static partial Regex Numbers();
}
