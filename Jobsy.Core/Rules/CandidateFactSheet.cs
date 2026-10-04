using System.Text;
using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>
/// The only personal facts an AI text about a candidate may use.
/// Employer names are never included. Empty work experience is explicit.
/// </summary>
public sealed class CandidateFactSheet
{
    private CandidateFactSheet(
        IReadOnlyList<string> scores,
        IReadOnlyList<string> workExperience,
        IReadOnlyList<string> education,
        IReadOnlyList<string> certificates,
        IReadOnlyList<string> allowedJobTitles,
        IReadOnlyList<string> confirmedItems,
        bool personalHistory,
        bool checkJobTitles)
    {
        Scores = scores;
        WorkExperience = workExperience;
        Education = education;
        Certificates = certificates;
        AllowedJobTitles = allowedJobTitles;
        ConfirmedItems = confirmedItems;
        PersonalHistory = personalHistory;
        CheckJobTitles = checkJobTitles;
    }

    public IReadOnlyList<string> Scores { get; }

    public IReadOnlyList<string> WorkExperience { get; }

    public IReadOnlyList<string> Education { get; }

    public IReadOnlyList<string> Certificates { get; }

    public IReadOnlyList<string> AllowedJobTitles { get; }

    public IReadOnlyList<string> ConfirmedItems { get; }

    /// <summary>Story and coach: do not talk about sectors as if they were the person's past.</summary>
    public bool PersonalHistory { get; }

    /// <summary>When true, a catalogue job title must be on the allowed list or in the work entries.</summary>
    public bool CheckJobTitles { get; }

    public bool HasWorkExperience => WorkExperience.Count > 0;

    public string ToPrompt()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Feitenlijst. Gebruik alleen deze feiten. Noem geen werkgever, sector, jaartal, diploma of ervaring die hier niet staat. Staat er werkervaring: geen, zeg dan niets over eerder werk. Ontbreekt een feit, zeg dan dat je het niet weet.");
        if (Scores.Count > 0)
        {
            sb.AppendLine("Scores:");
            foreach (var line in Scores)
            {
                sb.Append("- ").AppendLine(line);
            }
        }

        sb.AppendLine(HasWorkExperience
            ? "werkervaring: " + string.Join(", ", WorkExperience)
            : "werkervaring: geen");
        sb.AppendLine(Education.Count == 0
            ? "opleidingen: geen"
            : "opleidingen: " + string.Join(", ", Education));
        sb.AppendLine(Certificates.Count == 0
            ? "certificaten: geen"
            : "certificaten: " + string.Join(", ", Certificates));
        if (ConfirmedItems.Count > 0)
        {
            sb.AppendLine("bevestigd: " + string.Join(", ", ConfirmedItems));
        }

        if (AllowedJobTitles.Count > 0)
        {
            sb.AppendLine("Toegestane beroepen (kies ALLEEN uit deze lijst, exact deze titels):");
            foreach (var title in AllowedJobTitles)
            {
                sb.Append("- ").AppendLine(title);
            }
        }

        return sb.ToString();
    }

    public bool HistoryContains(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var corpus = string.Join(
            '\n',
            WorkExperience.Concat(Education).Concat(Certificates).Concat(ConfirmedItems));
        return corpus.Contains(token.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public bool AllowsJob(string phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
        {
            return false;
        }

        if (WorkExperience.Any(role => role.Contains(phrase, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var canonical = CareerCompassSanitize.CanonicalTitle(phrase) ?? phrase.Trim();
        return AllowedJobTitles.Any(title =>
        {
            if (title.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var allowed = CareerCompassSanitize.CanonicalTitle(title) ?? title;
            return string.Equals(allowed, canonical, StringComparison.OrdinalIgnoreCase);
        });
    }

    public static CandidateFactSheet ForWhoAmI(
        CompetencyScores competency,
        RiasecScores career,
        CulturePersonalityScores culture,
        WhoAmIProfileHighlights? profile,
        SchwartzValuesScores? values)
    {
        profile ??= WhoAmIProfileHighlights.Empty;
        var scores = new List<string>();
        foreach (var code in CompetencyTestCatalog.CategoryCodes)
        {
            scores.Add($"{WhoAmIKeywords.EverydayCompetency(code)}: {competency.Get(code)}%");
        }

        foreach (var code in CulturePersonalityCatalog.CategoryCodes)
        {
            scores.Add($"{CulturePersonalityCatalog.EverydayLabel(code)}: {culture.Get(code)}%");
        }

        if (values is { IsComplete: true })
        {
            foreach (var code in SchwartzValuesCatalog.CategoryCodes)
            {
                scores.Add($"{SchwartzValuesCatalog.EverydayLabel(code)}: {values.Get(code)}%");
            }
        }

        foreach (var code in CareerTestCatalog.RiasecCodes)
        {
            scores.Add($"{CareerCompassBuilder.TypeLabel(code)}: {career.Get(code)}%");
        }

        return new CandidateFactSheet(
            scores,
            profile.Roles,
            profile.Educations,
            profile.Certificates,
            [],
            [],
            personalHistory: true,
            checkJobTitles: true);
    }

    public static CandidateFactSheet ForCareer(
        IReadOnlyList<DeepAnalysisDomainScore> scores,
        IReadOnlyDictionary<int, int> answers)
    {
        var ordered = scores
            .OrderByDescending(s => s.Percent)
            .ThenBy(s => CareerCompassBuilder.TypeLabel(s.Domain), StringComparer.Ordinal)
            .ToList();
        var lines = new List<string>
        {
            $"test: uitgebreide beroepentest (200 unieke vragen, {answers.Count} antwoorden in de score verwerkt)"
        };
        if (ordered.Count > 0)
        {
            lines.Add("Kernfit (hoogste richtingen): " + string.Join(
                ", ",
                ordered.Take(3).Select(s => $"{CareerCompassBuilder.TypeLabel(s.Domain)} {s.Percent}%")));
        }

        foreach (var score in ordered)
        {
            lines.Add($"{CareerCompassBuilder.TypeLabel(score.Domain)}: {score.Percent}%");
        }

        var allowed = new List<string>();
        var riasec = DeepAnalysisCatalog.ToRiasecScores(scores);
        if (riasec.IsComplete)
        {
            allowed.AddRange(CareerCompassBuilder.Ranked(riasec).Take(40).Select(job => job.Title));
        }

        return new CandidateFactSheet(lines, [], [], [], allowed, [], personalHistory: false, checkJobTitles: true);
    }

    /// <summary>Prose already tied to a catalogue job list. Job titles are checked on the list itself.</summary>
    public static CandidateFactSheet ForCareerProse(IEnumerable<string>? allowedTitles = null)
        => new(
            [],
            [],
            [],
            [],
            allowedTitles?.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            ?? [],
            [],
            personalHistory: false,
            checkJobTitles: false);

    public static CandidateFactSheet Personal(
        IReadOnlyList<string>? workExperience,
        IReadOnlyList<string>? education,
        IReadOnlyList<string>? certificates,
        IReadOnlyList<string>? allowedJobTitles = null,
        IReadOnlyList<string>? scores = null,
        IReadOnlyList<string>? confirmedItems = null)
        => new(
            scores ?? [],
            workExperience ?? [],
            education ?? [],
            certificates ?? [],
            allowedJobTitles ?? [],
            confirmedItems ?? [],
            personalHistory: true,
            checkJobTitles: true);

    /// <summary>Role, confirmed years and start/end years. Never the employer name.</summary>
    public static string? FormatWorkEntry(CandidateEmployerHistoryDto entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Role))
        {
            return null;
        }

        var role = entry.Role.Trim();
        if (entry.Years is int years and > 0)
        {
            role += $" ({years} jaar)";
        }

        var start = YearOf(entry.StartMonth);
        var end = YearOf(entry.EndMonth);
        if (start is not null && end is not null)
        {
            role += $" {start}-{end}";
        }
        else if (start is not null)
        {
            role += $" vanaf {start}";
        }

        return role;
    }

    public static string FormatCertificate(CandidateCertificateDto certificate)
    {
        var name = certificate.Name.Trim();
        return certificate.Year is int year and > 0 ? $"{name} ({year})" : name;
    }

    private static string? YearOf(string? month)
    {
        if (string.IsNullOrWhiteSpace(month) || month.Length < 4)
        {
            return null;
        }

        var year = month[..4];
        return int.TryParse(year, out var value) && value is >= 1900 and <= 2100 ? year : null;
    }
}
