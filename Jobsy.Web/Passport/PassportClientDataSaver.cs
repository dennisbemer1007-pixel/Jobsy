using Jobsy.Core.Rules;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Auth;
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

    internal static async Task<IResult> SaveAvailabilityAsync(
        IFormCollection form,
        JobsyApiClient api,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        var returnTab = PassportTabs.Normalize(form["returnTab"].ToString());
        var openSection = (form["openSection"].ToString() ?? "availability").Trim();

        try
        {
            var profile = await api.GetMyProfileAsync(cancellationToken) ?? new MeProfile();
            var preferences = profile.Preferences ?? new CandidatePreferences();
            var preset = (form["availabilityPreset"].ToString() ?? "").Trim();
            var flexibleChecked = string.Equals(form["flexibleTimes"].ToString(), "true", StringComparison.Ordinal);
            var matrixKey = (form["passportMatrix"].ToString() ?? "").Trim();

            if (!string.IsNullOrWhiteSpace(matrixKey))
            {
                var keys = PassportAvailabilityMatrix.ToggleKey(preferences.Availability, matrixKey);
                preferences.Availability = PassportAvailabilityMatrix.FromSelectedKeys(keys);
                preferences.FlexibleTimes = false;
                if (preferences.MinHoursPerWeek is not > 0)
                {
                    preferences.MinHoursPerWeek = 8;
                }

                if (preferences.MaxHoursPerWeek is not decimal maxHours
                    || maxHours < (preferences.MinHoursPerWeek ?? 8))
                {
                    preferences.MaxHoursPerWeek = Math.Max(preferences.MinHoursPerWeek ?? 8, 24);
                }

                await api.UpdateMyProfileAsync(preferences: preferences, ct: cancellationToken);
                return RedirectAfterAvailabilitySave(returnTab, openSection, form);
            }

            if (string.Equals(preset, CandidateAvailabilityPresets.Immediate, StringComparison.OrdinalIgnoreCase))
            {
                var patch = CandidateAvailabilityPresets.Apply(CandidateAvailabilityPresets.Immediate);
                preferences.FlexibleTimes = patch.FlexibleTimes;
                preferences.MinHoursPerWeek = patch.MinHoursPerWeek;
                preferences.MaxHoursPerWeek = patch.MaxHoursPerWeek;
                preferences.Availability = patch.Availability.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.ToList(),
                    StringComparer.OrdinalIgnoreCase);

                await api.UpdateMyProfileAsync(
                    preferences: preferences,
                    openForWork: true,
                    clearAvailableFromDate: true,
                    ct: cancellationToken);
                return RedirectAfterAvailabilitySave(returnTab, openSection, form);
            }
            else if (flexibleChecked)
            {
                preferences.FlexibleTimes = true;
            }
            else if (PassportAvailabilityMatrix.ToSelectedKeys(preferences.Availability).Count > 0)
            {
                preferences.FlexibleTimes = false;
            }
            else
            {
                return RedirectWithError(returnTab, culture["Common.Error.TryAgain"], openSection);
            }

            await api.UpdateMyProfileAsync(preferences: preferences, ct: cancellationToken);
        }
        catch (ApiErrorException ex)
        {
            return RedirectWithError(returnTab, ex.UserMessage ?? culture["Common.Error.TryAgain"], openSection);
        }
        catch (Exception)
        {
            return RedirectWithError(returnTab, culture["Common.Error.TryAgain"], openSection);
        }

        return RedirectAfterAvailabilitySave(returnTab, openSection, form);
    }

    private static RedirectHttpResult RedirectAfterAvailabilitySave(
        string returnTab,
        string openSection,
        IFormCollection form)
    {
        var returnUrl = (form["returnUrl"].ToString() ?? "").Trim();
        var safe = AuthRedirects.SafeLocalUrl(returnUrl);
        if (!string.IsNullOrWhiteSpace(safe))
        {
            var back = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(safe, "saved", "1");
            return TypedResults.Redirect(back);
        }

        var url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(
            PassportRedirects.BuildPassportUrl(returnTab),
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["saved"] = "1",
                ["section"] = openSection,
            });
        return SeeOther(url);
    }

    internal static IResult RedirectWithError(string tab, string message, string? section = null)
    {
        var url = PassportRedirects.BuildPassportUrl(tab);
        url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(url, "error", message);
        if (!string.IsNullOrWhiteSpace(section))
        {
            url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(url, "section", section);
        }

        return SeeOther(url);
    }

    private static RedirectHttpResult SeeOther(string url)
        => TypedResults.Redirect(url, permanent: false, preserveMethod: false);
}
