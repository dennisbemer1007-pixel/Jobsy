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
        bool checkJobTitles,
        IReadOnlyList<string>? directionLabels = null,
        string? homeCity = null)
    {
        Scores = scores;
        WorkExperience = workExperience;
        Education = education;
        Certificates = certificates;
        AllowedJobTitles = allowedJobTitles;
        ConfirmedItems = confirmedItems;
        PersonalHistory = personalHistory;
        CheckJobTitles = checkJobTitles;
        DirectionLabels = directionLabels ?? [];
        HomeCity = string.IsNullOrWhiteSpace(homeCity) ? null : homeCity.Trim();
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

    /// <summary>Top direction labels a compass "why" must name. Empty skips that check.</summary>
    public IReadOnlyList<string> DirectionLabels { get; }

    /// <summary>City from the home address. Empty means the story must not name a place.</summary>
    public string? HomeCity { get; }

    /// <summary>Exact outlook sentences the coach may quote. Empty means no outlook claim is allowed.</summary>
    public IReadOnlyList<string> OutlookLines { get; private set; } = [];

    /// <summary>Stored honest-advice text the coach may quote. Empty means no parallel advice is allowed.</summary>
    public IReadOnlyList<string> HonestAdviceLines { get; private set; } = [];

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
        sb.AppendLine(string.IsNullOrWhiteSpace(HomeCity)
            ? "woonplaats: geen"
            : "woonplaats: " + HomeCity);
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

        if (OutlookLines.Count > 0)
        {
            sb.AppendLine("Vooruitblik (citeer alleen deze zinnen, verzin geen andere zin over 2030 of AI):");
            foreach (var line in OutlookLines)
            {
                sb.Append("- ").AppendLine(line);
            }
        }

        if (HonestAdviceLines.Count > 0)
        {
            sb.AppendLine("Eerlijk advies (citeer alleen deze tekst, verzin geen ander advies over het beroep of over AI):");
            foreach (var line in HonestAdviceLines)
            {
                sb.Append("- ").AppendLine(line);
            }
        }

        return sb.ToString();
    }

    public void RememberOutlook(IEnumerable<string>? lines)
    {
        OutlookLines = (lines ?? [])
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public void RememberHonestAdvice(IEnumerable<string>? lines)
    {
        HonestAdviceLines = (lines ?? [])
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public bool HistoryContains(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var corpus = string.Join(
            '\n',
            WorkExperience.Concat(Education).Concat(Certificates).Concat(ConfirmedItems).Concat(Scores)
                .Concat(OutlookLines)
                .Append(HomeCity ?? ""));
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
        foreach (var code in CompetencyTestCatalog.QuickScanCategories)
        {
            AddScore(scores, FactLabel(WhoAmIKeywords.EverydayCompetency(code), code), competency.Get(code));
        }

        foreach (var code in CulturePersonalityCatalog.CategoryCodes)
        {
            AddScore(scores, FactLabel(CulturePersonalityCatalog.EverydayLabel(code), "werksfeer"), culture.Get(code));
        }

        if (values is { IsComplete: true })
        {
            foreach (var code in SchwartzValuesCatalog.CategoryCodes)
            {
                AddScore(scores, FactLabel(SchwartzValuesCatalog.EverydayLabel(code), "waarden"), values.Get(code));
            }
        }

        foreach (var code in CareerTestCatalog.RiasecCodes)
        {
            AddScore(scores, FactLabel(CareerCompassBuilder.TypeLabel(code), "richting"), career.Get(code));
        }

        var allowed = career.IsComplete
            ? CareerCompassBuilder.Ranked(career).Take(15).Select(job => job.Title).ToList()
            : new List<string>();

        return new CandidateFactSheet(
            scores,
            profile.Roles,
            profile.Educations,
            profile.Certificates,
            allowed,
            [],
            personalHistory: true,
            checkJobTitles: allowed.Count > 0,
            homeCity: profile.HomeCity);
    }

    /// <summary>Same everyday words stay unique when two tests share a label.</summary>
    internal static string FactLabel(string label, string source)
    {
        if (string.Equals(label, "Nieuwe dingen proberen", StringComparison.OrdinalIgnoreCase)
            && string.Equals(source, "werksfeer", StringComparison.OrdinalIgnoreCase))
        {
            return "Vernieuwen in de werksfeer";
        }

        if (string.Equals(label, "Zelf kiezen en uitdaging", StringComparison.OrdinalIgnoreCase)
            && source is "waarden" or "richting")
        {
            return source == "waarden" ? "Zelf kiezen wat belangrijk is" : label;
        }

        if (string.Equals(source, CulturePersonalityCatalog.Autonomy, StringComparison.OrdinalIgnoreCase)
            || (string.Equals(label, "Zelf kiezen en uitdaging", StringComparison.OrdinalIgnoreCase)
                && string.Equals(source, "werksfeer", StringComparison.OrdinalIgnoreCase)))
        {
            return "Zelfstandig je dag indelen";
        }

        return label;
    }

    private static void AddScore(List<string> scores, string label, int percent)
        => scores.Add($"{label}: {percent}% ({ScoreBand(percent)})");

    /// <summary>laag / gemiddeld / hoog. Under 50 is below average.</summary>
    public static string ScoreBand(int percent)
        => percent < 50 ? "laag" : percent < 70 ? "gemiddeld" : "hoog";

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
        var directions = ordered.Take(3).Select(s => CareerCompassBuilder.TypeLabel(s.Domain)).ToList();
        var riasec = DeepAnalysisCatalog.ToRiasecScores(scores);
        if (riasec.IsComplete)
        {
            lines.Add("Berekende aansluiting (dit percentage ligt vast, verzin geen ander cijfer).");
            lines.Add("Elke why-zin noemt de richting achter de pijl, bijvoorbeeld: Chauffeur → Aanpakken met je handen.");
            var outlook = new List<string>();
            var adviceLines = new List<string>();
            foreach (var job in CareerCompassBuilder.Listed(riasec))
            {
                allowed.Add(job.Title);
                var direction = CareerCompassBuilder.TypeLabel(CareerCompassBuilder.PrimaryCode(job.Title));
                lines.Add($"{job.Title} → {direction}: {CareerCompassBuilder.FormatPercent(job.Percent)}%");
                var sourced = Jobsy.Core.Careers.OccupationOutlook.Shared.Get(job.EscoId);
                if (!string.IsNullOrWhiteSpace(sourced.DemandLine))
                {
                    outlook.Add(sourced.DemandLine);
                }

                if (!string.IsNullOrWhiteSpace(sourced.AiLine))
                {
                    outlook.Add(sourced.AiLine);
                }

                var advice = Jobsy.Core.Careers.HonestAdviceService.Shared.Get(job.EscoId);
                if (advice is not null)
                {
                    adviceLines.Add(advice.Text);
                }
            }

            var sheet = new CandidateFactSheet(
                lines,
                [],
                [],
                [],
                allowed,
                [],
                personalHistory: false,
                checkJobTitles: true,
                directionLabels: directions);
            sheet.RememberOutlook(outlook);
            sheet.RememberHonestAdvice(adviceLines);
            return sheet;
        }

        return new CandidateFactSheet(
            lines,
            [],
            [],
            [],
            allowed,
            [],
            personalHistory: false,
            checkJobTitles: true,
            directionLabels: directions);
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
        IReadOnlyList<string>? confirmedItems = null,
        bool checkJobTitles = true,
        string? homeCity = null)
        => new(
            scores ?? [],
            workExperience ?? [],
            education ?? [],
            certificates ?? [],
            allowedJobTitles ?? [],
            confirmedItems ?? [],
            personalHistory: true,
            checkJobTitles: checkJobTitles,
            homeCity: homeCity);

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
