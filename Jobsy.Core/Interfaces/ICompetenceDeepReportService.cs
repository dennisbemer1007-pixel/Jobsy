using Jobsy.Core.Reports.Competence;

namespace Jobsy.Core.Interfaces;

/// <summary>
/// Builds, persists, and refreshes the paid competence deep-analysis report
/// (<see cref="CompetenceDeepReport"/>) for one candidate.
/// </summary>
public interface ICompetenceDeepReportService
{
    /// <summary>
    /// Returns the stored report, or null when the competence deep analysis is not completed
    /// or no report has been built yet. When the stored report is on an older
    /// <see cref="CompetenceDeepReportJson.CurrentReportVersion"/>, the (still usable) old report
    /// is returned and a rebuild is queued in the background.
    /// </summary>
    Task<CompetenceDeepReport?> GetStoredAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Builds a fresh report from the candidate's stored answers and persists it. When
    /// <paramref name="tryAi"/> is true, attempts an OpenAI summary/action plan first and falls
    /// back to template copy on failure or timeout.
    /// </summary>
    Task<CompetenceDeepReport> BuildAndStoreAsync(Guid userId, bool tryAi, CancellationToken ct);

    /// <summary>
    /// Retries the OpenAI summary/action plan for an already-stored template report when it is
    /// older than one hour, so a transient AI failure at completion time gets a second chance.
    /// No-op when the row is not completed, has no stored report, or already came from OpenAI.
    /// </summary>
    Task RefineAiAsync(Guid userId, CancellationToken ct);
}
