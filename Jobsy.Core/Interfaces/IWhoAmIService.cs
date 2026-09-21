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
    bool DiscCompleted,
    bool IsUnlocked,
    string Encouragement,
    string? Story,
    bool FromOpenAi,
    IReadOnlyList<string> Keywords,
    CompetencyScores? CompetencyScores,
    DiscScores? DiscScores,
    bool IncludeOnCv,
    DateTime? StoryGeneratedAtUtc);
