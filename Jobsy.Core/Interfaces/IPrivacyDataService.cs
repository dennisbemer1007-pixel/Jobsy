using System.Security.Claims;
using Jobsy.Core.Contracts;

namespace Jobsy.Core.Interfaces;

public interface IPrivacyDataService
{
    Task<object> ExportAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    Task DeleteOrAnonymizeAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Candidate unsubscribe step 1: store reason, e-mail a verification code.
    /// </summary>
    Task<RequestUnsubscribeResponse> RequestUnsubscribeAsync(
        ClaimsPrincipal principal,
        string reasonCode,
        string? reasonOther,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Candidate unsubscribe step 2: verify code, block account and anonymize data.
    /// </summary>
    Task ConfirmUnsubscribeAsync(
        ClaimsPrincipal principal,
        string verificationCode,
        CancellationToken cancellationToken = default);
}
