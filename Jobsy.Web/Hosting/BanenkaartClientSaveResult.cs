using System.Net;
using Jobsy.Web.Banenkaart;
using Jobsy.Core.Hosting;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

namespace Jobsy.Web.Hosting;

internal static class BanenkaartClientSaveResult
{
    internal sealed record SaveJson(bool Ok, bool Liked, string? Error, int Status, string? Code);

    internal static async Task<IResult> ExecuteAsync(
        HttpContext http,
        IFormCollection form,
        JobsyApiClient api,
        CultureState culture,
        ILogger logger,
        DeploymentEnvironmentLabel deployment,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(form["vacancyId"].ToString(), out var vacancyId))
        {
            return Fail(http, culture, logger, vacancyId, liked: false, "invalid_vacancy", StatusCodes.Status400BadRequest);
        }

        var liked = string.Equals(form["liked"].ToString(), "1", StringComparison.Ordinal)
                    || string.Equals(form["liked"].ToString(), "true", StringComparison.OrdinalIgnoreCase);

        try
        {
            var savedLiked = await api.SetLikedAsync(vacancyId, liked, cancellationToken);
            return Success(http, savedLiked);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode code)
        {
            logger.LogWarning(
                ex,
                "Banenkaart save API like mutation failed vacancy={VacancyId} liked={Liked} status={Status}",
                vacancyId,
                liked,
                code);
            AcceptatieDiagnosticHeaders.TrySet(http.Response, deployment, ex);

            if (await MatchesDesiredLikeStateAsync(api, vacancyId, liked, cancellationToken))
            {
                return Success(http, liked);
            }

            return Fail(
                http,
                culture,
                logger,
                vacancyId,
                liked: false,
                $"like_api_{(int)code}",
                (int)code >= 400 ? (int)code : StatusCodes.Status502BadGateway);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Banenkaart save failed vacancy={VacancyId} liked={Liked}", vacancyId, liked);
            AcceptatieDiagnosticHeaders.TrySet(http.Response, deployment, ex);

            if (await MatchesDesiredLikeStateAsync(api, vacancyId, liked, cancellationToken))
            {
                return Success(http, liked);
            }

            return Fail(
                http,
                culture,
                logger,
                vacancyId,
                liked: false,
                ex.GetType().Name,
                StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<bool> MatchesDesiredLikeStateAsync(
        JobsyApiClient api,
        Guid vacancyId,
        bool desiredLiked,
        CancellationToken ct)
    {
        try
        {
            var actual = await api.GetLikedAsync(vacancyId, ct);
            return actual == desiredLiked;
        }
        catch
        {
            return false;
        }
    }

    private static IResult Success(HttpContext http, bool liked)
    {
        if (BanenkaartClientJson.WantsJson(http))
        {
            return Results.Json(new SaveJson(true, liked, null, StatusCodes.Status200OK, null));
        }

        var returnQuery = http.Request.Form["returnQuery"].ToString();
        if (!string.IsNullOrWhiteSpace(returnQuery) && returnQuery.StartsWith('?'))
        {
            return SeeOther("/banenkaart" + returnQuery + "&saved=1");
        }

        var state = BanenkaartSsrQuery.Parse(http.Request.Query);
        return SeeOther(BanenkaartSsrQuery.BuildUrl(state, saved: true));
    }

    private static IResult Fail(
        HttpContext http,
        CultureState culture,
        ILogger logger,
        Guid vacancyId,
        bool liked,
        string code,
        int httpStatus)
    {
        logger.LogWarning(
            "Banenkaart save rejected vacancy={VacancyId} code={Code} status={Status}",
            vacancyId,
            code,
            httpStatus);

        if (BanenkaartClientJson.WantsJson(http))
        {
            return Results.Json(
                new SaveJson(false, liked, culture["Common.Error.TryAgain"], httpStatus, code),
                statusCode: httpStatus);
        }

        var state = BanenkaartSsrQuery.Parse(http.Request.Query);
        return SeeOther(BanenkaartSsrQuery.BuildUrl(state, error: culture["Common.Error.TryAgain"]));
    }

    private static RedirectHttpResult SeeOther(string url)
        => TypedResults.Redirect(url, permanent: false, preserveMethod: false);
}
