using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;
using Jobsy.Web.Services;
using Jobsy.Web.Components.Candidate.Onboarding;

namespace Jobsy.Web.Onboarding;

internal static class OnboardingKompasSsrData
{
    internal sealed record PageData(
        string FirstName,
        OnboardingState State,
        OnboardingImpression? Impression,
        int MatchCount,
        int MaxTravelMinutes,
        string? HomeLabel,
        OnboardingFinaleCardsInput CardsInput,
        FeatureFlagSnapshot Flags);

    internal static async Task<PageData?> LoadAsync(
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        CancellationToken cancellationToken = default)
    {
        var onboardingTask = api.GetMyOnboardingAsync(cancellationToken);
        var profileTask = api.GetMyProfileAsync(cancellationToken);
        var flagsTask = featureFlags.GetAsync(cancellationToken).AsTask();
        var nearbyTask = api.GetOnboardingNearbySignalAsync(cancellationToken);
        await Task.WhenAll(onboardingTask, profileTask, flagsTask, nearbyTask);

        var state = await onboardingTask ?? new OnboardingState();
        var profile = await profileTask;
        var flags = await flagsTask;
        var nearby = await nearbyTask;

        var firstName = string.IsNullOrWhiteSpace(profile?.FirstName) ? "" : profile!.FirstName.Trim();
        var draft = OnboardingProfileDraftHydrator.FromProfile(profile);
        var maxTravel = draft.MaxTravel > 0 ? draft.MaxTravel : 30;
        var homeLabel = ResolveHomeCityLabel(profile, draft);
        var impression = state.Impression;
        var matchCount = impression?.MatchingVacancyCount ?? 0;
        if (matchCount < 0)
        {
            matchCount = 0;
        }

        if (nearby is { EmployersFeatureOff: false })
        {
            if (nearby.MaxTravelMinutes > 0)
            {
                maxTravel = nearby.MaxTravelMinutes;
            }

            if (nearby.Count > matchCount)
            {
                matchCount = nearby.Count;
            }
        }

        var cardsInput = new OnboardingFinaleCardsInput(
            flags.CandidatePassportEnabled,
            ReferenceVerificationEnabled: false,
            flags.ProactiveMatchingEnabled,
            flags.KandidatenkaartEnabled,
            DeeperTestsComplete: false,
            HasDreamJob: false);

        return new PageData(
            firstName,
            state,
            impression,
            matchCount,
            maxTravel,
            homeLabel,
            cardsInput,
            flags);
    }

    internal static string FullTestsUrl => OnboardingRoutes.KompasHubPath + "#kompas-full-tests";

    internal static string? ResolveHomeCityLabel(MeProfile? profile, OnboardingProfileDraft draft)
    {
        var fromStored = HomeAddressCity.From(profile?.Preferences?.HomeAddress);
        if (!string.IsNullOrWhiteSpace(fromStored))
        {
            return fromStored;
        }

        var fromQuery = HomeAddressCity.From(draft.AddressQuery);
        if (!string.IsNullOrWhiteSpace(fromQuery))
        {
            return fromQuery;
        }

        var fromCity = HomeAddressCity.From(draft.City);
        if (!string.IsNullOrWhiteSpace(fromCity))
        {
            return fromCity;
        }

        return string.IsNullOrWhiteSpace(draft.City) ? null : draft.City.Trim();
    }
}
