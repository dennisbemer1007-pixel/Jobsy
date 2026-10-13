using System.Security.Claims;
using Jobsy.Core.Authorization;
using Microsoft.AspNetCore.Authentication;

namespace Jobsy.Web.Security;

/// <summary>
/// Pupil pages must not touch the response from a component. The interactive circuit
/// renders after headers are read-only, and <c>AuthenticateAsync</c> on the pupil cookie
/// registers <c>OnStarting</c> for sliding renewal — that throws and shuts the circuit down.
/// This middleware runs only for the HTTP request, before the response starts.
/// </summary>
public sealed class LeerlingNoStoreMiddleware
{
    /// <summary>Claims principal captured for the layout. Components read this; they do not authenticate.</summary>
    public const string PupilPrincipalItemKey = "Jobsy.Leerling.Principal";

    private readonly RequestDelegate _next;

    public LeerlingNoStoreMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/leerling") && !context.Response.HasStarted)
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.CacheControl = "no-store";
                return Task.CompletedTask;
            });

            var pupil = await context.AuthenticateAsync(PupilAuthDefaults.Scheme);
            if (pupil.Succeeded
                && pupil.Principal?.Identity?.IsAuthenticated == true)
            {
                context.Items[PupilPrincipalItemKey] = pupil.Principal;
            }
        }

        await _next(context);
    }
}
