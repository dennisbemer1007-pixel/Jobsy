using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Discovery;
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

        draft.AboutMe = preferences.AboutMe;
        draft.DutchLevel = preferences.DutchLevel;
        draft.Certificates.Clear();
        draft.Certificates.AddRange(preferences.Certificates ?? []);
        draft.LearningGoals.Clear();
        draft.LearningGoals.AddRange((preferences.LearningGoals ?? [])
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .Take(DiscoveryCatalogs.MaxLearningGoals));
        draft.EmployerPreferences.Clear();
        foreach (var code in preferences.EmployerPreferences ?? [])
        {
            if (DiscoveryCatalogs.IsKnownEmployerPreference(code))
            {
                draft.EmployerPreferences.Add(DiscoveryCatalogs.CanonicalEmployer(code)!);
            }
        }

        draft.WorkStatuses.Clear();
        foreach (var code in preferences.WorkStatuses ?? [])
        {
            if (WorkStatusCatalog.IsKnown(code))
            {
                draft.WorkStatuses.Add(WorkStatusCatalog.Canonical(code)!);
            }
        }

        draft.Roles.Clear();
        foreach (var role in preferences.Roles ?? [])
        {
            var label = WorkTypeLabels.Expand(WorkTypeLabels.Parse(role)).FirstOrDefault()
                        ?? WorkTypeLabels.All.FirstOrDefault(b => string.Equals(b, role, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(label))
            {
                draft.Roles.Add(label);
            }
        }

        draft.Hobbies.Clear();
        draft.HobbyFreeText.Clear();
        foreach (var hobby in preferences.Hobbies ?? [])
        {
            if (string.IsNullOrWhiteSpace(hobby))
            {
                continue;
            }

            if (DiscoveryCatalogs.IsKnownHobby(hobby))
            {
                draft.Hobbies.Add(DiscoveryCatalogs.CanonicalHobby(hobby)!);
            }
            else
            {
                draft.HobbyFreeText.Add(hobby.Trim());
            }
        }

        draft.SpokenLanguages.Clear();
        foreach (var lang in preferences.SpokenLanguages ?? [])
        {
            var code = DiscoveryCatalogs.CanonicalLanguage(lang.Code);
            if (code is null)
            {
                continue;
            }

            draft.SpokenLanguages.Add(new CandidateLanguage { Code = code, Level = lang.Level });
        }

        draft.DrivingLicenses.Clear();
        foreach (var license in preferences.DrivingLicenses ?? [])
        {
            foreach (var code in DrivingLicenseLabels.Split(license))
            {
                draft.DrivingLicenses.Add(code);
            }
        }

        return draft;
    }

    public static void ApplyPrivatePreferences(OnboardingProfileDraft draft, CandidatePrivatePreferences? prefs)
    {
        draft.Dislikes.Clear();
        draft.CustomDislikes.Clear();
        if (prefs is null)
        {
            return;
        }

        foreach (var d in prefs.Dislikes ?? [])
        {
            if (DiscoveryCatalogs.IsKnownDislike(d))
            {
                draft.Dislikes.Add(DiscoveryCatalogs.CanonicalDislike(d)!);
            }
        }

        draft.CustomDislikes.AddRange((prefs.CustomDislikes ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Take(DiscoveryCatalogs.MaxCustomDislikes));
    }

    public static void ApplyDreamFromCareerPath(OnboardingProfileDraft draft, CareerPathPlanApiModel? careerPath)
    {
        if (!string.IsNullOrWhiteSpace(careerPath?.DreamTitle))
        {
            draft.Dream = careerPath.DreamTitle.Trim();
        }
    }
}
