using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface IWhoAmIService
{
    Task<WhoAmIStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<WhoAmIStateDto> SetIncludeOnCvAsync(
        Guid userId,
        bool includeOnCv,
        CancellationToken cancellationToken = default);

    Task<LobsyCvWhoAmI?> GetCvAttachmentAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record WhoAmIStateDto(
    bool ProfileFilled,
    bool CompetencyCompleted,
    bool CareerCompleted,
    bool CultureCompleted,
    bool IsUnlocked,
    string Encouragement,
    string? Story,
    bool FromOpenAi,
    IReadOnlyList<string> Keywords,
    CompetencyScores? CompetencyScores,
    CulturePersonalityScores? CultureScores,
    RiasecScores? CareerScores,
    bool IncludeOnCv,
    DateTime? StoryGeneratedAtUtc,
    IReadOnlyList<WhoAmIEmployerDto> Employers,
    IReadOnlyList<string> Educations,
    IReadOnlyList<string> Certificates,
    string InsightsStatus = InsightsStatuses.Ready);

public sealed record WhoAmIEmployerDto(string EmployerName, string? Role);
