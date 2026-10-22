using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Jobsy.Web.Passport;

internal static class PassportClientDataSaver
{
    internal static async Task<IResult> SaveAsync(
        IFormCollection form,
        JobsyApiClient api,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        var firstName = (form["firstName"].ToString() ?? "").Trim();
        var lastName = (form["lastName"].ToString() ?? "").Trim();
        var phone = (form["phoneNumber"].ToString() ?? "").Trim();
        var aboutMe = (form["aboutMe"].ToString() ?? "").Trim();
        var returnTab = PassportTabs.Normalize(form["returnTab"].ToString());

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            return RedirectWithError(returnTab, culture["Common.Error.TryAgain"]);
        }

        try
        {
            var profile = await api.GetMyProfileAsync(cancellationToken) ?? new MeProfile();
            var preferences = profile.Preferences ?? new CandidatePreferences();
            if (!string.IsNullOrWhiteSpace(aboutMe) || !string.IsNullOrWhiteSpace(preferences.AboutMe))
            {
                preferences.AboutMe = string.IsNullOrWhiteSpace(aboutMe) ? null : aboutMe;
            }

            await api.UpdateMyProfileAsync(
                preferences: preferences,
                firstName: firstName,
                lastName: lastName,
                phoneNumber: string.IsNullOrWhiteSpace(phone) ? null : phone,
                ct: cancellationToken);
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
            PassportRedirects.BuildPassportUrl(returnTab),
            "saved",
            "1");
        return SeeOther(url);
    }

    internal static IResult RedirectWithError(string tab, string message)
    {
        var url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(
            PassportRedirects.BuildPassportUrl(tab),
            "error",
            message);
        return SeeOther(url);
    }

    private static RedirectHttpResult SeeOther(string url)
        => TypedResults.Redirect(url, permanent: false, preserveMethod: false);
}
