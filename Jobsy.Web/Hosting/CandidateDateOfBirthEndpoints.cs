using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Web.Auth;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.WebUtilities;

namespace Jobsy.Web.Hosting;

public static class CandidateDateOfBirthEndpoints
{
    public static IEndpointRouteBuilder MapCandidateDateOfBirthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/candidate-client/save-date-of-birth", SaveDateOfBirthAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Candidate" });

        return app;
    }

    private static async Task<IResult> SaveDateOfBirthAsync(
        HttpContext http,
        IAntiforgery antiforgery,
        JobsyApiClient api,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        if (!http.Request.HasFormContentType)
        {
            return Results.BadRequest();
        }

        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return RedirectError(culture["Common.Error.TryAgain"]);
        }

        var form = await http.Request.ReadFormAsync(cancellationToken);
        var birthText = form["birthDateText"].ToString();
        if (!JobsyDateInputRules.TryParseFlexibleDate(birthText?.Trim() ?? "", out var dob))
        {
            return RedirectError(culture["Onboarding.Validation.BirthDate"]);
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (CandidateConsentRules.IsBelowMinimumAge(dob, today))
        {
            return RedirectError(culture["Onboarding.Validation.BirthDate"]);
        }

        if (dob > today.AddYears(-13) || dob < today.AddYears(-100))
        {
            return RedirectError(culture["Onboarding.Validation.BirthDate"]);
        }

        try
        {
            var updated = await api.UpdateDateOfBirthAsync(dob, cancellationToken);
            if (updated is null || !updated.HasDateOfBirth)
            {
                return RedirectError(culture["Common.Error.TryAgain"]);
            }
        }
        catch (ApiErrorException ex)
        {
            return RedirectError(ex.UserMessage ?? culture["Common.Error.TryAgain"]);
        }
        catch (Exception)
        {
            return RedirectError(culture["Common.Error.TryAgain"]);
        }

        var returnUrl = AuthRedirects.TrySafeLocalUrl(form["returnUrl"].ToString());
        var redirect = string.IsNullOrWhiteSpace(returnUrl)
            ? "/candidate/kompas"
            : returnUrl!;
        return Results.Redirect(redirect);
    }

    private static IResult RedirectError(string message)
    {
        var url = QueryHelpers.AddQueryString(
            CandidateDateOfBirthPathRules.DobCapturePath,
            "error",
            message);
        return Results.Redirect(url);
    }
}
