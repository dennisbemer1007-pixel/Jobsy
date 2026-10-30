using System.Globalization;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;

namespace Jobsy.Web.Auth;

public static class ParentPassportPrincipal
{
    public static ClaimsPrincipal Create(ParentPassportSessionDto session)
    {
        var claims = new List<Claim>
        {
            new(ParentPassportClaimTypes.PupilCodeId, session.PupilCodeId.ToString("D")),
            new(ParentPassportClaimTypes.AccessExpiresUtc,
                session.AccessExpiresUnix.ToString(CultureInfo.InvariantCulture)),
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, ParentPassportAuthDefaults.Scheme));
    }
}
