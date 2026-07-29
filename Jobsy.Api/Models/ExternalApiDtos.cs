namespace Jobsy.Api.Models;

public record ExternalVacancyStatusDto(
    Guid Id,
    Guid CompanyId,
    string Title,
    string Status,
    string CreatedVia,
    DateOnly StartDate,
    DateOnly EndDate);

public record UpdateExternalVacancyRequest(
    Guid? CompanyId = null,
    string? Title = null,
    string? Description = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    Jobsy.Core.Enums.TransportMode? RequiredTransport = null,
    string[]? WorkTypes = null,
    string? ImageUrl = null,
    string? VideoUrl = null,
    string? RequiredDrivingLicense = null,
    string? RequiredEducation = null,
    int? MinimumEmployers = null,
    string? Status = null);

public record GenerateApiKeyRequest(string? Name = null);

public record EmailApiKeyRequest(string? Email = null);

public record GeneratedApiKeyResponse(
    Guid Id,
    Guid CompanyId,
    string Name,
    string KeyPrefix,
    string PlaintextKey,
    DateTime CreatedAt,
    string Warning);
