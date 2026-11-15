using Jobsy.Core.Enums;

namespace Jobsy.Core.Contracts.WerkgeverApk;

public sealed record WerkgeverApkEmployeeLoginRequest(Guid CompanyId, Guid TeamId, string Code);

public sealed record WerkgeverApkEmployeeLoginResponse(
    string RedirectPath,
    Guid EmployeeCodeId,
    Guid TeamId,
    Guid CompanyId,
    string TeamName,
    string CompanyName,
    string CodeDisplay,
    WerkgeverApkEmployeeCodeStatus Status,
    int SessionVersion,
    bool WelcomeSeen);

public sealed record WerkgeverApkTeamDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    int CodeCount,
    DateTime CreatedAtUtc);

public sealed record WerkgeverApkTeamCreateRequest(string Name);

public sealed record WerkgeverApkEmployeeCodeRowDto(
    Guid Id,
    int Number,
    string DisplayCode,
    string? Label,
    WerkgeverApkEmployeeCodeStatus Status,
    bool SharedWithManager,
    DateTime? SharedAtUtc,
    DateTime? LastSeenAtUtc);

public sealed record WerkgeverApkEmployeeProgressDto(
    Guid EmployeeCodeId,
    string CodeDisplay,
    string TeamName,
    WerkgeverApkEmployeeCodeStatus Status,
    bool WelcomeSeen,
    bool Completed,
    string ProgressJson,
    int AnsweredCount,
    int TotalQuestions);

public sealed record WerkgeverApkEmployeeProgressPatchRequest(string ProgressJson, bool? WelcomeSeen);

public sealed record WerkgeverApkShareWithManagerRequest(bool Share);

public sealed record WerkgeverApkShareWithManagerResponse(
    bool SharedWithManager,
    DateTime? SharedAtUtc);

public sealed record WerkgeverApkManagerQuestionDto(
    string Id,
    string Label,
    string Category,
    string Scenario);

public sealed record WerkgeverApkManagerSheetDto(
    Guid EmployeeCodeId,
    string? EmployeeLabel,
    bool Completed,
    IReadOnlyDictionary<string, int> Answers);

public sealed record WerkgeverApkManagerAnswerRequest(string QuestionId, int Value);

public sealed record WerkgeverApkManagerSheetSubmitResponse(bool Completed);