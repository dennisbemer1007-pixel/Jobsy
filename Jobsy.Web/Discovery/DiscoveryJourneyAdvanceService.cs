using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Discovery;
using Jobsy.Web.Models;
using Jobsy.Web.Onboarding;
using Jobsy.Web.Services;

namespace Jobsy.Web.Discovery;

/// <summary>Server-side advance for De ontdekkingsreis (no Blazor circuit on Volgende).</summary>
public sealed class DiscoveryJourneyAdvanceService(JobsyApiClient api)
{
    public async Task<(bool Ok, string? Redirect, string? Error)> AdvanceNextAsync(
        int step,
        string screen,
        string clientJson,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(screen, "toestemming", StringComparison.OrdinalIgnoreCase))
        {
            return (false, null, "Toestemming via deze knop nog niet ondersteund.");
        }

        if (step is < 1 or > 10)
        {
            return (false, null, "Ongeldige stap.");
        }

        var profile = await api.GetMyProfileAsync(cancellationToken);
        if (profile is null)
        {
            return (false, null, "Profiel kon niet worden geladen.");
        }

        var draft = new OnboardingProfileDraft();
        OnboardingProfileDraftHydrator.ApplyProfile(draft, profile);

        try
        {
            if (step == 1)
            {
                OnboardingClientStepBridge.Apply(1, draft, clientJson);
            }
            else if (step is >= 2 and <= 6)
            {
                DiscoveryClientStepBridge.Apply(step, draft, clientJson);
            }
            else
            {
                return (false, null, "Deze stap vereist nog een circuit-update.");
            }
        }
        catch
        {
            return (false, null, "Invoer kon niet worden gelezen.");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var issues = step switch
        {
            1 => OnboardingWizardValidation.ValidatePersonal(
                draft.FirstName,
                draft.LastName,
                draft.BirthDate,
                draft.City,
                draft.HomeLat,
                draft.HomeLng,
                draft.Postcode,
                draft.PostcodeResolved,
                today,
                draft.GeolocationPendingConfirm),
            2 => OnboardingWizardValidation.ValidateAvailability(
                    draft.MinHours,
                    draft.MaxHours,
                    draft.Availability.Count,
                    draft.Immediate,
                    draft.AvailableFrom,
                    requirePresetSelection: draft.Presets.Count == 0 && draft.Availability.Count == 0)
                .Concat(OnboardingWizardValidation.ValidateTransport(draft.Transport, draft.MaxTravel))
                .ToList(),
            _ => Array.Empty<OnboardingValidationIssue>(),
        };

        if (issues.Count > 0)
        {
            return (false, null, "Controleer je invoer en probeer opnieuw.");
        }

        await api.UpdateMyProfileAsync(
            firstName: draft.FirstName.Trim(),
            lastName: draft.LastName.Trim(),
            phoneNumber: string.IsNullOrWhiteSpace(draft.Phone) ? null : draft.Phone.Trim(),
            whatsAppContactAllowed: draft.WhatsAppAllowed && !string.IsNullOrWhiteSpace(draft.Phone),
            dateOfBirth: draft.BirthDate,
            homeLatitude: draft.HomeLat,
            homeLongitude: draft.HomeLng,
            preferences: OnboardingProfileDraftHydrator.BuildPreferences(draft),
            ct: cancellationToken);

        var next = Math.Min(10, step + 1);
        await api.SaveMyOnboardingProgressAsync(next, stepCompleted: true, ct: cancellationToken);
        return (true, $"/candidate/ontdekkingsreis?stap={next}", null);
    }
}
