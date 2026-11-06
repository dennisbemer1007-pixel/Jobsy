using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Jobsy.Web.Career;

internal static class CareerClientDreamSaver
{
    internal static async Task<IResult> SaveAsync(
        IFormCollection form,
        JobsyApiClient api,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        var dream = (form["dreamTitle"].ToString() ?? "").Trim();
        var returnTab = CareerSsrTabs.Normalize(form["returnTab"].ToString());

        if (string.IsNullOrWhiteSpace(dream))
        {
            return RedirectWithError(returnTab, culture["Common.Error.TryAgain"]);
        }

        try
        {
            await api.SaveOnboardingDreamJobAsync(dream, cancellationToken);
        }
        catch (ApiErrorException ex)
        {
            return RedirectWithError(returnTab, ex.UserMessage ?? culture["Common.Error.TryAgain"]);
        }
        catch (Exception)
        {
            return RedirectWithError(returnTab, culture["Common.Error.TryAgain"]);
        }

        var url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(
            BuildCareerUrl(returnTab),
            "saved",
            "1");
        return SeeOther(url);
    }

    internal static IResult RedirectWithError(string tab, string message)
    {
        var url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(
            BuildCareerUrl(tab),
            "error",
            message);
        return SeeOther(url);
    }

    internal static string BuildCareerUrl(string tab, IReadOnlyCollection<string>? expandedGroups = null)
    {
        var url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString("/carriere", "tab", tab);
        if (expandedGroups is { Count: > 0 })
        {
            url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(
                url,
                "meer",
                string.Join(",", expandedGroups));
        }

        return url;
    }

    private static RedirectHttpResult SeeOther(string url)
        => TypedResults.Redirect(url, permanent: false, preserveMethod: false);
}
