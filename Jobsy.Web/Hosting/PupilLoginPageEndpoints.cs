using System.Net;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

/// <summary>SSR GET for /leerling login — no Blazor page or circuit.</summary>
public static class PupilLoginPageEndpoints
{
    public const string PagePath = "/leerling";

    public static IEndpointRouteBuilder MapPupilLoginPageEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(PagePath, GetPageAsync).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> GetPageAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        http.Response.Headers.CacheControl = "no-store";
        culture.InitializeFromRequest(http);
        if (PupilLeerlingLoginSsrHtml.IsPupilSession(http))
        {
            var tokensEarly = antiforgery.GetAndStoreTokens(http);
            var afEarly = BuildAntiforgeryHtml(tokensEarly);
            var htmlEarly = PupilLeerlingLoginSsrHtml.RenderDocument(
                culture, [], [], null, null, null, null, pupilSession: true, staffBlock: null, afEarly);
            return Results.Content(htmlEarly, "text/html; charset=utf-8");
        }

        var staff = await http.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (staff.Succeeded && staff.Principal?.Identity?.IsAuthenticated == true)
        {
            var block = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                culture["Leerling.Login.StaffBusy"],
                PupilLeerlingLoginSsrHtml.StaffRoleLabel(
                    culture, staff.Principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value));
            var tokensStaff = antiforgery.GetAndStoreTokens(http);
            var htmlStaff = PupilLeerlingLoginSsrHtml.RenderDocument(
                culture, [], [], null, null, null, null, false, block, BuildAntiforgeryHtml(tokensStaff));
            return Results.Content(htmlStaff, "text/html; charset=utf-8");
        }

        IReadOnlyList<PupilSchoolOptionDto> schools;
        try
        {
            schools = await api.GetPupilSchoolsAsync(cancellationToken);
        }
        catch
        {
            schools = [];
        }

        Guid? schoolId = Guid.TryParse(http.Request.Query["schoolId"], out var sid) ? sid : null;
        Guid? classId = Guid.TryParse(http.Request.Query["classId"], out var cid) ? cid : null;
        var error = http.Request.Query["error"].ToString();
        var questionSet = http.Request.Query["questionSet"].ToString();

        IReadOnlyList<PupilClassOptionDto> classes = [];
        if (schoolId is Guid schoolGuid)
        {
            try
            {
                classes = await api.GetPupilClassesAsync(schoolGuid, cancellationToken) ?? [];
            }
            catch
            {
                classes = [];
            }
        }

        var tokens = antiforgery.GetAndStoreTokens(http);
        var html = PupilLeerlingLoginSsrHtml.RenderDocument(
            culture,
            schools,
            classes,
            schoolId,
            classId,
            string.IsNullOrEmpty(error) ? null : error,
            string.IsNullOrEmpty(questionSet) ? null : questionSet,
            pupilSession: false,
            staffBlock: null,
            BuildAntiforgeryHtml(tokens));
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static string BuildAntiforgeryHtml(AntiforgeryTokenSet tokens)
        => tokens.RequestToken is { } token
            ? $"""<input type="hidden" name="{WebUtility.HtmlEncode(tokens.FormFieldName)}" value="{WebUtility.HtmlEncode(token)}" />"""
            : "";
}
