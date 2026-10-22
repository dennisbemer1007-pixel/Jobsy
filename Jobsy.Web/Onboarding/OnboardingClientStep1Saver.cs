using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Web.Onboarding;

/// <summary>BFF save for onboarding step 1 (profile + optional advance to step 2).</summary>
public static class OnboardingClientStep1Saver
{
    public static async Task<(int StatusCode, object Body)> SaveAsync(
        OnboardingClientStep1Request request,
        JobsyApiClient api,
        IGeocodingClient geocoder,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        MeProfile profile;
        try
        {
            profile = await api.GetMyProfileAsync(cancellationToken) ?? new MeProfile();
        }
        catch (Exception)
        {
            return (StatusCodes.Status502BadGateway, new { message = culture["Common.Error.TryAgain"] });
        }

        var draft = OnboardingProfileDraftHydrator.FromProfile(profile);
        OnboardingClientStepBridge.ApplyStep1(draft, request);

        await CandidateHomeAddressLookup.TryResolveTypedAddressAsync(draft, geocoder, cancellationToken);

        if (request.Advance)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var issues = OnboardingWizardValidation.ValidatePersonal(
                draft.FirstName,
                draft.LastName,
                draft.BirthDate,
                draft.City,
                draft.HomeLat,
                draft.HomeLng,
                draft.Postcode,
                draft.PostcodeResolved,
                today,
                draft.GeolocationPendingConfirm);
            if (issues.Count > 0)
            {
                return (StatusCodes.Status400BadRequest, new { message = culture[issues[0].MessageKey] });
            }
        }

        try
        {
            await api.UpdateMyProfileAsync(
                dateOfBirth: draft.BirthDate,
                preferences: new CandidatePreferences
                {
                    HomeAddress = BuildHomeAddress(draft),
                    WorkStatuses = draft.WorkStatuses.OrderBy(x => x).ToList(),
                },
                homeLatitude: draft.HomeLat,
                homeLongitude: draft.HomeLng,
                clearHomeLocation: string.IsNullOrWhiteSpace(BuildHomeAddress(draft)),
                firstName: draft.FirstName.Trim(),
                lastName: draft.LastName.Trim(),
                phoneNumber: string.IsNullOrWhiteSpace(draft.Phone) ? null : draft.Phone.Trim(),
                ct: cancellationToken);
        }
        catch (ApiErrorException ex)
        {
            return (StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
        catch (Exception)
        {
            return (StatusCodes.Status502BadGateway, new { message = culture["Common.Error.TryAgain"] });
        }

        if (!request.Advance)
        {
            return (StatusCodes.Status200OK, new { saved = true });
        }

        try
        {
            await api.SaveMyOnboardingProgressAsync(2, stepCompleted: true, ct: cancellationToken);
            api.InvalidateMeCache(JobsyApiClient.OnboardingCacheKey);
        }
        catch (Exception)
        {
            return (StatusCodes.Status502BadGateway, new { message = culture["Onboarding.NotSaved"] });
        }

        return (StatusCodes.Status200OK, new { redirectUrl = "/candidate/start" });
    }

    private static string BuildHomeAddress(OnboardingProfileDraft draft)
        => CandidateHomeAddressLookup.BuildHomeAddressForSave(draft);
}
