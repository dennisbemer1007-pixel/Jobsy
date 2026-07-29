using Jobsy.Core.Enums;

namespace Jobsy.Api.Models;

public record CsvImportRequest(
    Guid CompanyId,
    IReadOnlyList<CsvImportRowRequest> Rows);

public record CsvImportRowRequest(
    int RowNumber,
    string? Title,
    string? Description,
    string? StartDate,
    string? EndDate,
    string? Branches,
    string? SalaryTableId,
    string? CompanyId = null,
    string? HourlyWage = null,
    string? Image = null,
    string? Video = null,
    string? Transport = null,
    string? DrivingLicense = null,
    string? Education = null,
    string? MinimumEmployers = null);

public record CsvImportResultDto(
    int TotalRows,
    int SuccessCount,
    int FailedCount,
    IReadOnlyList<CsvImportRowResultDto> Rows,
    string PublishHint);

public record CsvImportRowResultDto(
    int RowNumber,
    bool Success,
    Guid? VacancyId,
    string? ErrorMessage,
    CsvImportRowRequest Data);

public record CsvImportRetryRequest(Guid CompanyId, CsvImportRowRequest Row);

