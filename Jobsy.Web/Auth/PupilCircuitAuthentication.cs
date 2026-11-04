using System.Collections.Concurrent;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using Microsoft.AspNetCore.Authentication;

namespace Jobsy.Web.Auth;

/// <summary>
/// Server-side copy of the opaque API parent passport ticket, keyed by pupil code id.
/// The Blazor circuit has no request cookies, so <see cref="Jobsy.Web.Services.JobsyApiAuthHandler"/>
/// reads the ticket from here after <see cref="PupilCircuitAuthenticationMiddleware"/> captured it
/// on <c>/_blazor</c>.
/// </summary>
public sealed class ParentApiTicketStore
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
                PupilApiTicketStoreSeeding.SeedFromRequestCookie(context, codeId);
            }
            else
            {
                var parent = await context.AuthenticateAsync(ParentPassportAuthDefaults.Scheme);
                if (parent.Succeeded
                    && parent.Principal?.Identity?.IsAuthenticated == true
                    && global::Jobsy.Web.Services.JobsyApiAuthHandler.IsParentPassportPrincipal(parent.Principal))
                {
                    context.User = parent.Principal;
                    var codeId = parent.Principal.FindFirst(ParentPassportClaimTypes.PupilCodeId)?.Value;
                    ParentApiTicketStoreSeeding.SeedFromRequestCookie(context, codeId);
                }
            }
        }
        else if (global::Jobsy.Web.Services.JobsyApiAuthHandler.IsPupilPrincipal(context.User))
        {
            var codeId = context.User.FindFirst(PupilClaimTypes.PupilCodeId)?.Value;
            PupilApiTicketStoreSeeding.SeedFromRequestCookie(context, codeId);
        }
        else if (global::Jobsy.Web.Services.JobsyApiAuthHandler.IsParentPassportPrincipal(context.User))
        {
            var codeId = context.User.FindFirst(ParentPassportClaimTypes.PupilCodeId)?.Value;
            ParentApiTicketStoreSeeding.SeedFromRequestCookie(context, codeId);
        }

        await _next(context);
        PupilApiTicketSyncMiddleware.TrySyncResponseCookie(context);
    }

    internal static bool IsBlazorCircuit(PathString path)
        => path.StartsWithSegments("/_blazor");
}

/// <summary>
/// After pupil SSR handlers, mirror the latest API ticket from
/// <see cref="PupilApiTicketStore"/> into <see cref="PupilApiSessionCookie"/> when the
/// browser cookie lags (common after sliding refresh on JSON saves).
/// </summary>
public static class PupilApiTicketSyncMiddleware
{
    internal static void TrySyncResponseCookie(HttpContext context)
    {
        if (context.Response.HasStarted
            || !global::Jobsy.Web.Services.JobsyApiAuthHandler.IsPupilPrincipal(context.User))
        {
            return;
        }

        var codeId = context.User.FindFirst(PupilClaimTypes.PupilCodeId)?.Value;
        var ticketStore = context.RequestServices.GetService<PupilApiTicketStore>();
        if (ticketStore is null || string.IsNullOrWhiteSpace(codeId))
        {
            return;
        }

        var cookie = PupilApiSessionCookie.Read(context);
        var resolved = ticketStore.ResolveTicket(codeId, cookie);
        if (string.IsNullOrWhiteSpace(resolved))
        {
            return;
        }

        if (!string.Equals(resolved, cookie, StringComparison.Ordinal))
        {
            PupilApiSessionCookie.Set(context, resolved);
        }
    }
}

/// <summary>
/// Seeds the per-code API ticket store from the browser cookie without downgrading a fresher
/// in-memory ticket after sliding refresh on the previous hop (same web instance).
/// </summary>
internal static class PupilApiTicketStoreSeeding
{
    internal static void SeedFromRequestCookie(HttpContext context, string? codeId)
    {
        var store = context.RequestServices.GetService<PupilApiTicketStore>();
        if (store is null || string.IsNullOrWhiteSpace(codeId))
        {
            return;
        }

        var cookie = PupilApiSessionCookie.Read(context);
        if (!store.TryGetEntry(codeId, out var stored))
        {
            store.Set(codeId, cookie);
            return;
        }

        if (string.IsNullOrWhiteSpace(cookie) || string.Equals(cookie, stored.Ticket, StringComparison.Ordinal))
        {
            return;
        }

        var clock = context.RequestServices.GetService<TimeProvider>() ?? TimeProvider.System;
        var now = clock.GetUtcNow();
        var storeFresh = now - stored.UpdatedAt <= PupilApiTicketStore.SameNodeBurstWindow;
        if (storeFresh)
        {
            // Browser cookie lags after JSON save on this node; upgrade the response cookie below.
            return;
        }

        // Browser cookie was renewed on another web node — adopt it locally; never downgrade the browser.
        store.Set(codeId, cookie);
    }
}

internal static class ParentApiTicketStoreSeeding
{
    internal static void SeedFromRequestCookie(HttpContext context, string? codeId)
    {
        var store = context.RequestServices.GetService<ParentApiTicketStore>();
        if (store is null || string.IsNullOrWhiteSpace(codeId))
        {
            return;
        }

        var cookie = ParentApiSessionCookie.Read(context);
        var stored = store.Get(codeId);
        if (string.IsNullOrWhiteSpace(stored))
        {
            store.Set(codeId, cookie);
            return;
        }

        if (string.IsNullOrWhiteSpace(cookie) || string.Equals(cookie, stored, StringComparison.Ordinal))
        {
            return;
        }

        ParentApiSessionCookie.Set(context, stored);
    }
}
