using System.Net;
using Jobsy.Core.Authorization;
using Jobsy.Core.Features;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;

namespace Jobsy.Web.Hosting;

/// <summary>SSR GET for /ouder — no Blazor page.</summary>
public static class ParentPortalEndpoints
{
    public static IEndpointRouteBuilder MapParentPortalEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(OuderSsrHtml.PagePath, GetPageAsync).AllowAnonymous();
        return app;
    }

    private static async Task<IResult> GetPageAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IFeatureFlags features,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        http.Response.Headers.CacheControl = "no-store";
        culture.InitializeFromRequest(http);

        var pageTitle = culture["Ouder.Seo.Title"];
        if (!await SchoolsFeatureGate.IsEnabledAsync(features, cancellationToken))
        {
            var disabled = OuderSsrHtml.RenderDocument(culture, pageTitle, OuderSsrHtml.RenderFeatureDisabled(culture));
            return Results.Content(disabled, "text/html; charset=utf-8");
        }

        var parentAuth = await http.AuthenticateAsync(ParentPassportAuthDefaults.Scheme);
        var session = parentAuth.Succeeded && parentAuth.Principal?.Identity?.IsAuthenticated == true;
        if (!session)
        {
            var error = http.Request.Query["error"].ToString();
            var tokens = antiforgery.GetAndStoreTokens(http);
            var afHtml = BuildAntiforgeryHtml(tokens);
            var body = OuderSsrHtml.RenderLoginForm(
                culture,
                string.IsNullOrEmpty(error) ? null : error,
                afHtml);
            var html = OuderSsrHtml.RenderDocument(culture, pageTitle, body);
            return Results.Content(html, "text/html; charset=utf-8");
        }

        var languageQuery = culture.Language;
        try
        {
            var passport = await api.GetParentG78PassportAsync(languageQuery, cancellationToken);
            if (passport is not null && passport.Enabled)
            {
                var passportBody = OuderSsrHtml.RenderPassport(culture, passport, languageQuery);
                var passportDoc = OuderSsrHtml.RenderDocument(culture, pageTitle, passportBody);
                return Results.Content(passportDoc, "text/html; charset=utf-8");
            }
        }
        catch
        {
            // Fall through to invalid session UI.
        }

        var invalid = OuderSsrHtml.RenderDocument(culture, pageTitle, OuderSsrHtml.RenderInvalidSession(culture));
        return Results.Content(invalid, "text/html; charset=utf-8");
    }

    private static string BuildAntiforgeryHtml(AntiforgeryTokenSet tokens)
        => tokens.RequestToken is { } token
            ? $"""<input type="hidden" name="{WebUtility.HtmlEncode(tokens.FormFieldName)}" value="{WebUtility.HtmlEncode(token)}" />"""
            : "";
}
