using Jobsy.Core.Enums;

namespace Jobsy.Core.Contracts.Scholen;

public sealed record PupilSchoolOptionDto(Guid Id, string SchoolName, string City, bool ReadOnly);

public sealed record PupilClassOptionDto(
    Guid Id,
    string Label,
    string ClassName,
    SchoolLevel Level,
    int Year,
    bool ReadOnly);

public sealed record PupilLoginRequest(Guid SchoolId, Guid ClassId, string Code);

public sealed record PupilLoginResponse(
    string RedirectPath,
    Guid PupilCodeId,
    Guid ClassId,
    Guid SchoolId,
    string ClassLabel,
    string CodeDisplay,
    PupilCodeStatus Status,
    int CurrentIndex,
    int TotalItems,
    int SessionVersion);

public sealed record PupilProgressStateDto(
    Guid PupilCodeId,
    string ClassLabel,
    string CodeDisplay,
    PupilCodeStatus Status,
    int CurrentIndex,
    int TotalItems,
    int AnsweredCount,
    int PlatesShed,
    bool NewShell,
    string? CurrentItemId,
    string? CurrentWorldKey,
    IReadOnlyDictionary<string, int> Answers,
    bool WindowOpen,
    bool Completed,
    bool NeedsIsland,
    bool IslandDone,
    IReadOnlyList<string> Likes,
    IReadOnlyList<string> Dislikes,
    string? LikeOtherWord,
    string? DislikeOtherWord);

public sealed record PupilAnswerRequest(int Value);

public sealed record PupilAnswerResponse(
    int CurrentIndex,
    int AnsweredCount,
    int PlatesShed,
    bool Completed,
    string? NextItemId,
    string? NextWorldKey,
    bool NeedsIsland);

public sealed record PupilChipsRequest(
    IReadOnlyList<string>? Likes,
    IReadOnlyList<string>? Dislikes,
    string? LikeOtherWord,
    string? DislikeOtherWord);

public sealed record PupilChipsResponse(
    bool Ok,
    int CurrentIndex,
    string? NextItemId,
    string? NextWorldKey);

public sealed record PupilResultPageDto(
    string ClassLabel,
    string CodeDisplay,
    string SchoolName,
    PupilStoryViewDto Story,
    IReadOnlyList<string> Likes,
    IReadOnlyList<string> Dislikes,
    DreamJobRouteStubDto? DreamJob,
    string? DreamJobKey);

public sealed record PupilDreamJobRequest(string? Key);

public sealed record PupilDreamJobResponse(string Key, DreamJobRouteStubDto DreamJob);

public sealed record PupilErrorDto(string Error, string Message);
