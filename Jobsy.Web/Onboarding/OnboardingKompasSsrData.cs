using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;
using Jobsy.Web.Services;

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
        await Task.WhenAll(onboardingTask, profileTask, flagsTask);

        var state = await onboardingTask ?? new OnboardingState();
        var profile = await profileTask;
        var flags = await flagsTask;

        var firstName = string.IsNullOrWhiteSpace(profile?.FirstName) ? "" : profile!.FirstName.Trim();
        var draft = OnboardingProfileDraftHydrator.FromProfile(profile);
        var maxTravel = draft.MaxTravel > 0 ? draft.MaxTravel : 30;
        var homeLabel = string.IsNullOrWhiteSpace(draft.City) ? null : draft.City.Trim();
        var impression = state.Impression;
        var matchCount = impression?.MatchingVacancyCount ?? 0;
        if (matchCount < 0)
        {
            matchCount = 0;
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
}
