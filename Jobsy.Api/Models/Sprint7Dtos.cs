using Jobsy.Core.Enums;

namespace Jobsy.Api.Models;

public record SubmitRegistrationRequest(
    string KvkNumber,
    string KvkEstablishmentId,
    RegistrationScope Scope,
    string ContactName,
    string ContactEmail,
    string? ContactPhone = null,
    bool AcceptedTerms = false,
    string? ConsentVersion = null);

public record RegistrationSubmitResponse(
    Guid RegistrationId,
    string Status,
    bool RequiresTakeover,
    string Message,
    string? ActivationUrl);

public record RegistrationActivationResponse(
    Guid RegistrationId,
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    Guid? CompanyId,
    IReadOnlyList<Guid> CompanyIds,
    string TemporaryPassword,
    Guid? OrganizationCompanyId,
    Guid? BranchCompanyId);

public record TakeoverInboxItemDto(
    Guid TakeoverId,
    Guid RegistrationId,
    Guid TargetCompanyId,
    string TargetCompanyName,
    string KvkEstablishmentId,
    string RequesterName,
    string RequesterEmail,
    string Scope,
    DateTime CreatedAt);

public record TakeoverDecisionResponse(
    Guid TakeoverId,
    string Status,
    string Message,
    Guid? OrganizationCompanyId,
    Guid? BranchCompanyId);

public record RejectTakeoverRequest(string? Note = null);

public record LocalLoginRequest(string Email, string Password);

public record LocalLoginResponse(
    string Email,
    string FullName,
    string Role,
    Guid? CompanyId,
    IReadOnlyList<Guid> CompanyIds);
