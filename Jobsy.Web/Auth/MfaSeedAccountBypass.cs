using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Security;
using Microsoft.Extensions.Configuration;

namespace Jobsy.Web.Auth;

/// <summary>
/// Acceptatie-only MFA skip for <c>@jobsy.local</c> seed accounts (<see cref="Core.Options.AuthOptions.SkipMfaForSeedAccounts"/>).
/// </summary>
public static class MfaSeedAccountBypass
{
    public static bool IsEnabled(IConfiguration configuration)
        => configuration.GetValue("Auth:SkipMfaForSeedAccounts", false);

    public static bool AppliesTo(IConfiguration configuration, string? email)
        => MfaPolicy.ShouldSkipMandatoryMfaForSeedAccount(email, IsEnabled(configuration));

    public static bool ShouldRedirectToMfaStep(
        IConfiguration configuration,
        string? email,
        bool requiresMfa,
        string? mfaChallengeToken)
    {
        if (!requiresMfa || AppliesTo(configuration, email))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(mfaChallengeToken);
    }

    public static void EnsureMfaVerifiedClaim(System.Security.Claims.ClaimsIdentity identity, IConfiguration configuration, string? email)
    {
        if (!AppliesTo(configuration, email))
        {
            return;
        }

        if (identity.HasClaim(c => c.Type == JobsyClaimTypes.MfaVerified && c.Value == "1"))
        {
            return;
        }

        identity.AddClaim(new Claim(JobsyClaimTypes.MfaVerified, "1"));
    }
}
