using System.Security.Claims;
using Jobsy.Core.Contracts;

namespace Jobsy.Core.Interfaces;

public interface ICandidateProfileResetService
{
    /// <summary>
    /// Wipes all candidate profile data while keeping login identity. Requires typed confirm phrase.
    /// </summary>
    Task<ResetCandidateProfileResponse> ResetAsync(
        ClaimsPrincipal principal,
        string confirmPhrase,
        string? uiLanguage,
        CancellationToken cancellationToken = default);
}
