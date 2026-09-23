using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace Jobsy.Web.Services;

/// <summary>
/// Circuit-scoped Match gate state: loads Wie ben ik–style completeness and keeps
/// <see cref="MatchProfileGateViewModel.IsProfileComplete"/> in sync for the Match tab.
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

        var educations = profile?.Preferences?.Educations ?? [];
        var hasEducation = MatchProfileCompleteness.HasEducationLevel(educations);
        var basics = whoAmI?.ProfileFilled == true;
        var competency = whoAmI?.CompetencyCompleted == true;
        var career = whoAmI?.CareerCompleted == true;
        var disc = whoAmI?.DiscCompleted == true;

        gate.ProfileBasicsFilled = basics;
        gate.HasEducationLevel = hasEducation;
        gate.CompetencyCompleted = competency;
        gate.CareerCompleted = career;
        gate.DiscCompleted = disc;
        gate.IsProfileComplete = MatchProfileCompleteness.IsProfileComplete(
            basics, hasEducation, competency, career, disc);
        gate.CompletedCount = MatchProfileCompleteness.CompletedCount(
            basics, hasEducation, competency, career, disc);
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
