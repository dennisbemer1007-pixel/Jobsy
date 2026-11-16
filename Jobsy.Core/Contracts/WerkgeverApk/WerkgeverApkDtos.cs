using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Core.Contracts.WerkgeverApk;

public sealed record WerkgeverApkErrorDto(string Error, string Message);

public sealed record WerkgeverApkEmployeeTeamOptionDto(Guid Id, string Name, int CodeCount);

public sealed record WerkgeverApkEmployeeCompanyOptionDto(
    Guid Id,
    string Name,
    IReadOnlyList<WerkgeverApkEmployeeTeamOptionDto> Teams);

public sealed record WerkgeverApkGenerateCodesRequest(int Count);

public sealed record WerkgeverApkEmployeeQuestionDto(
    PupilMboHboBlock Block,
    string BlockKey,
    string ItemId,
    int Index,
    int Total,
    string Dimension,
    string Question,
    string? Scenario,
    PupilMboHboScaleKind Scale,
    int? Value,
    string? PreviousItemId,
    WerkgeverApkAbItemKind ItemKind = WerkgeverApkAbItemKind.Ab,
    string? OptionA = null,
    string? OptionB = null,
    string? Heading = null,
    int DisplayBlock = 1,
    int DisplayBlockCount = 3,
    WerkgeverApkPackageKind Package = WerkgeverApkPackageKind.Basis);

public sealed record WerkgeverApkEmployeeAnswerRequest(int Value);

public sealed record WerkgeverApkEmployeeAnswerResponse(string? NextItemId, string? NextPath, bool BlockComplete);

public sealed record WerkgeverApkEmployeeComparisonResponse(WerkgeverApkComparisonDto Comparison);

public sealed record WerkgeverApkTeamOverviewCodeDto(
    Guid EmployeeCodeId,
    string? Label,
    string CodeDisplay,
    WerkgeverApkEmployeeCodeStatus Status,
    bool EmployeeCompleted,
    bool SharedWithManager,
    bool ManagerCompleted,
    string? EscoOccupationId,
    string? OfficialOccupationName);

public sealed record WerkgeverApkTeamOverviewDto(
    Guid TeamId,
    string TeamName,
    IReadOnlyList<WerkgeverApkTeamOverviewCodeDto> Codes);

public sealed record WerkgeverApkUpdateEmployeeLabelRequest(string? Label);

public sealed record UpdateLoopbaanApkRequest(bool LoopbaanApkEnabled);

public sealed record UpdateLoopbaanApkPackageRequest(WerkgeverApkPackageKind Package);

public sealed record WerkgeverApkPackagePricingDto(
    string BasisPriceLabel,
    string UitgebreidPriceLabel,
    string? PriceUnit);

public sealed record WerkgeverApkManagerPackageDto(
    WerkgeverApkPackageKind ActivePackage,
    bool UitgebreidRequestPending,
    string BasisPriceLabel,
    string UitgebreidPriceLabel);

public sealed record WerkgeverApkPauseDto(
    int CompletedDisplayBlock,
    int DisplayBlockCount,
    int AnsweredCount,
    int TotalQuestions,
    WerkgeverApkPackageKind Package);

public sealed record WerkgeverApkEmployeeLoginRequest(string Code);

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

public sealed record WerkgeverApkSharePreviewDto(
    bool SharedWithManager,
    bool EmployeeCompleted,
    string ManagerLabel,
    IReadOnlyList<string> ManagerSeesBullets);

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

public sealed record WerkgeverApkOccupationSearchHitDto(string EscoOccupationId, string Name);

public sealed record WerkgeverApkSetEmployeeOccupationRequest(string EscoOccupationId);

public sealed record WerkgeverApkEmployeeFunctieResponse(WerkgeverApkFunctieComparisonDto Comparison);

public sealed record WerkgeverApkFunctiePickDto(
    Guid EmployeeCodeId,
    string? EmployeeLabel,
    string? SelectedEscoOccupationId,
    string? SelectedOccupationName,
    IReadOnlyList<WerkgeverApkOccupationSearchHitDto> Suggestions,
    string? Query,
    IReadOnlyList<WerkgeverApkTeamOverviewCodeDto> TeamRows);

public sealed record WerkgeverApkFunctieGroupInsightsResponse(WerkgeverApkFunctieGroupInsightsDto Insights);
