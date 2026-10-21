using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Web.Discovery;

internal static class OnboardingProfileDraftHydrator
{
    public static void ApplyProfile(OnboardingProfileDraft draft, MeProfile profile)
    {
        draft.FirstName = profile.FirstName ?? "";
        draft.LastName = profile.LastName ?? "";
        draft.Phone = profile.PhoneNumber ?? "";
        draft.WhatsAppAllowed = profile.WhatsAppContactAllowed;
        draft.BirthDate = profile.DateOfBirth;
        draft.HomeLat = profile.HomeLatitude;
        draft.HomeLng = profile.HomeLongitude;
        draft.HasConsent = profile.TestAiConsentAt is not null;
        draft.ConsentRequested = draft.HasConsent;

        var preferences = profile.Preferences ?? new CandidatePreferences();
        CandidateHomeAddressLookup.HydrateDraftFromStored(draft, preferences.HomeAddress);
        draft.MinHours = preferences.MinHoursPerWeek ?? 8;
        draft.MaxHours = preferences.MaxHoursPerWeek ?? 24;
        draft.MaxTravel = preferences.MaxTravelMinutes ?? 30;
        draft.Transport = TransportLabels.Normalize(preferences.PreferredTransport);
        draft.Immediate = profile.AvailableFromDate is null;
        draft.AvailableFrom = profile.AvailableFromDate;
        draft.Presets.Clear();
        foreach (var preset in preferences.AvailabilityPresets.Where(AvailabilityPresetRules.IsKnown))
        {
            draft.Presets.Add(preset);
        }

        draft.Availability.Clear();
        foreach (var (day, parts) in preferences.Availability)
        {
            foreach (var part in parts)
            {
                draft.Availability.Add($"{day}:{part}");
            }
        }
    }

    public static CandidatePreferences BuildPreferences(OnboardingProfileDraft draft)
    {
        var availability = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in draft.Availability)
        {
            var parts = token.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                continue;
            }

            if (!availability.TryGetValue(parts[0], out var list))
            {
                list = [];
                availability[parts[0]] = list;
            }

            list.Add(parts[1]);
        }

        return new CandidatePreferences
        {
            HomeAddress = string.IsNullOrWhiteSpace(draft.AddressQuery) ? draft.City : draft.AddressQuery,
            MinHoursPerWeek = draft.MinHours,
            MaxHoursPerWeek = draft.MaxHours,
            MaxTravelMinutes = draft.MaxTravel,
            PreferredTransport = string.IsNullOrWhiteSpace(draft.Transport) ? null : draft.Transport,
            AvailabilityPresets = draft.Presets.ToList(),
            AvailabilityPresetsOverridden = draft.PresetsOverridden,
            Availability = availability,
        };
    }
}
