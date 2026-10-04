using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;

namespace Jobsy.Core.Contracts.Scholen;

public sealed record TeacherAssignedClassDto(
    Guid Id,
    string ClassName,
    SchoolLevel Level,
    int Year,
    int SchoolYearStart,
    string SchoolYearLabel,
    PupilQuestionSet QuestionSet);

public sealed record TeacherClassOverviewDto(
    Guid ClassId,
    string ClassName,
    SchoolLevel Level,
    int Year,
    string SchoolYearLabel,
    int CodeCount,
    int CompletedCount,
    int InProgressCount,
    int NotStartedCount,
    double CompletedPercent,
    /// <summary>Average minutes once at least one finished code has a duration; otherwise null.</summary>
    int? AverageMinutes,
    TestWindowState TestWindow,
    DateOnly? TestWindowClosesOn,
    bool ParentalInfoConfirmed,
    DateTime? LoginPausedUntilUtc,
    IReadOnlyList<TeacherCodeRowDto> CodesPreview,
    TeacherGroupInsightsDto GroupInsights,
    PupilQuestionSet QuestionSet,
    string? RetentionBanner = null);

public sealed record TeacherCodeRowDto(
    Guid Id,
    int Number,
    string DisplayCode,
    PupilCodeStatus Status,
    int ProgressCurrent,
    int ProgressTotal,
    DateTime? LastSeenAtUtc);

public sealed record TeacherGroupInsightsDto(
    bool Visible,
    int CompletedCount,
    IReadOnlyList<RiasecBarDto> RiasecBars,
    IReadOnlyList<NamedCountDto> TopValues,
    IReadOnlyList<NamedCountDto> DreamJobs,
    IReadOnlyList<NamedCountDto> TopCultures,
    IReadOnlyList<NamedCountDto> CompetenceBands,
    IReadOnlyList<string> DiscussionPromptKeys,
    PupilQuestionSet QuestionSet,
    int UndecidedDreamJobCount = 0,
    int NotFilledDreamJobCount = 0);

public sealed record RiasecBarDto(string Letter, string KidLabelKey, int Count);

public sealed record TeacherDreamJobsDto(
    bool Visible,
    int CompletedCount,
    IReadOnlyList<NamedCountDto> Jobs,
    int UndecidedCount,
    int NotFilledCount = 0);

public sealed record TeacherCodeDetailDto(
    Guid CodeId,
    Guid ClassId,
    string DisplayCode,
    string ClassName,
    PupilCodeStatus Status,
    int ProgressCurrent,
    int ProgressTotal,
    DateTime? CompletedAtUtc,
    int? DurationMinutes,
    PupilStoryViewDto? Story,
    IReadOnlyList<string> Likes,
    IReadOnlyList<string> Dislikes,
    string? LikeOtherWord,
    string? DislikeOtherWord,
    IReadOnlyList<string> ConversationStarterKeys,
    DreamJobRouteStubDto? DreamJob,
    bool PdfAvailable,
    bool ResultPending = false);

/// <summary>Rendered "Dit ben jij" story (text regenerated from templates).</summary>
public sealed record PupilStoryViewDto(
    string Title,
    string Body,
    IReadOnlyList<PupilStoryTileDto> Tiles,
    IReadOnlyList<string> JobIdeas,
    IReadOnlyList<string> LikeChipKeys);

public sealed record PupilStoryTileDto(string ModelKey, string KidLabel, string Explanation);

/// <summary>Droombaan fit + school route (fixed templates, no AI/links/vacancies).</summary>
public sealed record DreamJobRouteStubDto(
    string? JobKey,
    string? JobTitle,
    int HaveCount,
    int TotalCount,
    IReadOnlyList<string> HaveItems,
    IReadOnlyList<string> LearnItems,
    IReadOnlyList<string> RouteSteps,
    string? Encouragement = null,
    string? AltRoute = null);
