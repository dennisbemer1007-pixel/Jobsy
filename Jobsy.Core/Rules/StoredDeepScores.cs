using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Reports.Career;
using Jobsy.Core.Reports.Competence;
using Jobsy.Core.Reports.Culture;
using Jobsy.Core.Reports.Values;

namespace Jobsy.Core.Rules;

/// <summary>
/// Scores from a completed deep test. The report's own domain scores win.
/// The quick-scan row is only a fallback when no completed deep test exists.
/// </summary>
public static class StoredDeepScores
{
    public static RiasecScores? Career(string? status, string? answersJson, string? reportJson)
    {
        if (!CandidateDeepAnalysisStatuses.IsCompleted(status))
        {
            return null;
        }

        var fromReport = CareerDeepReportBuilder.DomainScores(CareerDeepReportJson.Deserialize(reportJson));
        if (fromReport is { IsComplete: true })
        {
            return fromReport;
        }

        var answers = DeepAnalysisCatalog.ParseAnswersJson(answersJson, AssessmentKind.Career);
        if (!DeepAnalysisCatalog.IsComplete(answers, AssessmentKind.Career))
        {
            return null;
        }

        return DeepAnalysisCatalog.ToRiasecScores(
            DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Career));
    }

    public static SchwartzValuesScores? Values(string? status, string? answersJson, string? reportJson)
    {
        if (!CandidateDeepAnalysisStatuses.IsCompleted(status))
        {
            return null;
        }

        var report = ValuesDeepReportJson.Deserialize(reportJson);
        if (report is not null && HasDomains(report.Domains, SchwartzValuesCatalog.CategoryCodes))
        {
            return DeepAnalysisCatalog.ToSchwartzScores(AsDomainScores(report.Domains));
        }

        var answers = DeepAnalysisCatalog.ParseAnswersJson(answersJson, AssessmentKind.Values);
        if (!DeepAnalysisCatalog.IsComplete(answers, AssessmentKind.Values))
        {
            return null;
        }

        return DeepAnalysisCatalog.ToSchwartzScores(
            DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Values));
    }

    public static CulturePersonalityScores? Culture(string? status, string? answersJson, string? reportJson)
    {
        if (!CandidateDeepAnalysisStatuses.IsCompleted(status))
        {
            return null;
        }

        var report = CultureDeepReportJson.Deserialize(reportJson);
        if (report is not null && HasDomains(report.Domains, CulturePersonalityCatalog.CategoryCodes))
        {
            return DeepAnalysisCatalog.ToCulturePersonalityScores(AsDomainScores(report.Domains));
        }

        var answers = DeepAnalysisCatalog.ParseAnswersJson(answersJson, AssessmentKind.Culture);
        if (!DeepAnalysisCatalog.IsComplete(answers, AssessmentKind.Culture))
        {
            return null;
        }

        return DeepAnalysisCatalog.ToCulturePersonalityScores(
            DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Culture));
    }

    public static IReadOnlyList<(string Domain, int Score)> CompetenceTraits(
        string? status,
        string? answersJson,
        string? reportJson)
    {
        if (!CandidateDeepAnalysisStatuses.IsCompleted(status))
        {
            return [];
        }

        var report = CompetenceDeepReportJson.Deserialize(reportJson);
        if (report?.Traits is { Count: > 0 })
        {
            return report.Traits
                .OrderByDescending(trait => trait.Score)
                .ThenBy(trait => trait.Domain, StringComparer.Ordinal)
                .Select(trait => (trait.Domain, trait.Score))
                .ToList();
        }

        var answers = DeepAnalysisCatalog.ParseAnswersJson(answersJson, AssessmentKind.Competence);
        if (!DeepAnalysisCatalog.IsComplete(answers, AssessmentKind.Competence))
        {
            return [];
        }

        return DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Competence)
            .OrderByDescending(score => score.Percent)
            .ThenBy(score => score.Domain, StringComparer.Ordinal)
            .Select(score => (score.Domain, score.Percent))
            .ToList();
    }

    private static bool HasDomains(IReadOnlyList<DeepDomainScore> domains, IEnumerable<string> codes)
        => codes.All(code => domains.Any(domain =>
            string.Equals(domain.Domain, code, StringComparison.OrdinalIgnoreCase)));

    private static List<DeepAnalysisDomainScore> AsDomainScores(IReadOnlyList<DeepDomainScore> domains)
        => domains.Select(domain => new DeepAnalysisDomainScore(domain.Domain, domain.Score, 1)).ToList();
}
