using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;

namespace Jobsy.Core.Contracts.Scholen;

public sealed record PupilMboHboOverviewDto(
    string ClassLabel,
    string CodeDisplay,
    bool IntroSeen,
    bool PassportUnlocked,
    int CompletedBlocks,
    IReadOnlyList<PupilMboHboBlockStatusDto> Blocks);

public sealed record PupilMboHboBlockStatusDto(
    PupilMboHboBlock Block,
    string Key,
    string Title,
    string Description,
    int QuestionCount,
    int AnsweredCount,
    PupilMboHboTileStatus Status);

public enum PupilMboHboTileStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2
}

public sealed record PupilMboHboIntroResponse(bool Ok, string RedirectPath);

public sealed record PupilMboHboQuestionDto(
    PupilMboHboBlock Block,
    string BlockKey,
    string ItemId,
    int Index,
    int Total,
    string Dimension,
    string Question,
    string Scenario,
    PupilMboHboScaleKind Scale,
    int? CurrentValue,
    int? CurrentLeuk,
    int? CurrentGoed);

public sealed record PupilMboHboAnswerRequest(int Value);

public sealed record PupilMboHboVakAnswerRequest(int Leuk, int Goed);

public sealed record PupilMboHboAnswerResponse(
    bool BlockComplete,
    bool AllComplete,
    string? NextItemId,
    int Index,
    int Total);

public sealed record PupilMboHboOutcomeDto(
    PupilMboHboBlock Block,
    string BlockKey,
    string Title,
    int AnsweredCount,
    int QuestionCount,
    IReadOnlyList<PupilMboHboBarDto> Bars,
    string Summary,
    IReadOnlyList<string> Highlights);

public sealed record PupilMboHboBarDto(string Label, int Percent);

public sealed record PupilMboHboPassportDto(
    string ClassLabel,
    string CodeDisplay,
    string SchoolName,
    bool Unlocked,
    IReadOnlyList<PupilMboHboPassportSectionDto> Sections,
    string FooterNote);

public sealed record PupilMboHboPassportSectionDto(string Title, string BodyHtml);

public sealed record TeacherMboHboClassProgressDto(
    Guid ClassId,
    string ClassLabel,
    PupilQuestionSet QuestionSet,
    IReadOnlyList<TeacherMboHboCodeRowDto> Codes,
    TeacherMboHboGroupChartsDto Charts);

public sealed record TeacherMboHboCodeRowDto(
    Guid CodeId,
    string CodeDisplay,
    IReadOnlyList<PupilMboHboTileStatus> BlockStatuses,
    int CompletedCount,
    bool PassportReady,
    bool PatternSignal);

public sealed record TeacherMboHboGroupChartsDto(
    IReadOnlyList<PupilMboHboBarDto> InteresseAverage,
    IReadOnlyList<TeacherMboHboBlockCompletionDto> BlockCompletion);

public sealed record TeacherMboHboBlockCompletionDto(
    string BlockKey,
    int CompletedPercent,
    int InProgressPercent);
