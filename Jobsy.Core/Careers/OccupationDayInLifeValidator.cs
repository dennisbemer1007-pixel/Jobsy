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
        var blockDay = draft.Blocks is { Count: > 0 };
        var min = blockDay
            ? (facts.IsThin ? 12 : 24)
            : (facts.IsThin ? 20 : 40);
        var max = facts.IsThin ? 700 : 900;
        RequireSection(found, draft.Morning, min, max);
        RequireSection(found, draft.Midday, min, max);
        RequireSection(found, draft.Afternoon, min, max);
        RequireSection(found, draft.Closing, min, max);
        if (draft.Blocks is { Count: > 0 } blocks)
        {
            ValidateBlocks(found, blocks, facts.IsThin);
        }

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

        var sections = draft.Blocks is { Count: > 0 } timeline
            ? timeline.Select(block => block.Text).ToArray()
            : [draft.Morning, draft.Midday, draft.Afternoon, draft.Closing];
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
            string.Join("\n", (draft.Blocks ?? []).Select(block => block.Label + "\n" + block.Text)),
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

        if (source.Blocks is { Count: > 0 })
        {
            var translatedBlocks = translated.Blocks ?? [];
            if (translatedBlocks.Count != source.Blocks.Count
                || !translatedBlocks.Select(block => block.Key).SequenceEqual(source.Blocks.Select(block => block.Key)))
            {
                found.Add("blok");
            }
            else
            {
                foreach (var block in translatedBlocks)
                {
                    RequireTranslationSection(found, block.Text);
                    if (string.IsNullOrWhiteSpace(block.Label) || block.Label.Length > 32)
                    {
                        found.Add("blok");
                        break;
                    }
                }
            }
        }

        if (Same(source.Morning, translated.Morning)
            && Same(source.Midday, translated.Midday)
            && Same(source.Afternoon, translated.Afternoon)
            && Same(source.Closing, translated.Closing)
            && Same(source.VariesNote, translated.VariesNote)
            && SameBlocks(source.Blocks, translated.Blocks))
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
            string.Join("\n", (translated.Blocks ?? []).Select(block => block.Label + "\n" + block.Text)),
            string.Join("\n", translated.Tasks ?? []),
            string.Join("\n", translated.Skills ?? []),
            translated.VariesNote);
        var allowed = string.Join(
            "\n",
            source.TitleNl,
            source.Morning,
            source.Midday,
            source.Afternoon,
            source.Closing,
            string.Join("\n", source.Highlights),
            string.Join("\n", (source.Blocks ?? []).Select(block => block.Label + "\n" + block.Text)),
            string.Join("\n", source.Tasks ?? []),
            string.Join("\n", source.Skills ?? []),
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

    private static void ValidateBlocks(List<string> found, IReadOnlyList<OccupationDayBlock> blocks, bool thin)
    {
        var noted = 0;
        var min = thin ? OccupationDayBlocks.MinThin : OccupationDayBlocks.MinNormal;
        if (blocks.Count < min)
        {
            Note(blocks.Count == 1
                ? "blok: 1 blok, minimaal " + min
                : "blok: " + blocks.Count + " blokken, minimaal " + min);
        }
        else if (blocks.Count > OccupationDayBlocks.Max)
        {
            Note("blok: " + blocks.Count + " blokken, maximaal " + OccupationDayBlocks.Max);
        }

        var keys = new List<string>();
        var textMin = thin ? 12 : 24;
        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            var number = i + 1;
            var rawKey = (block.Key ?? "").Trim();
            var canonical = OccupationDayBlocks.IsKnown(rawKey)
                ? rawKey.ToLowerInvariant()
                : OccupationDayBlocks.CanonicalKey(rawKey, block.Label);
            if (canonical is null)
            {
                var shown = rawKey.Length == 0 ? "?" : rawKey.Length <= 24 ? rawKey : rawKey[..24];
                Note("blok " + number + ": onbekende key '" + shown + "'");
                continue;
            }

            if (keys.Contains(canonical, StringComparer.OrdinalIgnoreCase))
            {
                Note("blok: dubbele key " + canonical);
            }
            else
            {
                keys.Add(canonical);
            }

            var label = (block.Label ?? "").Trim();
            var text = (block.Text ?? "").Trim();
            if (label.Length > 32)
            {
                Note("blok " + number + ": label te lang");
            }
            else if (label.Length < 2)
            {
                Note("blok " + number + ": label te kort");
            }
            else if (LongestSentence(label) > 6)
            {
                Note("blok " + number + ": zin te lang");
            }

            if (text.Length < textMin)
            {
                Note("blok " + number + ": tekst te kort");
            }
            else if (text.Length > 700)
            {
                Note("blok " + number + ": tekst te lang");
            }
            else if (LongestSentence(text) > MaxWordsPerSentence)
            {
                Note("blok " + number + ": zin te lang");
            }
        }

        foreach (var required in new[] { "start", "morning", "afternoon" })
        {
            if (!keys.Contains(required, StringComparer.OrdinalIgnoreCase))
            {
                Note("blok: ontbreekt " + required);
            }
        }

        void Note(string reason)
        {
            if (noted >= 4 || found.Contains(reason, StringComparer.Ordinal))
            {
                return;
            }

            found.Add(reason);
            noted++;
        }
    }

    private static bool SameBlocks(IReadOnlyList<OccupationDayBlock>? left, IReadOnlyList<OccupationDayBlock>? right)
    {
        var a = left ?? [];
        var b = right ?? [];
        if (a.Count == 0 && b.Count == 0)
        {
            return true;
        }

        if (a.Count != b.Count)
        {
            return false;
        }

        for (var i = 0; i < a.Count; i++)
        {
            if (!Same(a[i].Text, b[i].Text))
            {
                return false;
            }
        }

        return true;
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
