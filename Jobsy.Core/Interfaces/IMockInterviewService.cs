namespace Jobsy.Core.Interfaces;

public interface IMockInterviewService
{
    /// <summary>
    /// Continues a practice interview for the given vacancy context.
    /// When history is empty, returns the recruiter's opening turn.
    /// </summary>
    Task<MockInterviewTurnResult> ContinueAsync(
        MockInterviewVacancyContext vacancy,
        IReadOnlyList<MockInterviewMessage> history,
        string? language = null,
        CancellationToken cancellationToken = default);
}

public sealed record MockInterviewVacancyContext(
    Guid VacancyId,
    string Title,
    string Description,
    string CompanyName,
    string? CompanyAddress,
    DateOnly StartDate,
    IReadOnlyList<string> RequiredTransport,
    decimal? HourlyWage,
    IReadOnlyList<string> WorkTypes);

public sealed record MockInterviewMessage(string Role, string Content);

public sealed record MockInterviewTurnResult(string Reply, bool UsedAi);
