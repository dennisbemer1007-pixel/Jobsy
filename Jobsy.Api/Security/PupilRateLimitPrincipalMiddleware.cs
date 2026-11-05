using System.Security.Claims;
using Jobsy.Core.Authorization;
using Microsoft.AspNetCore.Authentication;

namespace Jobsy.Api.Security;

/// <summary>
/// Pupil rate limits partition on <see cref="PupilClaimTypes.PupilCodeId"/>, but the default
/// authenticate scheme is JWT and the pupil cookie is only required by
/// <see cref="JobsyPolicies.PupilSession"/> after <c>UseRateLimiter</c>. Authenticate the pupil
/// cookie here for <c>/api/pupil/*</c> so limiters see the signed-in pupil without changing
/// authorization behaviour.
/// </summary>
public sealed class PupilRateLimitPrincipalMiddleware
{
    private readonly RequestDelegate _next;

    public PupilRateLimitPrincipalMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/api/pupil", StringComparison.OrdinalIgnoreCase))
        {
            var pupil = await context.AuthenticateAsync(PupilAuthDefaults.Scheme);
            if (pupil.Succeeded
                && pupil.Principal?.Identity?.IsAuthenticated == true
                && pupil.Principal.FindFirst(PupilClaimTypes.PupilCodeId) is not null)
            {
                var identities = context.User.Identities.ToList();
                if (pupil.Principal.Identity is ClaimsIdentity pupilIdentity)
                {
                    identities.Add(pupilIdentity);
                }
                else
                {
                    identities.Add(new ClaimsIdentity(
                        pupil.Principal.Claims,
                        PupilAuthDefaults.Scheme));
                }

                context.User = new ClaimsPrincipal(identities);
            }
        }

        await _next(context);
    }
}
