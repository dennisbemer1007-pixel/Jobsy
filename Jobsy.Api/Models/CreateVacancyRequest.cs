using Jobsy.Core.Enums;

namespace Jobsy.Api.Models;

public record CreateVacancyRequest(
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
    int? MinimumEmployers = null);
