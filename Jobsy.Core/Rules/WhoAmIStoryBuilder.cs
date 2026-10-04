using System.Text;
using System.Text.RegularExpressions;

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
        bool employersEnabled = true)
    {
        var careerTop = TopLabels(
            CareerTestCatalog.RiasecCodes.Select(c => (CareerCompassBuilder.TypeLabel(c), career.Get(c))),
            2);
        var cultureTop = TopLabels(
            CulturePersonalityCatalog.CategoryCodes.Select(c => (CulturePersonalityCatalog.EverydayLabel(c), culture.Get(c))),
            2);
        var compTop = TopLabels(
            CompetencyTestCatalog.CategoryCodes.Select(c => (WhoAmIKeywords.EverydayCompetency(c), competency.Get(c))),
            2);
        var valuesTop = values is { IsComplete: true }
            ? TopLabels(
                SchwartzValuesCatalog.CategoryCodes.Select(c => (SchwartzValuesCatalog.EverydayLabel(c), values.Get(c))),
                2)
            : [];
        var keywords = WhoAmIKeywords.FromScores(competency, career, culture, values);
        profile ??= WhoAmIProfileHighlights.Empty;

        var sb = new StringBuilder();
        sb.Append("Ik ben iemand die tot zijn recht komt bij ");
        sb.Append(JoinDutch(careerTop));
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
                bits.Add("ervaring als " + JoinDutch(profile.Roles.Take(2).ToList()));
            }

            if (profile.Educations.Count > 0)
            {
                bits.Add("opleiding in " + JoinDutch(profile.Educations.Take(2).ToList()));
            }

            if (profile.Certificates.Count > 0)
            {
                bits.Add("cursussen zoals " + JoinDutch(profile.Certificates.Take(2).ToList()));
            }

            sb.Append(JoinDutch(bits));
            sb.AppendLine(".");
            sb.AppendLine();
        }

        sb.Append("Op de werkvloer is mijn kracht ");
        sb.Append(JoinDutch(compTop));
        sb.Append(". Ik zoek geen droge lijst van tests. Ik wil werk waarin ik dat elke dag laat zien. Dat doe ik dichtbij huis. In Den Haag of het Westland. Bij een ploeg die op elkaar kan bouwen.");
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

    /// <summary>Stored stories stay Dutch. Hide the employer line while that feature is off.</summary>
    public static string ForDisplay(string? story, bool employersEnabled)
    {
        if (string.IsNullOrWhiteSpace(story) || employersEnabled)
        {
            return story ?? "";
        }

        return story
            .Replace(
                "Dat zoek ik terug in cultuur en beloftes van een werkgever.",
                "Dat zoek ik terug in hoe een gewone werkdag eruitziet.",
                StringComparison.Ordinal)
            .Replace(
                "zodat een werkgever meteen voelt of we bij elkaar passen.",
                "zodat meteen duidelijk is of het werk bij me past.",
                StringComparison.Ordinal);
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

    private const string Fallback =
        "Ik ben klaar voor werk dichterbij dan je denkt. Ik zoek een ploeg waar ik mijn inzet, ritme en aandacht voor mensen kwijt kan, in gewone taal, zonder poespas.";

    private static List<string> TopLabels(IEnumerable<(string Label, int Percent)> items, int take)
        => items
            .OrderByDescending(x => x.Percent)
            .ThenBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Label)
            .Where(l => !CareerCompassBuilder.ContainsForbiddenJargon(l))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .ToList();

    private static string LowerInside(string value)
        => string.IsNullOrWhiteSpace(value)
            ? value
            : char.ToLowerInvariant(value[0]) + value[1..];

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
