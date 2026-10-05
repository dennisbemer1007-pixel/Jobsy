using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Jobsy.Core.Localization;

namespace Jobsy.Core.Rules;

/// <summary>Local first-person "Wie ben ik?" narrative when OpenAI is unavailable.</summary>
public static class WhoAmIStoryBuilder
{
    public const int MaxStoryChars = 2500;

    public static string Build(
        CompetencyScores competency,
        RiasecScores career,
        CulturePersonalityScores culture,
        WhoAmIProfileHighlights? profile = null,
        SchwartzValuesScores? values = null,
        bool employersEnabled = true,
        string? language = null,
        IReadOnlyList<(string Code, int Score)>? competence = null)
    {
        var careerTop = TopLabels(
            CareerTestCatalog.RiasecCodes.Select(c => (c, CareerCompassBuilder.TypeLabel(c), career.Get(c))),
            2);
        var cultureTop = TopLabels(
            CulturePersonalityCatalog.CategoryCodes.Select(c => (c, CulturePersonalityCatalog.EverydayLabel(c), culture.Get(c))),
            2);
        var compTop = StrengthLabels(competence, competency, "nl");
        var valuesTop = values is { IsComplete: true }
            ? TopLabels(
                SchwartzValuesCatalog.CategoryCodes.Select(c => (c, SchwartzValuesCatalog.EverydayLabel(c), values.Get(c))),
                2)
            : [];
        var keywords = WhoAmIKeywords.FromScores(competency, career, culture, values, language);
        profile ??= WhoAmIProfileHighlights.Empty;
        var lang = JobsyLanguages.Normalize(language);
        if (lang is not ("" or "nl"))
        {
            return Sanitize(BuildTranslated(lang, career, culture, competency, values, profile, keywords, employersEnabled, competence)) ?? Fallback;
        }

        var sb = new StringBuilder();
        sb.Append("Ik kom tot mijn recht als ik ");
        sb.Append(JoinDutch(careerTop.Select(FirstPersonDirection).ToList()));
        sb.Append(". Op de werkvloer voel ik me het best bij ");
        sb.Append(JoinDutch(cultureTop));
        sb.AppendLine(".");
        sb.AppendLine();
        if (valuesTop.Count > 0)
        {
            sb.Append("Wat mij drijft is ");
            sb.Append(JoinDutch(valuesTop));
            sb.AppendLine(employersEnabled
                ? ". Dat zoek ik terug in cultuur en beloftes van een werkgever."
                : ". Dat zoek ik terug in hoe een gewone werkdag eruitziet.");
            sb.AppendLine();
        }

        if (profile.Roles.Count > 0 || profile.Educations.Count > 0 || profile.Certificates.Count > 0)
        {
            sb.Append("In mijn pad zie je ");
            var bits = new List<string>();
            if (profile.Roles.Count > 0)
            {
                bits.Add("ervaring als " + JoinDutch(LocalizedRoles(profile, "nl")));
            }

            if (profile.Educations.Count > 0)
            {
                bits.Add("opleiding in " + JoinDutch(profile.Educations.Take(2).ToList()));
            }

            if (profile.Certificates.Count > 0)
            {
                bits.Add("het certificaat " + JoinDutch(profile.Certificates.Take(2).ToList()));
            }

            sb.Append(JoinDutch(bits));
            sb.AppendLine(".");
            sb.AppendLine();
        }

        AppendStrength(sb, "nl", compTop);
        if (compTop.Count > 0 && !string.IsNullOrWhiteSpace(profile.HomeCity))
        {
            sb.Append(" Dat doe ik in ");
            sb.Append(profile.HomeCity.Trim());
            sb.Append('.');
        }

        sb.Append(" Bij een ploeg die op elkaar kan bouwen.");
        sb.AppendLine();
        sb.AppendLine();
        if (keywords.Count > 0)
        {
            sb.Append("Wat mij typeert: ");
            sb.Append(JoinDutch(keywords.Take(4).ToList()));
            sb.Append(employersEnabled
                ? ". Ik vertel dit verhaal liever in gewone woorden, zodat een werkgever meteen voelt of we bij elkaar passen."
                : ". Ik vertel dit verhaal liever in gewone woorden, zodat meteen duidelijk is of het werk bij me past.");
        }
        else
        {
            sb.Append(employersEnabled
                ? "Ik vertel dit verhaal liever in gewone woorden, zodat een werkgever meteen voelt of we bij elkaar passen."
                : "Ik vertel dit verhaal liever in gewone woorden, zodat meteen duidelijk is of het werk bij me past.");
        }

        return Sanitize(sb.ToString()) ?? Fallback;
    }

    private static string BuildTranslated(
        string lang,
        RiasecScores career,
        CulturePersonalityScores culture,
        CompetencyScores competency,
        SchwartzValuesScores? values,
        WhoAmIProfileHighlights profile,
        IReadOnlyList<string> keywords,
        bool employersEnabled,
        IReadOnlyList<(string Code, int Score)>? competence)
    {
        string Label(string code) => DimensionLabels.For(code, lang);
        var careerTop = TopLabels(CareerTestCatalog.RiasecCodes.Select(c => (c, Label(c), career.Get(c))), 2);
        var cultureTop = TopLabels(CulturePersonalityCatalog.CategoryCodes.Select(c => (c, Label(c), culture.Get(c))), 2);
        var compTop = StrengthLabels(competence, competency, lang);
        var valuesTop = values is { IsComplete: true }
            ? TopLabels(SchwartzValuesCatalog.CategoryCodes.Select(c => (c, Label(c), values.Get(c))), 2)
            : [];
        var and = lang switch
        {
            "pl" => "i",
            "ro" => "și",
            "ar" => "و",
            _ => "and"
        };
        string Join(IReadOnlyList<string> items) => JoinWith(items, and, lang is "ar" ? "، " : ", ");

        var sb = new StringBuilder();
        sb.Append(lang switch
        {
            "pl" => "Najlepiej pracuję, gdy chodzi o: ",
            "ro" => "Lucrez cel mai bine când mă concentrez pe ",
            "ar" => "أعمل بأفضل شكل عندما أركز على ",
            _ => "I do my best work when I focus on "
        });
        sb.Append(Join(careerTop));
        sb.Append(lang switch
        {
            "pl" => ". W pracy najlepiej czuję się przy ",
            "ro" => ". La lucru mă simt cel mai bine cu ",
            "ar" => ". في العمل أشعر بأفضل حال مع ",
            _ => ". At work I feel best with "
        });
        sb.Append(Join(cultureTop));
        sb.AppendLine(".");
        sb.AppendLine();
        if (valuesTop.Count > 0)
        {
            sb.Append(lang switch
            {
                "pl" => "Napędza mnie ",
                "ro" => "Mă motivează ",
                "ar" => "ما يدفعني هو ",
                _ => "What drives me is "
            });
            sb.Append(Join(valuesTop));
            sb.AppendLine(employersEnabled
                ? lang switch
                {
                    "pl" => ". Tego szukam w zwykłym dniu pracy i w obietnicach miejsca pracy.",
                    "ro" => ". Asta caut într-o zi normală de lucru și în promisiunile locului de muncă.",
                    "ar" => ". هذا ما أبحث عنه في يوم عمل عادي وفي وعود مكان العمل.",
                    _ => ". I look for that in a normal workday and in what a workplace promises."
                }
                : lang switch
                {
                    "pl" => ". Tego szukam w zwykłym dniu pracy.",
                    "ro" => ". Asta caut într-o zi normală de lucru.",
                    "ar" => ". هذا ما أبحث عنه في يوم عمل عادي.",
                    _ => ". I look for that in a normal workday."
                });
            sb.AppendLine();
        }

        if (profile.Roles.Count > 0 || profile.Educations.Count > 0 || profile.Certificates.Count > 0)
        {
            var bits = new List<string>();
            if (profile.Roles.Count > 0)
            {
                bits.Add(lang switch
                {
                    "pl" => "doświadczenie jako " + Join(LocalizedRoles(profile, lang)),
                    "ro" => "experiență ca " + Join(LocalizedRoles(profile, lang)),
                    "ar" => "خبرة كـ " + Join(LocalizedRoles(profile, lang)),
                    _ => "experience as " + Join(LocalizedRoles(profile, lang))
                });
            }

            if (profile.Educations.Count > 0)
            {
                bits.Add(lang switch
                {
                    "pl" => "wykształcenie " + Join(profile.Educations.Take(2).ToList()),
                    "ro" => "studii " + Join(profile.Educations.Take(2).ToList()),
                    "ar" => "تعليم " + Join(profile.Educations.Take(2).ToList()),
                    _ => "education " + Join(profile.Educations.Take(2).ToList())
                });
            }

            if (profile.Certificates.Count > 0)
            {
                bits.Add(lang switch
                {
                    "pl" => "certyfikat " + Join(profile.Certificates.Take(2).ToList()),
                    "ro" => "certificatul " + Join(profile.Certificates.Take(2).ToList()),
                    "ar" => "شهادة " + Join(profile.Certificates.Take(2).ToList()),
                    _ => "the certificate " + Join(profile.Certificates.Take(2).ToList())
                });
            }

            sb.Append(lang switch
            {
                "pl" => "Na mojej drodze widać ",
                "ro" => "Pe drumul meu se vede ",
                "ar" => "في مساري ترى ",
                _ => "On my path you see "
            });
            sb.Append(Join(bits));
            sb.AppendLine(".");
            sb.AppendLine();
        }

        AppendStrength(sb, lang, compTop);
        if (compTop.Count > 0 && !string.IsNullOrWhiteSpace(profile.HomeCity))
        {
            sb.Append(lang switch
            {
                "pl" => " Robię to w ",
                "ro" => " Fac asta în ",
                "ar" => " أفعل ذلك في ",
                _ => " I do that in "
            });
            sb.Append(profile.HomeCity.Trim());
            sb.Append('.');
        }

        sb.Append(lang switch
        {
            "pl" => " W zespole, na który można liczyć.",
            "ro" => " Într-o echipă pe care te poți baza.",
            "ar" => " مع فريق يمكن الاعتماد عليه.",
            _ => " With a team that can count on each other."
        });
        sb.AppendLine();
        sb.AppendLine();
        if (keywords.Count > 0)
        {
            sb.Append(lang switch
            {
                "pl" => "Co mnie opisuje: ",
                "ro" => "Ce mă descrie: ",
                "ar" => "ما يصفني: ",
                _ => "What describes me: "
            });
            sb.Append(Join(keywords.Take(4).ToList()));
            sb.Append('.');
        }

        return sb.ToString();
    }

    private static string JoinWith(IReadOnlyList<string> items, string and, string comma = ", ") => items.Count switch
    {
        0 => "",
        1 => items[0],
        2 => $"{items[0]} {and} {items[1]}",
        _ => string.Join(comma, items.Take(items.Count - 1)) + " " + and + " " + items[^1]
    };

    /// <summary>Traits at 50 or higher. One trait uses the singular line. None drops the line.</summary>
    private static List<string> StrengthLabels(
        IReadOnlyList<(string Code, int Score)>? competence,
        CompetencyScores competency,
        string? lang)
    {
        IEnumerable<(string Code, int Score)> source = competence is not null
            ? competence
            : CompetencyTestCatalog.QuickScanCategories.Select(code => (code, competency.Get(code)));
        return source
            .Where(item => item.Score >= 50 && !string.IsNullOrWhiteSpace(item.Code))
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .Select(item => DimensionLabels.For(item.Code, lang))
            .Where(label => !string.IsNullOrWhiteSpace(label) && !CareerCompassBuilder.ContainsForbiddenJargon(label))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();
    }

    private static void AppendStrength(StringBuilder sb, string lang, IReadOnlyList<string> labels)
    {
        if (labels.Count == 0)
        {
            return;
        }

        if (lang is "" or "nl")
        {
            if (labels.Count == 1)
            {
                sb.Append("Mijn sterkste kant is ");
                sb.Append(LowerInside(labels[0]));
            }
            else
            {
                sb.Append("Op de werkvloer is mijn kracht ");
                sb.Append(JoinDutch(labels));
            }

            sb.Append(". Ik wil werk waarin ik dat elke dag laat zien.");
            return;
        }

        if (labels.Count == 1)
        {
            sb.Append(lang switch
            {
                "pl" => "Moja najmocniejsza strona to ",
                "ro" => "Partea mea cea mai puternică este ",
                "ar" => "أقوى جانب لدي هو ",
                _ => "My strongest side is "
            });
            sb.Append(labels[0]);
        }
        else
        {
            sb.Append(lang switch
            {
                "pl" => "W pracy moją siłą jest ",
                "ro" => "La lucru puterea mea este ",
                "ar" => "في العمل قوتي هي ",
                _ => "At work my strength is "
            });
            var and = lang switch
            {
                "pl" => "i",
                "ro" => "și",
                "ar" => "و",
                _ => "and"
            };
            sb.Append(JoinWith(labels, and, lang is "ar" ? "، " : ", "));
        }

        sb.Append(lang switch
        {
            "pl" => ". Chcę pracy, w której pokazuję to każdego dnia.",
            "ro" => ". Vreau muncă în care arăt asta în fiecare zi.",
            "ar" => ". أريد عملاً أُظهر فيه ذلك كل يوم.",
            _ => ". I want work where I show that every day."
        });
    }

    /// <summary>Hide the employer line while that feature is off. The Dutch template is translated on display.</summary>
    public static string ForDisplay(string? story, bool employersEnabled)
    {
        if (string.IsNullOrWhiteSpace(story))
        {
            return "";
        }

        var text = employersEnabled
            ? story
            : story
                .Replace(
                    "Dat zoek ik terug in cultuur en beloftes van een werkgever.",
                    "Dat zoek ik terug in hoe een gewone werkdag eruitziet.",
                    StringComparison.Ordinal)
                .Replace(
                    "zodat een werkgever meteen voelt of we bij elkaar passen.",
                    "zodat meteen duidelijk is of het werk bij me past.",
                    StringComparison.Ordinal);
        return ShortenLongSentences(PlainLanguage(text));
    }

    public static string? Sanitize(string? story)
    {
        if (string.IsNullOrWhiteSpace(story))
        {
            return null;
        }

        var trimmed = story.Trim();
        if (trimmed.Length > MaxStoryChars)
        {
            trimmed = trimmed[..MaxStoryChars].Trim();
        }

        if (CareerCompassBuilder.ContainsForbiddenJargon(trimmed))
        {
            return null;
        }

        if (trimmed.Contains('@', StringComparison.Ordinal)
            || Regex.IsMatch(trimmed, @"\+?\d[\d\s\-]{7,}\d"))
        {
            return null;
        }

        trimmed = PlainLanguage(trimmed);
        return ShortenLongSentences(trimmed);
    }

    /// <summary>
    /// Rejects a model story that invents work, contradicts a high samenwerken score,
    /// repeats the same idea, or is not 2–4 paragraphs. The caller then uses the local story.
    /// </summary>
    public static bool Accepts(
        string? story,
        WhoAmIProfileHighlights? profile,
        CompetencyScores? competency,
        CulturePersonalityScores? culture,
        RiasecScores? career = null,
        SchwartzValuesScores? values = null)
    {
        return StoryRuleReason(story, profile, competency, culture, career, values) is null
               && CandidateFactGuard.RejectionReason(
                   story,
                   FactSheet(profile, competency, culture, career, values)) is null;
    }

    /// <summary>
    /// Which story-shape rule failed: too-short, paragraphs, repeats, first-person, samenwerken.
    /// Null when the shape is fine. Fact mistakes stay on <see cref="CandidateFactGuard"/>.
    /// </summary>
    public static string? StoryRuleReason(
        string? story,
        WhoAmIProfileHighlights? profile = null,
        CompetencyScores? competency = null,
        CulturePersonalityScores? culture = null,
        RiasecScores? career = null,
        SchwartzValuesScores? values = null)
    {
        if (string.IsNullOrWhiteSpace(story) || story.Trim().Length < 40)
        {
            return "too-short";
        }

        if (!FirstPerson.IsMatch(story))
        {
            return "first-person";
        }

        var normalized = NormalizeParagraphs(story);
        var paragraphs = normalized
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (paragraphs.Length is < 2 or > 6)
        {
            return "paragraphs";
        }

        if (RepeatsIdea(story))
        {
            return "repeats";
        }

        if (ContradictsSamenwerken(story, competency, culture))
        {
            return "samenwerken";
        }

        _ = profile;
        _ = career;
        _ = values;
        return null;
    }

    private static CandidateFactSheet FactSheet(
        WhoAmIProfileHighlights? profile,
        CompetencyScores? competency,
        CulturePersonalityScores? culture,
        RiasecScores? career,
        SchwartzValuesScores? values)
    {
        profile ??= WhoAmIProfileHighlights.Empty;
        return competency is not null && career is not null && culture is not null
            ? CandidateFactSheet.ForWhoAmI(competency, career, culture, profile, values)
            : CandidateFactSheet.Personal(profile.Roles, profile.Educations, profile.Certificates, homeCity: profile.HomeCity);
    }

    /// <summary>
    /// Blank lines split paragraphs. A model that only uses single newlines is split into
    /// 2–4 paragraphs before the shape check. One short sentence stays one paragraph.
    /// </summary>
    public static string NormalizeParagraphs(string story)
    {
        var text = story.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Trim();
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        if (text.Contains("\n\n", StringComparison.Ordinal))
        {
            return text.Trim();
        }

        if (!text.Contains('\n', StringComparison.Ordinal))
        {
            var split = SplitLongBlock(text);
            return split is null ? text : string.Join("\n\n", split);
        }

        var blocks = text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        blocks = MergeShortLines(blocks);
        while (blocks.Count > 4)
        {
            var index = 0;
            var best = int.MaxValue;
            for (var i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].Length < best)
                {
                    best = blocks[i].Length;
                    index = i;
                }
            }

            var other = index == 0 ? 1 : index - 1;
            var keep = Math.Min(index, other);
            var drop = Math.Max(index, other);
            blocks[keep] = blocks[keep] + " " + blocks[drop];
            blocks.RemoveAt(drop);
        }

        if (blocks.Count == 1)
        {
            var split = SplitLongBlock(blocks[0]);
            if (split is not null)
            {
                blocks = split;
            }
        }

        return string.Join("\n\n", blocks).Trim();
    }

    /// <summary>Raw line count and lengths for the rejection log. No spaces, no profile text.</summary>
    public static string ParagraphStats(string? story)
    {
        if (string.IsNullOrWhiteSpace(story))
        {
            return "paragraphs=0";
        }

        var raw = story.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var parts = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var lengths = string.Join(',', parts.Select(part => part.Length.ToString(CultureInfo.InvariantCulture)));
        if (lengths.Length > 24)
        {
            lengths = lengths[..24];
        }

        return $"paragraphs={parts.Length};lengths={lengths}";
    }

    private static List<string> MergeShortLines(List<string> blocks)
    {
        var merged = new List<string>();
        foreach (var block in blocks)
        {
            if (merged.Count > 0 && (block.Length < 40 || merged[^1].Length < 80))
            {
                merged[^1] = merged[^1] + " " + block;
            }
            else
            {
                merged.Add(block);
            }
        }

        return merged;
    }

    private static List<string>? SplitLongBlock(string block)
    {
        var sentences = Regex.Split(block.Trim(), @"(?<=[.!?])\s+")
            .Where(sentence => sentence.Length > 0)
            .ToList();
        if (sentences.Count < 2 || block.Length < 80)
        {
            return null;
        }

        var mid = Math.Max(1, sentences.Count / 2);
        var first = string.Join(' ', sentences.Take(mid));
        var second = string.Join(' ', sentences.Skip(mid));
        if (first.Length < 40 || second.Length < 40)
        {
            return null;
        }

        return [first, second];
    }

    private static List<string> LocalizedRoles(WhoAmIProfileHighlights profile, string? language)
        => profile.Roles.Take(2).Select(role => OccupationTitles.LocalizeWorkLine(role, language)).ToList();

    private static bool RepeatsIdea(string story)
    {
        var stripped = story;
        foreach (var label in ScoreLabels)
        {
            stripped = Regex.Replace(
                stripped,
                Regex.Escape(label),
                " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        foreach (var stem in new[] { "helpen", "help", "netjes" })
        {
            var count = Regex.Matches(
                stripped,
                $@"\b{stem}\w*",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Count;
            if (count >= 6)
            {
                return true;
            }
        }

        return false;
    }

    private static readonly string[] ScoreLabels =
    [
        "mensen helpen",
        "netjes organiseren",
        "netjes en betrouwbaar",
        "afmaken & netjes werken",
        "afmaken en netjes werken"
    ];

    private static bool ContradictsSamenwerken(
        string story,
        CompetencyScores? competency,
        CulturePersonalityScores? culture)
    {
        var samenHigh = competency?.Samenwerken is >= 60;
        var prefersAlone = culture?.Autonomy is >= 60 && culture.Collaboration is < 50;
        var negatesTogether = Regex.IsMatch(
            story,
            @"samen\w{0,12}.{0,40}niet te veel|niet te veel.{0,40}samen|liever niet.{0,40}samen|samenwerken doe ik liever niet",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);
        if (samenHigh && negatesTogether)
        {
            return true;
        }

        var saysAlone = Regex.IsMatch(
            story,
            @"liever alleen|het liefst alleen|alleen werken|liever niet te veel samen",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (saysAlone && (samenHigh || !prefersAlone))
        {
            return true;
        }

        return false;
    }

    /// <summary>B1: drop abstract nouns and split any sentence longer than 20 words.</summary>
    internal static string ShortenLongSentences(string text)
    {
        var sb = new StringBuilder(text.Length + 32);
        var start = 0;
        for (var i = 0; i <= text.Length; i++)
        {
            var atEnd = i == text.Length;
            var boundary = !atEnd
                && text[i] is '.' or '!' or '?'
                && (i + 1 == text.Length || char.IsWhiteSpace(text[i + 1]));
            if (!atEnd && !boundary)
            {
                continue;
            }

            var end = boundary ? i + 1 : text.Length;
            if (end <= start)
            {
                continue;
            }

            var sentence = text[start..end];
            var lead = 0;
            while (lead < sentence.Length && char.IsWhiteSpace(sentence[lead]))
            {
                lead++;
            }

            sb.Append(sentence[..lead]);
            var body = sentence[lead..];
            sb.Append(WordCount(body) > 20 ? ChunkSentence(body) : body);
            start = end;
        }

        return sb.ToString();
    }

    private static string PlainLanguage(string text)
        => Regex.Replace(
            Regex.Replace(
                Regex.Replace(text, @"\bvermogen\b", "wat ik kan", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
                "stimuleren van groei",
                "beter worden",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
            "maken van impact",
            "iets doen voor anderen",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static int WordCount(string sentence)
        => sentence.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    private static string ChunkSentence(string sentence)
    {
        var trimmed = sentence.TrimEnd();
        var punct = trimmed.Length > 0 && trimmed[^1] is '.' or '!' or '?' ? trimmed[^1] : '.';
        var core = trimmed.Length > 0 && trimmed[^1] is '.' or '!' or '?'
            ? trimmed[..^1]
            : trimmed;
        var words = core.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        var bucket = new List<string>();

        void Flush(bool last)
        {
            if (bucket.Count == 0)
            {
                return;
            }

            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            var line = string.Join(' ', bucket).Trim().TrimEnd(',', ';');
            if (line.Length > 0 && char.IsLetter(line[0]))
            {
                line = char.ToUpperInvariant(line[0]) + line[1..];
            }

            sb.Append(line);
            sb.Append(last ? punct : '.');
            bucket.Clear();
        }

        for (var i = 0; i < words.Length; i++)
        {
            var word = words[i];
            bucket.Add(word.TrimEnd(','));
            var soft = word.EndsWith(',')
                || word.Equals("en", StringComparison.OrdinalIgnoreCase)
                || word.Equals("maar", StringComparison.OrdinalIgnoreCase)
                || word.Equals("zodat", StringComparison.OrdinalIgnoreCase)
                || word.Equals("want", StringComparison.OrdinalIgnoreCase);
            var more = i + 1 < words.Length;
            if (bucket.Count >= 15 || (more && soft && bucket.Count >= 8))
            {
                Flush(!more);
            }
        }

        Flush(true);
        return sb.ToString();
    }

    private static readonly Regex FirstPerson = new(
        @"\b(ik|i|eu|ja)\b|أنا",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private const string Fallback =
        "Ik ben klaar voor werk dichterbij dan je denkt. Ik zoek een ploeg waar ik mijn inzet, ritme en aandacht voor mensen kwijt kan, in gewone taal, zonder poespas.";

    private static List<string> TopLabels(IEnumerable<(string Code, string Label, int Percent)> items, int take)
        => items
            .OrderByDescending(x => x.Percent)
            .ThenBy(x => x.Code, StringComparer.Ordinal)
            .Select(x => x.Label)
            .Where(l => !CareerCompassBuilder.ContainsForbiddenJargon(l))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .ToList();

    private static string LowerInside(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var parts = value.Split(' ');
        parts[0] = KeepCaps(parts[0]);
        return string.Join(' ', parts);
    }

    private static string KeepCaps(string token)
    {
        var letters = token.Where(char.IsLetter).ToArray();
        if (letters.Length >= 2 && letters.All(char.IsUpper))
        {
            return token;
        }

        return token.Length == 0 ? token : char.ToLowerInvariant(token[0]) + token[1..];
    }

    private static string FirstPersonDirection(string label) => label.Trim().ToLowerInvariant() switch
    {
        "aanpakken met je handen" => "met mijn handen werk",
        "mensen helpen" => "mensen help",
        "netjes organiseren" => "dingen netjes organiseer",
        "uitzoeken hoe het zit" => "uitzoek hoe het zit",
        "iets moois of nieuws maken" => "iets moois of nieuws maak",
        "aanjagen en verkopen" => "zaken aanjaag en verkoop",
        _ => label
    };

    private static string JoinDutch(IReadOnlyList<string> items)
    {
        if (items.Count == 0)
        {
            return "betrouwbaar werk";
        }

        if (items.Count == 1)
        {
            return LowerInside(items[0]);
        }

        return string.Join(", ", items.Take(items.Count - 1).Select(LowerInside)) + " en " + LowerInside(items[^1]);
    }
}
