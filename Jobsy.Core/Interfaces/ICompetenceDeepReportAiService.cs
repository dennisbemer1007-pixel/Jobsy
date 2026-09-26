using Jobsy.Core.Reports.Competence;

namespace Jobsy.Core.Interfaces;

/// <summary>
/// Optional OpenAI enrichment for the competence deep-analysis report: turns the template-only
/// draft into a personal three-sentence summary and a three-step action plan. Returns null when
/// no API key is configured or the call fails, so callers must fall back to template copy.
/// </summary>
public interface ICompetenceDeepReportAiService
{
    Task<(string Summary, IReadOnlyList<(string Title, string Body)> Steps)?> TryGenerateAsync(
        CompetenceDeepReport draftWithoutAi, string? jobTitle, CancellationToken ct);
}
