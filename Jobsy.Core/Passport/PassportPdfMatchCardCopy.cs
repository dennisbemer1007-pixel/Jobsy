using System.Text.RegularExpressions;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;

namespace Jobsy.Core.Passport;

/// <summary>Employer-facing paspoort vacancy cards: no scores, no match-engine copy, deduped vacancies.</summary>
public static class PassportPdfMatchCardCopy
{
    private static readonly Regex ScoreLine = new(
        @"\d+\s*%|scoor|score|Past goed|matchscore|RoleFit",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex EngineCopyLine = new(
        @"Wie ben ik|Uit je tests|Jouw sterke|sterke punten|probleemoplossen|matchscore|Overdraagbare ervaring|past bij wat deze baan",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static IReadOnlyList<CandidateMatchedVacancyDto> DistinctTop(
        IReadOnlyList<CandidateMatchedVacancyDto> matches,
        int take = 3)
    {
        var seenIds = new HashSet<Guid>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<CandidateMatchedVacancyDto>(take);
        foreach (var match in matches)
        {
            if (match.Id == Guid.Empty || !seenIds.Add(match.Id))
            {
                continue;
            }

            var key = DedupeKey(match);
            if (!seenKeys.Add(key))
            {
                continue;
            }

            list.Add(match);
            if (list.Count >= take)
            {
                break;
            }
        }

        return list;
    }

    public static PassportJobFit ToJobFit(
        CandidateMatchedVacancyDto match,
        string lang,
        int? maxTravelMinutes = null)
    {
        var title = NormalizeTitle(match.Title);
        var why = BuildFactualWhy(match, lang, maxTravelMinutes);
        return new PassportJobFit(title, why);
    }

    public static IReadOnlyList<PassportJobFit> ToJobFits(
        IReadOnlyList<CandidateMatchedVacancyDto> matches,
        string lang,
        int? maxTravelMinutes = null,
        int take = 3)
        => DistinctTop(matches, take)
            .Select(m => ToJobFit(m, lang, maxTravelMinutes))
            .ToArray();

    public static PassportPdfFacts AttachEmployerVacancyCards(
        PassportPdfFacts facts,
        IReadOnlyList<CandidateMatchedVacancyDto> matches)
    {
        var lang = JobsyLanguages.Normalize(facts.Language);
        return facts with
        {
            StageVacancyMatches = ToJobFits(matches, lang, facts.MaxTravelMinutes).ToList()
        };
    }

    internal static string DedupeKey(CandidateMatchedVacancyDto match)
    {
        var title = NormalizeTitleForDedupe(match.Title);
        var company = (match.CompanyName ?? string.Empty).Trim();
        return $"{title}|{company}".ToLowerInvariant();
    }

    private static string NormalizeTitleForDedupe(string? title)
    {
        var text = string.IsNullOrWhiteSpace(title) ? "—" : title.Trim();
        text = Regex.Replace(text, @"\s*\(\d+\)\s*$", "").Trim();
        return text;
    }

    private static string NormalizeTitle(string? title)
        => ClipAtWord(NormalizeTitleForDedupe(title), 72);

    private static string BuildFactualWhy(
        CandidateMatchedVacancyDto match,
        string lang,
        int? maxTravelMinutes)
    {
        _ = match.Why;
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(match.CompanyName))
        {
            parts.Add(match.CompanyName.Trim());
        }

        var place = PlaceFromTitle(match.Title);
        if (!string.IsNullOrWhiteSpace(place))
        {
            parts.Add(place);
        }

        if (maxTravelMinutes is > 0 and <= 300)
        {
            parts.Add(PassportPdfStrings.F(lang, "MaxTravel", maxTravelMinutes.Value));
        }

        if (parts.Count == 0)
        {
            return PassportPdfStrings.T(lang, "VacancyMatchNeutral");
        }

        return ClipAtWord(string.Join(" · ", parts), 160);
    }

    internal static string? PlaceFromTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var match = Regex.Match(title.Trim(), @"\bin\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            return null;
        }

        return ClipAtWord(match.Groups[1].Value.Trim(), 80);
    }

    internal static string? SanitizeExplainLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var text = line.Trim();
        if (text.Contains('%') || ScoreLine.IsMatch(text) || EngineCopyLine.IsMatch(text))
        {
            return null;
        }

        if (text.EndsWith('…') || text.Contains("...", StringComparison.Ordinal))
        {
            return null;
        }

        return text;
    }

    internal static string ClipAtWord(string text, int max)
    {
        if (text.Length <= max)
        {
            return text;
        }

        var slice = text[..max];
        var lastSpace = slice.LastIndexOf(' ');
        if (lastSpace > max / 2)
        {
            slice = slice[..lastSpace];
        }

        return slice.TrimEnd(',', '.', ';', ':');
    }
}
