using Jobsy.Core.Enums;

namespace Jobsy.Core.Contracts.Scholen;

public sealed record PupilSchoolOptionDto(Guid Id, string SchoolName, string City, bool ReadOnly);

public sealed record PupilClassOptionDto(
    Guid Id,
    string Label,
    string ClassName,
    SchoolLevel Level,
    int Year,
    bool ReadOnly,
    PupilQuestionSet QuestionSet = PupilQuestionSet.Vo);

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
    string? DislikeOtherWord,
    PupilQuestionSet QuestionSet = PupilQuestionSet.Vo,
    string NextStep = "question",
    string? NextPuzzleKey = null,
    string? JourneyThemeKey = null);

public sealed record PupilJourneyThemeRequest(string Key);

public sealed record PupilJourneyThemeResponse(string JourneyThemeKey, string RedirectPath);

public sealed record PupilAnswerRequest(int Value);

public sealed record PupilAnswerResponse(
    int CurrentIndex,
    int AnsweredCount,
    int PlatesShed,
    bool Completed,
    string? NextItemId,
    string? NextWorldKey,
    bool NeedsIsland,
    bool ResultPending = false,
    string NextStep = "question",
    string? NextPuzzleKey = null);

public sealed record PupilChipsRequest(
    IReadOnlyList<string>? Likes,
    IReadOnlyList<string>? Dislikes,
    string? LikeOtherWord,
    string? DislikeOtherWord,
    bool FinalizeIsland = true);

public sealed record PupilChipsResponse(
    bool Ok,
    int CurrentIndex,
    string? NextItemId,
    string? NextWorldKey,
    string NextStep = "question",
    string? NextPuzzleKey = null);

public sealed record PupilPuzzleRequest(string Status = "done");

public sealed record PupilPuzzleResponse(
    bool Ok,
    int CurrentIndex,
    string? NextItemId,
    string? NextWorldKey,
    bool NeedsIsland,
    string NextStep = "question",
    string? NextPuzzleKey = null);

public sealed record PupilErrorDto(
    string Error,
    string Message,
    string? NextStep = null,
    string? NextPuzzleKey = null,
    PupilQuestionSet? QuestionSet = null);

public sealed record PupilResultPageDto(
    string ClassLabel,
    string CodeDisplay,
    string SchoolName,
    PupilStoryViewDto Story,
    IReadOnlyList<string> Likes,
    IReadOnlyList<string> Dislikes,
    DreamJobRouteStubDto? DreamJob,
    string? DreamJobKey,
    int TotalItems = 60,
    PupilQuestionSet QuestionSet = PupilQuestionSet.Vo);

public sealed record PupilDreamJobRequest(string? Key, string? DisplayTitle = null);

public sealed record PupilDreamJobResponse(string Key, DreamJobRouteStubDto DreamJob);

public sealed record PupilVoPassportDto(
    bool Enabled,
    IReadOnlyList<string> FavoriteSubjects,
    string? ProfileKey,
    IReadOnlyList<string> SideJobAvailability,
    string? TravelModeKey,
    string? OwnWords,
    IReadOnlyList<string> LearningGoals);

public sealed record PupilVoPassportSaveRequest(
    IReadOnlyList<string>? FavoriteSubjects,
    string? ProfileKey,
    IReadOnlyList<string>? SideJobAvailability,
    string? TravelModeKey,
    string? OwnWords,
    IReadOnlyList<string>? LearningGoals);

public sealed record ParentPassportCodeRequest(string Code);

public sealed record ParentPassportErrorDto(string Code, string Message);

public sealed record ParentPassportSessionDto(Guid PupilCodeId, long AccessExpiresUnix);
