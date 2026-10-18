using System.Globalization;
using System.Text;
using Jobsy.Core.Careers;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Models;

namespace Jobsy.Web.Onboarding;

public sealed record OnboardingDreamSuggestItem(string Title, string Reason);

/// <summary>Dream-job cards for onboarding step 6 from profile + nearby signal, with sector fallback.</summary>
public static class OnboardingDreamSuggestionPicker
{
    private static readonly CultureInfo Nl = CultureInfo.GetCultureInfo("nl-NL");

    private static readonly (string Title, string Reason)[] SectorFallback =
    [
        ("verpleegkundige", "Veel vraag in de zorg"),
        ("monteur", "Techniek blijft groeien"),
        ("magazijnmedewerker", "Logistiek zoekt mensen"),
        ("horecamedewerker", "Horeca dichtbij en afwisselend"),
        ("administratief medewerker", "Kantoorwerk in allerlei sectoren"),
        ("leerkracht basisonderwijs", "Onderwijs zoekt enthousiaste mensen"),
    ];

    public static IReadOnlyList<OnboardingDreamSuggestItem> BuildFromProfile(
        OnboardingProfileDraft draft,
        IEnumerable<string> nearbyVacancyTitles,
        int max = 4)
    {
        var list = new List<OnboardingDreamSuggestItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void TryAdd(string? rawTitle, string reason, bool fromVacancy = false)
        {
            if (list.Count >= max)
            {
                return;
            }

            var title = fromVacancy
                ? FormatProfessionFromVacancy(rawTitle)
                : FormatProfession(rawTitle);
            if (string.IsNullOrWhiteSpace(title) || !seen.Add(title))
            {
                return;
            }

            list.Add(new OnboardingDreamSuggestItem(title, reason));
        }

        foreach (var job in draft.Jobs)
        {
            if (string.IsNullOrWhiteSpace(job.Role))
            {
                continue;
            }

            var role = FormatProfession(job.Role);
            var roleLower = role.ToLower(Nl);
            TryAdd(role, $"Sluit aan bij je werk als {roleLower}");
        }

        if (!string.IsNullOrWhiteSpace(draft.Education))
        {
            foreach (var (title, reason) in EducationDreamHints(draft.Education.Trim()))
            {
                TryAdd(title, reason);
            }
        }

        foreach (var vacancyTitle in nearbyVacancyTitles)
        {
            TryAdd(vacancyTitle, "Komt voor in vacatures bij jou in de buurt", fromVacancy: true);
        }

        foreach (var role in draft.Roles)
        {
            TryAdd(role, "Sluit aan bij wat je al doet");
        }

        foreach (var hobby in draft.Hobbies.Concat(draft.HobbyFreeText))
        {
            if (string.IsNullOrWhiteSpace(hobby))
            {
                continue;
            }

            foreach (var hit in OccupationCatalog.Shared.Search(hobby, 3))
            {
                if (LooksLikeDisplayOccupation(hit.Title))
                {
                    TryAdd(hit.Title, $"Past bij je interesse in {hobby.ToLower(Nl)}");
                }
            }
        }

        foreach (var (title, reason) in SectorFallback)
        {
            TryAdd(title, reason);
            if (list.Count >= max)
            {
                break;
            }
        }

        return list;
    }

    public static IReadOnlyList<string> BuildRegionalTitles(IEnumerable<string> nearbyVacancyTitles, int max = 4)
    {
        return nearbyVacancyTitles
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .Select(FormatRegionalVacancyLine)
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .ToList();
    }

    internal static string FormatProfessionFromVacancy(string? raw)
        => FormatProfession(ExtractProfessionToken(raw));

    internal static string FormatRegionalVacancyLine(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "";
        }

        var words = raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return "";
        }

        if (words.Length >= 3)
        {
            return FormatProfession(words[0]);
        }

        return FormatProfession(string.Join(' ', words));
    }

    private static string ExtractProfessionToken(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "";
        }

        var words = raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length switch
        {
            0 => "",
            >= 3 => words[0],
            _ => string.Join(' ', words),
        };
    }

    private static string FormatProfession(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "";
        }

        var text = CollapseWhitespace(raw.Trim().Replace('-', ' ')).ToLower(Nl);
        text = NormalizeHrAdviseur(text);
        if (text.Length == 0)
        {
            return "";
        }

        return char.ToUpper(text[0], Nl) + text[1..];
    }

    private static string NormalizeHrAdviseur(string text)
    {
        if (text is "hr adviseur" or "hr-adviseur")
        {
            return "HR-adviseur";
        }

        return text;
    }

    private static bool LooksLikeDisplayOccupation(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        var t = title.Trim();
        if (t.Length > 48 || t.Contains("hoger onderwijs", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (t.Contains('-') && !t.Contains(' '))
        {
            return false;
        }

        return true;
    }

    private static IEnumerable<(string Title, string Reason)> EducationDreamHints(string education)
    {
        var key = education.ToUpperInvariant();
        if (key.Contains("HBO", StringComparison.Ordinal))
        {
            yield return ("accountmanager", "Past bij je HBO-niveau");
            yield return ("HR-adviseur", "Veel kantoorbanen op HBO-niveau");
        }
        else if (key.Contains("MBO", StringComparison.Ordinal))
        {
            yield return ("elektromonteur", "Techniek past goed bij MBO");
            yield return ("verkoopmedewerker", "Veel MBO-banen in de regio");
        }
        else if (key.Contains("WO", StringComparison.Ordinal) || key.Contains("UNIVERS", StringComparison.Ordinal))
        {
            yield return ("beleidsadviseur", "Past bij academisch niveau");
        }
        else if (key.Contains("VWO", StringComparison.Ordinal))
        {
            yield return ("verpleegkundige", "Populair na VWO richting zorg");
        }
    }

    private static string CollapseWhitespace(string value)
    {
        var sb = new StringBuilder(value.Length);
        var space = false;
        foreach (var ch in value)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!space)
                {
                    sb.Append(' ');
                    space = true;
                }
            }
            else
            {
                sb.Append(ch);
                space = false;
            }
        }

        return sb.ToString().Trim();
    }
}
