using Jobsy.Core.Authorization;
using Jobsy.Web.Auth;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Authentication;

namespace Jobsy.Web.Scholen;

/// <summary>Sign out pupil SSR session and send them to the styled code login.</summary>
public static class PupilSessionRecovery
{
    public static async Task<IResult> RedirectToCodeLoginAsync(HttpContext http, string? returnPath = null)
    {
        var codeId = http.User.FindFirst(PupilClaimTypes.PupilCodeId)?.Value;
        http.RequestServices.GetService<PupilApiTicketStore>()?.Remove(codeId);
        await http.SignOutAsync(PupilAuthDefaults.Scheme);
        PupilApiSessionCookie.Clear(http);
        http.Response.Headers.CacheControl = "no-store";
        var safeReturn = AuthRedirects.SafeLocalUrl(returnPath ?? http.Request.Path.Value);
        var target = string.IsNullOrEmpty(safeReturn)
            ? "/leerling?error=expired"
            : $"/leerling?error=expired&returnUrl={Uri.EscapeDataString(safeReturn)}";
        return Results.Redirect(target);
    }
}
