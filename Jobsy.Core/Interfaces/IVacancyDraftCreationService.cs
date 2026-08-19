using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public sealed record VacancyDraftInput(
    Guid CompanyId,
    string Title,
    string Description,
    decimal HourlyWage,
    DateOnly StartDate,
    DateOnly EndDate,
    TransportMode RequiredTransport,
    string[] WorkTypes,
    Guid SalaryTableId,
    string? ImageUrl = null,
    string? VideoUrl = null,
    string? RequiredDrivingLicense = null,
    string? RequiredEducation = null,
    int? MinimumEmployers = null,
    Guid? IntermediaryCompanyId = null,
    bool ShowClientAddressOnMap = false,
    bool RequireEndClientKvk = false,
    VacancyKind Kind = VacancyKind.Regular,
    int? MinimumReferences = null);

public sealed record VacancyDraftCreateResult(bool Succeeded, Vacancy? Vacancy, string? ErrorMessage)
{
    public static VacancyDraftCreateResult Ok(Vacancy vacancy) => new(true, vacancy, null);
    public static VacancyDraftCreateResult Fail(string message) => new(false, null, message);
}

public interface IVacancyDraftCreationService
{
    /// <summary>
    /// Validates and creates a Draft vacancy. Does not publish (token flow stays in Lobsy UI).
    /// </summary>
    Task<VacancyDraftCreateResult> CreateDraftAsync(
        VacancyDraftInput input,
        VacancySource source,
        CancellationToken cancellationToken = default);
}
