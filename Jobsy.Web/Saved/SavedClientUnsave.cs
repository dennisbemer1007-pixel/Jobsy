using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Jobsy.Web.Saved;

internal static class SavedClientUnsave
{
    internal static async Task<IResult> PostAsync(
        HttpContext http,
        IFormCollection form,
        JobsyApiClient api,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        var filter = SavedSsrQuery.NormalizeFilter(form["filter"].ToString());
        var sort = SavedSsrQuery.NormalizeSort(form["sort"].ToString());
        if (!Guid.TryParse(form["vacancyId"].ToString(), out var vacancyId))
        {
            return Fail(filter, sort, culture["Common.Error.TryAgain"], http);
        }

        try
        {
            await api.SetLikedAsync(vacancyId, liked: false, cancellationToken);
            if (SavedClientJson.WantsJson(http))
            {
                return Results.Json(new { ok = true, vacancyId = vacancyId.ToString("D") });
            }

            return SeeOther(SavedSsrQuery.BuildUrl(filter, sort, saved: true));
        }
        catch
        {
            return Fail(filter, sort, culture["Common.Error.TryAgain"], http);
        }
    }

    private static IResult Fail(string filter, string sort, string message, HttpContext http)
    {
        if (SavedClientJson.WantsJson(http))
        {
            return Results.Json(new { ok = false, error = message });
        }

        return RedirectWithError(filter, sort, message);
    }

    private static RedirectHttpResult RedirectWithError(string filter, string sort, string message)
        => SeeOther(SavedSsrQuery.BuildUrl(filter, sort, error: message));

    private static RedirectHttpResult SeeOther(string url)
        => TypedResults.Redirect(url, permanent: false, preserveMethod: false);
}
