using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Web.Passport;

internal static class PassportClientCvConfirmSaver
{
    internal static async Task<IResult> ConfirmAsync(
        IFormCollection form,
        JobsyApiClient api,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        var fields = form["cvField"]
            .Select(v => v?.ToString() ?? "")
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (fields.Count == 0)
        {
            return RedirectWithError(culture["Profile.CvProposalConfirm"]);
        }

        try
        {
            var updated = await api.ConfirmMyCvExtractionAsync(fields, cancellationToken);
            if (updated is null)
            {
                return RedirectWithError(culture["Common.Error.TryAgain"]);
            }
        }
        catch (ApiErrorException ex)
        {
            return RedirectWithError(ex.UserMessage ?? culture["Common.Error.TryAgain"]);
        }
        catch (Exception)
        {
            return RedirectWithError(culture["Common.Error.TryAgain"]);
        }

        var url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(
            PassportRedirects.BuildPassportUrl(PassportTabs.Proof),
            "saved",
            "1");
        return Results.Redirect(url);
    }

    private static IResult RedirectWithError(string message)
    {
        var url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(
            PassportRedirects.BuildPassportUrl(PassportTabs.Proof),
            "error",
            message);
        return Results.Redirect(url);
    }
}
