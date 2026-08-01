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
        MockInterviewCandidateContext? candidate = null,
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
    IReadOnlyList<string> WorkTypes,
    string? RequiredDrivingLicense = null,
    string? RequiredEducation = null,
    decimal? MinHoursPerWeek = null,
    decimal? MaxHoursPerWeek = null,
    int? MinimumEmployers = null);

/// <summary>Candidate profile signals + soft gaps for vacancy-specific coaching questions.</summary>
public sealed record MockInterviewCandidateContext(
    string? AboutMe,
    IReadOnlyList<string> Educations,
    IReadOnlyList<string> DrivingLicenses,
    IReadOnlyList<string> EmployerSummaries,
    string? HoursSummary,
    IReadOnlyList<MockInterviewGap> Gaps);

public sealed record MockInterviewGap(
    string Key,
    string Summary,
    string Question,
    string EnglishQuestion);

public sealed record MockInterviewMessage(string Role, string Content);

public sealed record MockInterviewTurnResult(string Reply, bool UsedAi);
