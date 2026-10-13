using System.Collections.Concurrent;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using Microsoft.AspNetCore.Authentication;

namespace Jobsy.Web.Auth;

/// <summary>
/// Server-side copy of the opaque API pupil ticket, keyed by pupil code id.
/// The Blazor circuit has no request cookies, so <see cref="Jobsy.Web.Services.JobsyApiAuthHandler"/>
/// reads the ticket from here after <see cref="PupilCircuitAuthenticationMiddleware"/> captured it
/// on <c>/_blazor</c>. Never written into the staff cookie or the Web pupil cookie.
/// </summary>
public sealed class PupilApiTicketStore
{
    private readonly ConcurrentDictionary<string, string> _byCode = new(StringComparer.Ordinal);

    public void Set(string? pupilCodeId, string? ticket)
    {
        if (string.IsNullOrWhiteSpace(pupilCodeId) || string.IsNullOrWhiteSpace(ticket))
        {
            return;
        }

        _byCode[pupilCodeId] = ticket;
    }

    public string? Get(string? pupilCodeId)
        => !string.IsNullOrWhiteSpace(pupilCodeId) && _byCode.TryGetValue(pupilCodeId, out var ticket)
            ? ticket
            : null;

    public void Remove(string? pupilCodeId)
    {
        if (!string.IsNullOrWhiteSpace(pupilCodeId))
        {
            _byCode.TryRemove(pupilCodeId, out _);
        }
    }
}

/// <summary>
/// <see cref="Microsoft.AspNetCore.Components.Server.ServerAuthenticationStateProvider"/> copies
/// <see cref="HttpContext.User"/> from the <c>/_blazor</c> connection. That request authenticates
/// only the staff cookie, so a pupil is anonymous in the circuit and
/// <c>AuthorizeRouteView</c> sends them to <c>/login</c>.
/// When the staff cookie is absent, authenticate the pupil scheme and use that principal.
/// A staff principal is never replaced, and this does not run for staff pages.
/// </summary>
public sealed class PupilCircuitAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public PupilCircuitAuthenticationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsBlazorCircuit(context.Request.Path)
            && context.User.Identity?.IsAuthenticated != true)
        {
            var pupil = await context.AuthenticateAsync(PupilAuthDefaults.Scheme);
            if (pupil.Succeeded
                && pupil.Principal?.Identity?.IsAuthenticated == true
                && global::Jobsy.Web.Services.JobsyApiAuthHandler.IsPupilPrincipal(pupil.Principal))
            {
                context.User = pupil.Principal;
                var codeId = pupil.Principal.FindFirst(PupilClaimTypes.PupilCodeId)?.Value;
                var ticket = PupilApiSessionCookie.Read(context);
                context.RequestServices.GetService<PupilApiTicketStore>()?.Set(codeId, ticket);
            }
        }

        await _next(context);
    }

    internal static bool IsBlazorCircuit(PathString path)
        => path.StartsWithSegments("/_blazor");
}
