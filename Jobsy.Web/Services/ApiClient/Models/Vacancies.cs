using Jobsy.Core.Enums;
using Jobsy.Web.Models;

namespace Jobsy.Web.Services;

public sealed class VacancyModerationException : Exception
{
    public VacancyModerationException(string warning, string suggestion)
        : base(warning)
    {
        Warning = warning;
        Suggestion = suggestion;
    }

    public string Warning { get; }
    public string Suggestion { get; }
}

public sealed class VacancyModerationFeedback
{
    public string? Code { get; set; }
    public string? Message { get; set; }
    public string? Suggestion { get; set; }
}

public record CreateVacancyForm(
    Guid CompanyId,
    string Title,
    string Description,
    decimal HourlyWage,
    DateOnly StartDate,
    DateOnly EndDate,
    TransportMode RequiredTransport,
    string[] WorkTypes,
    string? ImageUrl = null,
    string? VideoUrl = null,
    Guid? SalaryTableId = null,
    string? RequiredDrivingLicense = null,
    string? RequiredEducation = null,
    int? MinimumEmployers = null,
    bool OverrideContactPreference = false,
    bool DirectContactEnabled = false,
    bool ContactPreferMail = false,
    bool ContactPreferPhone = false,
    bool ContactPreferWhatsApp = false,
    decimal? MinHoursPerWeek = null,
    decimal? MaxHoursPerWeek = null,
    bool? FlexibleTimes = null,
    Dictionary<string, string[]>? ScheduleSlots = null,
    bool? LegalWorksAfter19 = null,
    bool? LegalNightShift23To06 = null,
    bool? LegalAdultSupervisorPresent = null,
    bool? LegalHandlesMoneyOrClosing = null,
    bool? LegalHeavyOrHazardousWork = null,
    bool ShowClientAddressOnMap = false,
    string Kind = "Regular",
    Guid? ExclusivitySettingId = null,
    Guid? CategoryId = null,
    Dictionary<string, string>? CategoryFields = null,
    bool SuitableFor65Plus = false,
    bool? RequireEmailVerification = null,
    int? MinimumReferences = null,
    string[]? CulturePillars = null,
    string? BarrierKind = null,
    string[]? BarrierDiplomas = null,
    string[]? BarrierCertifications = null,
    int? BarrierMinExperienceYears = null,
    int? BarrierMinExperienceHours = null,
    string[]? BarrierHardChecks = null);

public sealed class CsvImportRowForm
{
    public int RowNumber { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string? Branches { get; set; }
    public string? SalaryTableId { get; set; }
    public string? CompanyId { get; set; }
    public string? HourlyWage { get; set; }
    public string? Image { get; set; }
    public string? Video { get; set; }
    public string? Transport { get; set; }
    public string? DrivingLicense { get; set; }
    public string? Education { get; set; }
    public string? MinimumEmployers { get; set; }
    public string? KvkNumber { get; set; }
    public string? KvkEstablishmentId { get; set; }
    public string? ShowClientAddressOnMap { get; set; }
}

public sealed class CsvImportResult
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<CsvImportRowResult> Rows { get; set; } = [];
    public string PublishHint { get; set; } = string.Empty;
}

public sealed class CsvImportRowResult
{
    public int RowNumber { get; set; }
    public bool Success { get; set; }
    public Guid? VacancyId { get; set; }
    public string? ErrorMessage { get; set; }
    public CsvImportRowForm Data { get; set; } = new();
}

public sealed class VacancyCategoryItem
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#F54A1B";
    public decimal PublishCostTokens { get; set; }
    public bool HighlightAvailable { get; set; } = true;
    public decimal HighlightCostTokens { get; set; }
    public bool PushBomAvailable { get; set; } = true;
    public decimal? PushBomCostTokens { get; set; }
    public bool IsAlwaysFree { get; set; }
    public string PlacementKind { get; set; } = "Regular";
    public List<string> ExtraFields { get; set; } = [];
    public List<VacancyCategoryFieldItem> ExtraFieldDefinitions { get; set; } = [];
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool ShowInMapFilter { get; set; } = true;
    public bool ShowInLegend { get; set; } = true;
}

public sealed class VacancyCategoryFieldItem
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string InputType { get; set; } = "text";
    public List<string>? Options { get; set; }
}

public sealed class VacancyCategoryForm
{
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#F54A1B";
    public decimal PublishCostTokens { get; set; } = 1m;
    public bool HighlightAvailable { get; set; } = true;
    public decimal HighlightCostTokens { get; set; } = 2m;
    public bool PushBomAvailable { get; set; } = true;
    public decimal? PushBomCostTokens { get; set; }
    public bool IsAlwaysFree { get; set; }
    public string PlacementKind { get; set; } = "Regular";
    public List<string> ExtraFields { get; set; } = [];
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
    public bool? ShowInMapFilter { get; set; } = true;
    public bool? ShowInLegend { get; set; } = true;
}
