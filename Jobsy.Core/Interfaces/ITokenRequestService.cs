using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public sealed record TokenRequestDto(
    Guid Id,
    Guid OrganisationCompanyId,
    Guid BranchCompanyId,
    string BranchName,
    Guid RequestedByUserId,
    string RequestedByName,
    int Amount,
    string Reason,
    string? Note,
    string Status,
    Guid? HandledByUserId,
    DateTime? HandledAtUtc,
    DateTime CreatedAtUtc);

public sealed class TokenRequestException : Exception
{
    public TokenRequestException(string code, string message, int statusCode = 400)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public string Code { get; }
    public int StatusCode { get; }
}

public interface ITokenRequestService
{
    Task<TokenRequestDto?> GetAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task<TokenRequestDto> CreateAsync(
        Guid branchCompanyId,
        Guid requestedByUserId,
        int amount,
        TokenRequestReason reason,
        string? note,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TokenRequestDto>> ListAsync(
        IReadOnlyList<Guid> accessibleCompanyIds,
        Guid? forUserId,
        bool organisationWide,
        TokenRequestStatus? status,
        CancellationToken cancellationToken = default);

    Task<TokenRequestDto> ApproveAsync(
        Guid requestId,
        Guid handledByUserId,
        CancellationToken cancellationToken = default);

    Task<TokenRequestDto> RejectAsync(
        Guid requestId,
        Guid handledByUserId,
        string? reason,
        CancellationToken cancellationToken = default);

    Task<TokenRequestDto> WithdrawAsync(
        Guid requestId,
        Guid requestedByUserId,
        CancellationToken cancellationToken = default);
}
