using System.Text.RegularExpressions;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Careers;

/// <summary>
/// Rejects a day that invents an employer, city, wage, diploma or candidate fact, or that is not short B1.
/// A term that already sits in the ESCO source is allowed, because that is an occupation fact.
/// </summary>
public static partial class OccupationDayInLifeValidator
{
    public const int MaxWordsPerSentence = 22;

    private static readonly string[] Cities =
    [
        "amsterdam", "rotterdam", "utrecht", "eindhoven", "groningen", "tilburg",
        "almere", "breda", "nijmegen", "haarlem", "arnhem", "amersfoort", "apeldoorn",
        "maastricht", "dordrecht", "leiden", "zoetermeer", "zwolle", "delft", "venlo",
        "deventer", "westland", "den haag", "den bosch", "'s-hertogenbosch", "leeuwarden",
        "enschede", "hilversum", "amstelveen", "zaandam", "schiedam"
    ];

    private static readonly string[] Employers =
    [
        "albert heijn", "jumbo", "lidl", "aldi", "rabobank", "abn amro", "postnl",
        "coolblue", "bol.com", "ikea", "hema", "mediamarkt", "klm", "shell", "unilever",
        "philips", "asml", "booking.com"
    ];

    private static readonly string[] AlwaysEmployerPhrases =
    [
        "bij het bedrijf", "ons bedrijf", "het bedrijf waar", "mijn werkgever"
    ];

    public static bool TryValidate(OccupationDayDraft draft, OccupationDayFacts facts, out IReadOnlyList<string> reasons)
    {
        var found = new List<string>();
        var min = facts.IsThin ? 20 : 40;
        var max = facts.IsThin ? 700 : 900;
        RequireSection(found, draft.Morning, min, max);
        RequireSection(found, draft.Midday, min, max);
        RequireSection(found, draft.Afternoon, min, max);
        RequireSection(found, draft.Closing, min, max);

        var highlightMin = facts.IsThin ? 1 : 2;
        if (draft.Highlights.Count < highlightMin || draft.Highlights.Count > 4)
        {
            found.Add("highlights");
        }
        else
        {
            foreach (var line in draft.Highlights)
            {
                var words = WordCount(line);
                if (words < 2 || words > 16 || line.Length > 140)
                {
                    found.Add("highlights");
                    break;
                }

                if (LongestSentence(line) > MaxWordsPerSentence)
                {
                    found.Add("zin-te-lang");
                    break;
                }
            }
        }

        var varies = (draft.VariesNote ?? "").Trim();
        if (varies.Length < 20 || varies.Length > 400
            || !VariesPattern().IsMatch(varies))
        {
            found.Add("varies");
        }
        else if (LongestSentence(varies) > MaxWordsPerSentence)
        {
            found.Add("zin-te-lang");
        }

        var sections = new[] { draft.Morning, draft.Midday, draft.Afternoon, draft.Closing };
        for (var i = 0; i < sections.Length; i++)
        {
            for (var j = i + 1; j < sections.Length; j++)
            {
                if (Same(sections[i], sections[j]))
                {
                    found.Add("gelijk");
                }
            }
        }

        var text = string.Join(
            "\n",
            draft.Morning,
            draft.Midday,
            draft.Afternoon,
            draft.Closing,
            string.Join("\n", draft.Highlights),
            draft.VariesNote);
        var source = facts.SourceText;

        CollectInventionReasons(found, text, source);

        reasons = found.Distinct(StringComparer.Ordinal).ToList();
        return reasons.Count == 0;
    }

    /// <summary>
    /// A stored translation must keep the Dutch facts and must not add an employer, city, wage, or diploma.
    /// </summary>
    public static bool TryValidateTranslation(
        OccupationDayDraft source,
        OccupationDayDraft translated,
        out IReadOnlyList<string> reasons)
    {
        var found = new List<string>();
        RequireTranslationSection(found, translated.Morning);
        RequireTranslationSection(found, translated.Midday);
        RequireTranslationSection(found, translated.Afternoon);
        RequireTranslationSection(found, translated.Closing);
        if (translated.Highlights.Count != source.Highlights.Count || translated.Highlights.Count > 4)
        {
            found.Add("highlights");
        }
        else
        {
            foreach (var line in translated.Highlights)
            {
                if (string.IsNullOrWhiteSpace(line) || line.Length > 180)
                {
                    found.Add("highlights");
                    break;
                }
            }
        }

        var varies = (translated.VariesNote ?? "").Trim();
        if (varies.Length < 8 || varies.Length > 500)
        {
            found.Add("varies");
        }

        if (Same(source.Morning, translated.Morning)
            && Same(source.Midday, translated.Midday)
            && Same(source.Afternoon, translated.Afternoon)
            && Same(source.Closing, translated.Closing)
            && Same(source.VariesNote, translated.VariesNote))
        {
            found.Add("onvertaald");
        }

        var text = string.Join(
            "\n",
            translated.TitleNl,
            translated.Morning,
            translated.Midday,
            translated.Afternoon,
            translated.Closing,
            string.Join("\n", translated.Highlights),
            translated.VariesNote);
        var allowed = string.Join(
            "\n",
            source.TitleNl,
            source.Morning,
            source.Midday,
            source.Afternoon,
            source.Closing,
            string.Join("\n", source.Highlights),
            source.VariesNote);
        CollectInventionReasons(found, text, allowed);
        reasons = found.Distinct(StringComparer.Ordinal).ToList();
        return reasons.Count == 0;
    }

    private static void CollectInventionReasons(List<string> found, string text, string source)
    {
        if (CareerCompassBuilder.ContainsForbiddenJargon(text))
        {
            found.Add("jargon");
        }

        if (OccupationOutlook.ContainsForbiddenWord(text))
        {
            found.Add("toon");
        }

        if (WebPattern().IsMatch(text))
        {
            found.Add("web");
        }

        if (SalaryPattern().IsMatch(text) && !SourceCovers(SalaryPattern(), text, source))
        {
            found.Add("salaris");
        }

        if (DiplomaPattern().IsMatch(text) && !SourceCovers(DiplomaPattern(), text, source))
        {
            found.Add("diploma");
        }

        if (CandidatePattern().IsMatch(text))
        {
            found.Add("kandidaat");
        }

        if (PersonPattern().IsMatch(text))
        {
            found.Add("persoon");
        }

        if (LegalEntityPattern().IsMatch(text))
        {
            found.Add("werkgever");
        }

        foreach (var phrase in AlwaysEmployerPhrases)
        {
            if (text.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                found.Add("werkgever");
                break;
            }
        }

        if (HitsOutsideSource(text, source, Employers))
        {
            found.Add("werkgever");
        }

        if (HitsOutsideSource(text, source, Cities))
        {
            found.Add("stad");
        }
    }

    private static void RequireTranslationSection(List<string> found, string? value)
    {
        var text = (value ?? "").Trim();
        if (text.Length < 8)
        {
            found.Add(text.Length == 0 ? "leeg" : "te-kort");
            return;
        }

        if (text.Length > 1400)
        {
            found.Add("te-lang");
        }
    }

    private static void RequireSection(List<string> found, string? value, int min, int max)
    {
        var text = (value ?? "").Trim();
        if (text.Length < min)
        {
            found.Add(text.Length == 0 ? "leeg" : "te-kort");
            return;
        }

        if (text.Length > max)
        {
            found.Add("te-lang");
        }

        if (LongestSentence(text) > MaxWordsPerSentence)
        {
            found.Add("zin-te-lang");
        }
    }

    public static int LongestSentence(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var longest = 0;
        foreach (var sentence in SentencePattern().Split(text.Trim()))
        {
            var words = WordCount(sentence);
            if (words > longest)
            {
                longest = words;
            }
        }

        return longest;
    }

    public static int WordCount(string? text)
        => string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    private static bool Same(string? left, string? right)
        => string.Equals((left ?? "").Trim(), (right ?? "").Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool HitsOutsideSource(string text, string source, IEnumerable<string> terms)
    {
        foreach (var term in terms)
        {
            if (ContainsTerm(text, term) && !ContainsTerm(source, term))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsTerm(string text, string term)
    {
        if (term.Contains(' ', StringComparison.Ordinal) || term.Contains('.', StringComparison.Ordinal))
        {
            return text.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        return Regex.IsMatch(
            text,
            $@"\b{Regex.Escape(term)}\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static bool SourceCovers(Regex pattern, string text, string source)
    {
        foreach (Match match in pattern.Matches(text))
        {
            if (!source.Contains(match.Value, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    [GeneratedRegex(@"(?<=[.!?])\s+", RegexOptions.CultureInvariant)]
    private static partial Regex SentencePattern();

    [GeneratedRegex(@"verschil|werkgever|niet overal|per plek", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex VariesPattern();

    [GeneratedRegex(@"https?://|www\.|@", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WebPattern();

    [GeneratedRegex(@"€|\beuro(?:'s|s)?\b|\bsalaris\b|\bloon\b|\buurloon\b|\bmaandloon\b|\bjaarsalaris\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SalaryPattern();

    [GeneratedRegex(@"\b(?:diploma|hbo|mbo|universiteit|bachelor|master|pabo|havo|vmbo|vwo|opleiding)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DiplomaPattern();

    [GeneratedRegex(@"\b(?:jouw|je)\s+(?:cv|diploma|ervaring|score|profiel|competenties?|opleiding)\b|\bjij hebt\b|\bvoor jou persoonlijk\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CandidatePattern();

    [GeneratedRegex(@"\b(?:meneer|mevrouw|dhr\.|mevr\.)\s+\p{Lu}", RegexOptions.CultureInvariant)]
    private static partial Regex PersonPattern();

    [GeneratedRegex(@"\b[\p{Lu}][\p{L}\d&.'’\-]{1,40}(?:\s+[\p{Lu}][\p{L}\d&.'’\-]{1,40}){0,3}\s+(?:B\.V\.|BV|N\.V\.|NV|GmbH|Inc\.?|Ltd\.?)\b", RegexOptions.CultureInvariant)]
    private static partial Regex LegalEntityPattern();
}
