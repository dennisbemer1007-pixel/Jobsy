using System.Text.RegularExpressions;
using Jobsy.Core.Careers;

namespace Jobsy.Core.Rules;

/// <summary>
/// Rejects candidate-facing AI text that invents work, employers, years, diplomas,
/// job titles outside the catalogue or result list, or markdown.
/// </summary>
public static partial class CandidateFactGuard
{
    public const string StrictAddendum =
        "STRIKT: Gebruik alleen de feitenlijst. Verzin geen werkgever, sector, jaartal, diploma of ervaring. " +
        "Staat er werkervaring: geen, zeg niets over eerder werk. " +
        "Noem alleen beroepstitels uit de toegestane lijst. Geen markdown. " +
        "Ontbreekt een feit, zeg dat je het niet weet.";

    public static string? RejectionReason(string? text, CandidateFactSheet sheet)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "empty";
        }

        if (Markdown().IsMatch(text))
        {
            return "markdown";
        }

        if (MentionsUnknownWork(text, sheet))
        {
            return "invented-work";
        }

        if (MentionsUnknownDiploma(text, sheet))
        {
            return "unknown-diploma";
        }

        if (MentionsUnknownYear(text, sheet))
        {
            return "unknown-year";
        }

        if (sheet.CheckJobTitles && MentionsUnknownJob(text, sheet))
        {
            return "unknown-job";
        }

        return null;
    }

    public static string WithoutInventedHistory(string? text, CandidateFactSheet sheet, string fallback)
        => RejectionReason(text, sheet) is null ? text!.Trim() : fallback;

    /// <summary>True when every title maps onto the labour-market catalogue.</summary>
    public static bool IsCatalogueTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        var parts = TitleParts(title);
        return parts.Count > 0 && parts.TrueForAll(part => CareerCompassSanitize.CanonicalTitle(part) is not null);
    }

    public static string? CompassRejection(string? rawJson, CandidateFactSheet sheet)
    {
        var dto = CareerCompassJson.TryRead(rawJson);
        if (dto is null)
        {
            return "unreadable";
        }

        var titles = (dto.SuperMatches ?? []).Concat(dto.StrongChoices ?? []).Concat(dto.Broadening ?? [])
            .Select(item => item.Title)
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .Cast<string>()
            .ToList();
        if (titles.Count == 0)
        {
            return "no-jobs";
        }

        foreach (var title in titles)
        {
            foreach (var part in TitleParts(title))
            {
                var canonical = CareerCompassSanitize.CanonicalTitle(part);
                if (canonical is null)
                {
                    return "unknown-job";
                }

                if (sheet.AllowedJobTitles.Count > 0 && !sheet.AllowsJob(canonical) && !sheet.AllowsJob(part))
                {
                    return "unknown-job";
                }
            }
        }

        var prose = new List<string>();
        if (dto.Strengths is not null)
        {
            prose.AddRange(dto.Strengths);
        }

        if (dto.PracticalNotes is not null)
        {
            prose.AddRange(dto.PracticalNotes);
        }

        foreach (var item in (dto.SuperMatches ?? []).Concat(dto.StrongChoices ?? []).Concat(dto.Broadening ?? []))
        {
            if (!string.IsNullOrWhiteSpace(item.Why))
            {
                prose.Add(item.Why);
            }
        }

        var proseSheet = CandidateFactSheet.ForCareerProse(sheet.AllowedJobTitles);
        foreach (var line in prose)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var reason = RejectionReason(line, proseSheet);
            if (reason is not null)
            {
                return reason;
            }
        }

        return null;
    }

    private static bool MentionsUnknownWork(string text, CandidateFactSheet sheet)
    {
        foreach (var sentence in SplitSentences(text))
        {
            if (Negated().IsMatch(sentence))
            {
                continue;
            }

            var past = PastWork().IsMatch(sentence);
            var sector = SectorWord(sentence);
            var sectorClaim = sector is not null && SectorPhrase().IsMatch(sentence);
            if (!past && !sectorClaim)
            {
                continue;
            }

            if (past && !sheet.HasWorkExperience)
            {
                return true;
            }

            if (past && sheet.HasWorkExperience && !SentenceUsesKnownRole(sentence, sheet))
            {
                return true;
            }

            if (sectorClaim && sheet.PersonalHistory && sector is not null && !sheet.HistoryContains(sector))
            {
                return true;
            }
        }

        return false;
    }

    private static bool SentenceUsesKnownRole(string sentence, CandidateFactSheet sheet)
    {
        foreach (var role in sheet.WorkExperience)
        {
            foreach (var word in role.Split([' ', ',', '(', ')', '-'], StringSplitOptions.RemoveEmptyEntries))
            {
                if (word.Length >= 4 && sentence.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool MentionsUnknownDiploma(string text, CandidateFactSheet sheet)
    {
        foreach (Match match in Diploma().Matches(text))
        {
            if (!sheet.HistoryContains(match.Value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MentionsUnknownYear(string text, CandidateFactSheet sheet)
    {
        foreach (Match match in YearClaim().Matches(text))
        {
            if (!sheet.HistoryContains(match.Value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MentionsUnknownJob(string text, CandidateFactSheet sheet)
    {
        foreach (var phrase in JobPhrases.Value)
        {
            if (!ContainsPhrase(text, phrase))
            {
                continue;
            }

            if (!sheet.AllowsJob(phrase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsPhrase(string text, string phrase)
    {
        var index = 0;
        while ((index = text.IndexOf(phrase, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var before = index == 0 || !char.IsLetterOrDigit(text[index - 1]);
            var end = index + phrase.Length;
            var after = end >= text.Length || !char.IsLetterOrDigit(text[end]);
            if (before && after)
            {
                return true;
            }

            index = end;
        }

        return false;
    }

    private static string? SectorWord(string sentence)
    {
        var match = SectorPhrase().Match(sentence);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static List<string> TitleParts(string raw)
    {
        var trimmed = raw.Trim().Trim('*', '"', '\'', '«', '»');
        if (trimmed.Length == 0)
        {
            return [];
        }

        if (CareerCompassSanitize.CanonicalTitle(trimmed) is not null)
        {
            return [trimmed];
        }

        var parts = ComboSplit().Split(trimmed);
        if (parts.Length <= 1)
        {
            return [trimmed];
        }

        return parts
            .Select(part => part.Trim().Trim('*', '"', '\''))
            .Where(part => part.Length > 0)
            .ToList();
    }

    private static List<string> SplitSentences(string text)
        => Regex.Split(text, @"(?<=[\.!\?\n])")
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .ToList();

    private static readonly Lazy<string[]> JobPhrases = new(BuildJobPhrases);

    private static string[] BuildJobPhrases()
    {
        var phrases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Add(string? phrase, int minLength)
        {
            if (string.IsNullOrWhiteSpace(phrase))
            {
                return;
            }

            var trimmed = phrase.Trim();
            if (trimmed.Length >= minLength)
            {
                phrases.Add(trimmed);
            }
        }

        foreach (var occupation in CareerCompassBuilder.Occupations)
        {
            Add(occupation.Title, 3);
            foreach (var part in occupation.Title.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                Add(part, 4);
            }
        }

        foreach (var entry in CareerDreamCatalog.All)
        {
            Add(entry.Title, 5);
            foreach (var alias in entry.Aliases)
            {
                Add(alias, 8);
            }
        }

        return phrases.OrderByDescending(phrase => phrase.Length).ToArray();
    }

    [GeneratedRegex(@"\*\*|__|(?m)^[ \t]*[-*]\s+\S|(?m)^#{1,6}\s|\[[^\]]+\]\([^)]+\)", RegexOptions.CultureInvariant)]
    private static partial Regex Markdown();

    [GeneratedRegex(@"jarenlang|\bgewerkt\b|ervaring\s+in\s+de|ervaring\s+met|ervaring\s+als|\bgestaan\b|in\s+dienst\s+bij|mijn\s+werkgever", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PastWork();

    [GeneratedRegex(@"\bin\s+de\s+(zorg|bouw|horeca|kas|magazijn|keuken|logistiek|onderwijs|transport|techniek|winkel|kantoor|landbouw|schoonmaak)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SectorPhrase();

    [GeneratedRegex(@"\b(diploma|mbo|hbo|vwo|havo|vmbo|bachelor|master|universiteit)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Diploma();

    [GeneratedRegex(@"jarenlang|\b\d{1,2}\s+jaar\b|\b(?:19|20)\d{2}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex YearClaim();

    [GeneratedRegex(@"\b(geen|niet|nooit|weet ik niet|staat niet|do not know|don't know)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Negated();

    [GeneratedRegex(@"\s+of\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ComboSplit();
}
