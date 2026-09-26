using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace Jobsy.Web.Services;

/// <summary>
/// Circuit-scoped Match gate state: loads completeness and keeps
/// <see cref="MatchProfileGateViewModel.IsProfileComplete"/> in sync for the Match tab.
/// Unlock = basics + education + onboarding wizard completed.
/// </summary>
public sealed class CandidateMatchProfileService
{
    private readonly JobsyApiClient _api;
    private readonly AuthenticationStateProvider _auth;

    public CandidateMatchProfileService(JobsyApiClient api, AuthenticationStateProvider auth)
    {
        _api = api;
        _auth = auth;
    }

    public MatchProfileGateViewModel Gate { get; private set; } = new();

    public async Task<MatchProfileGateViewModel> RefreshAsync(CancellationToken ct = default)
    {
        var state = await _auth.GetAuthenticationStateAsync();
        var authenticated = state.User.Identity?.IsAuthenticated == true;
        var gate = new MatchProfileGateViewModel { IsAuthenticated = authenticated };

        if (!authenticated)
        {
            Gate = gate;
            return Gate;
        }

        WhoAmIState? whoAmI = null;
        MeProfile? profile = null;
        OnboardingState? onboarding = null;
        try
        {
            whoAmI = await _api.GetMyWhoAmIAsync(ct);
        }
        catch
        {
            // Checklist stays incomplete when WhoAmI is unavailable.
        }

        try
        {
            profile = await _api.GetMyProfileAsync(ct);
        }
        catch
        {
            // Travel / education filters fall back to defaults.
        }

        try
        {
            onboarding = await _api.GetMyOnboardingAsync(ct);
        }
        catch
        {
            // Wizard status optional for gate.
        }

        var educations = profile?.Preferences?.Educations ?? [];
        var hasEducation = MatchProfileCompleteness.HasEducationLevel(educations);
        var basics = whoAmI?.ProfileFilled == true;
        var wizardDone = onboarding?.IsComplete == true
                         || onboarding?.CompletedAtUtc is not null;
        var competency = whoAmI?.CompetencyCompleted == true;
        var career = whoAmI?.CareerCompleted == true;
        var culture = whoAmI?.CultureCompleted == true;

        gate.ProfileBasicsFilled = basics;
        gate.HasEducationLevel = hasEducation;
        gate.WizardCompleted = wizardDone;
        gate.CompetencyCompleted = competency;
        gate.CareerCompleted = career;
        gate.CultureCompleted = culture;
        gate.HasProvisionalScores = onboarding?.Impression is { } imp
            && (imp.CompetencyProvisional || imp.CareerProvisional || imp.CultureProvisional || imp.ValuesProvisional);
        gate.IsProfileComplete = MatchProfileCompleteness.IsProfileComplete(
            basics, hasEducation, wizardDone);
        gate.CompletedCount = MatchProfileCompleteness.CompletedCount(
            basics, hasEducation, wizardDone);
        gate.RequiredCount = MatchProfileCompleteness.RequiredStepCount;
        gate.Educations = educations;
        gate.PreferredTransport = profile?.Preferences?.PreferredTransport;
        gate.MaxTravelMinutes = profile?.Preferences?.MaxTravelMinutes;
        gate.HomeLatitude = profile?.HomeLatitude;
        gate.HomeLongitude = profile?.HomeLongitude;

        Gate = gate;
        return Gate;
    }
}
