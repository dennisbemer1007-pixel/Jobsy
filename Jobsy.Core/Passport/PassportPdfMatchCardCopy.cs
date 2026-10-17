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

    public static IReadOnlyList<CandidateMatchedVacancyDto> DistinctTop(
        IReadOnlyList<CandidateMatchedVacancyDto> matches,
        int take = 3)
    {
        var seen = new HashSet<Guid>();
        var list = new List<CandidateMatchedVacancyDto>(take);
        foreach (var match in matches)
        {
            if (match.Id == Guid.Empty || !seen.Add(match.Id))
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

    public static PassportJobFit ToJobFit(CandidateMatchedVacancyDto match, string lang)
    {
        var title = NormalizeTitle(match.Title);
        var why = BuildWhy(match, lang);
        return new PassportJobFit(title, why);
    }

    public static IReadOnlyList<PassportJobFit> ToJobFits(
        IReadOnlyList<CandidateMatchedVacancyDto> matches,
        string lang,
        int take = 3)
        => DistinctTop(matches, take).Select(m => ToJobFit(m, lang)).ToArray();

    public static PassportPdfFacts AttachEmployerVacancyCards(
        PassportPdfFacts facts,
        IReadOnlyList<CandidateMatchedVacancyDto> matches)
    {
        var lang = JobsyLanguages.Normalize(facts.Language);
        return facts with
        {
            StageVacancyMatches = ToJobFits(matches, lang).ToList()
        };
    }

    private static string NormalizeTitle(string? title)
    {
        var text = string.IsNullOrWhiteSpace(title) ? "—" : title.Trim();
        text = Regex.Replace(text, @"\s*\(\d+\)\s*$", "").Trim();
        return ClipAtWord(text, 72);
    }

    private static string BuildWhy(CandidateMatchedVacancyDto match, string lang)
    {
        foreach (var line in match.Why ?? [])
        {
            var clean = SanitizeExplainLine(line);
            if (clean is not null)
            {
                return ClipAtWord(clean, 160);
            }
        }

        if (!string.IsNullOrWhiteSpace(match.CompanyName))
        {
            return ClipAtWord(match.CompanyName.Trim(), 160);
        }

        return PassportPdfStrings.T(lang, "VacancyMatchNeutral");
    }

    internal static string? SanitizeExplainLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var text = line.Trim();
        if (text.Contains('%') || ScoreLine.IsMatch(text))
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
