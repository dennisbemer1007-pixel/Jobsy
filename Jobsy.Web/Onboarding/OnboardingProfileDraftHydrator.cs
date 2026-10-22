using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Web.Onboarding;

internal static class OnboardingProfileDraftHydrator
{
    public static OnboardingProfileDraft FromProfile(MeProfile? profile)
    {
        var draft = new OnboardingProfileDraft();
        if (profile is null)
        {
            return draft;
        }

        (draft.FirstName, draft.LastName) = CandidateNameRules.ResolveOnboardingPrefill(
            profile.FirstName,
            profile.LastName,
            profile.FullName,
            profile.Email);
        draft.Phone = profile.PhoneNumber ?? "";
        draft.BirthDate = profile.DateOfBirth;
        if (draft.BirthDate is { } date)
        {
            draft.BirthDay = date.Day.ToString();
            draft.BirthMonth = date.Month.ToString();
            draft.BirthYear = date.Year.ToString();
        }

        draft.HasConsent = profile.TestAiConsentAt is not null;
        draft.ConsentRequested = draft.HasConsent;
        draft.HomeLat = profile.HomeLatitude;
        draft.HomeLng = profile.HomeLongitude;
        var preferences = profile.Preferences ?? new CandidatePreferences();
        CandidateHomeAddressLookup.HydrateDraftFromStored(draft, preferences.HomeAddress);
        draft.MinHours = preferences.MinHoursPerWeek ?? 8;
        draft.MaxHours = preferences.MaxHoursPerWeek ?? 24;
        draft.MaxTravel = preferences.MaxTravelMinutes ?? 30;
        draft.Transport = TransportLabels.Normalize(preferences.PreferredTransport);
        draft.Immediate = profile.AvailableFromDate is null;
        draft.AvailableFrom = profile.AvailableFromDate;
        foreach (var preset in preferences.AvailabilityPresets.Where(AvailabilityPresetRules.IsKnown))
        {
            draft.Presets.Add(preset);
        }

        draft.PresetsOverridden = preferences.AvailabilityPresetsOverridden == true;
        foreach (var (day, parts) in preferences.Availability)
        {
            foreach (var part in parts)
            {
                draft.Availability.Add($"{day}:{part}");
            }
        }

        draft.FineTune = draft.Presets.Contains(AvailabilityPresetRules.Custom);
        draft.NoWork = preferences.NoWorkExperience == true;
        draft.Jobs.Clear();
        draft.Jobs.AddRange(preferences.Employers.Take(3));
        var education = preferences.Educations.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(education))
        {
            var parts = education.Split('–', 2, StringSplitOptions.TrimEntries);
            draft.Education = parts[0];
            draft.EducationDirection = parts.Length > 1 ? parts[1] : preferences.EducationDirection ?? "";
        }

        var serverWorkStatuses = preferences.WorkStatuses ?? [];
        if (serverWorkStatuses.Count > 0)
        {
            draft.WorkStatuses.Clear();
            foreach (var code in serverWorkStatuses)
            {
                if (WorkStatusCatalog.IsKnown(code))
                {
                    draft.WorkStatuses.Add(WorkStatusCatalog.Canonical(code)!);
                }
            }
        }

        return draft;
    }
}
