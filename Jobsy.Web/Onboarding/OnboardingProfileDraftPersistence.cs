using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Web.Onboarding;

internal static class OnboardingProfileDraftPersistence
{
    public static Task SaveProfileAsync(
        OnboardingProfileDraft draft,
        JobsyApiClient api,
        CancellationToken cancellationToken)
        => api.UpdateMyProfileAsync(
            dateOfBirth: draft.BirthDate,
            preferences: new CandidatePreferences
            {
                MinHoursPerWeek = draft.MinHours,
                MaxHoursPerWeek = draft.MaxHours,
                MaxTravelMinutes = draft.MaxTravel,
                PreferredTransport = string.IsNullOrWhiteSpace(draft.Transport) ? null : draft.Transport,
                Availability = BuildAvailability(draft),
                AvailabilityPresets = draft.Presets.ToList(),
                AvailabilityPresetsOverridden = draft.PresetsOverridden,
                Employers = draft.JobsForSave(),
                NoWorkExperience = draft.NoWork,
                Educations = string.IsNullOrWhiteSpace(draft.Education)
                    ? []
                    : [OnboardingWizardCatalog.FormatEducationLine(draft.Education, draft.EducationDirection)],
                EducationDirection = EducationDirectionCatalog.SupportsDirection(draft.Education)
                                     && !string.IsNullOrWhiteSpace(draft.EducationDirection)
                    ? draft.EducationDirection.Trim()
                    : null,
                HomeAddress = BuildHomeAddress(draft),
                WorkStatuses = draft.WorkStatuses.OrderBy(x => x).ToList(),
            },
            homeLatitude: draft.HomeLat,
            homeLongitude: draft.HomeLng,
            clearHomeLocation: string.IsNullOrWhiteSpace(BuildHomeAddress(draft)),
            firstName: draft.FirstName.Trim(),
            lastName: draft.LastName.Trim(),
            phoneNumber: string.IsNullOrWhiteSpace(draft.Phone) ? null : draft.Phone.Trim(),
            availableFromDate: draft.Immediate ? null : draft.AvailableFrom,
            clearAvailableFromDate: draft.Immediate,
            ct: cancellationToken);

    private static string BuildHomeAddress(OnboardingProfileDraft draft)
        => CandidateHomeAddressLookup.BuildHomeAddressForSave(draft);

    private static Dictionary<string, List<string>> BuildAvailability(OnboardingProfileDraft draft)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in draft.Availability)
        {
            var pair = key.Split(':', 2);
            if (pair.Length != 2)
            {
                continue;
            }

            if (!result.TryGetValue(pair[0], out var values))
            {
                result[pair[0]] = values = [];
            }

            values.Add(pair[1]);
        }

        return result;
    }
}
