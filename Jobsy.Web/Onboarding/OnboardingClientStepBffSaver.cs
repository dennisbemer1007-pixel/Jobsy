using System.Text.Json;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Web.Onboarding;

public static class OnboardingClientStepBffSaver
{
    private const string WelcomeDismissedSource = "welcome";
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public static async Task<(int StatusCode, object Body)> SaveStepAsync(
        int step,
        JsonElement body,
        JobsyApiClient api,
        IGeocodingClient geocoder,
        IFeatureFlags featureFlags,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        var advance = body.TryGetProperty("advance", out var adv) && adv.GetBoolean();
        var skipOptional = body.TryGetProperty("skipOptional", out var sk) && sk.GetBoolean();
        var goBack = body.TryGetProperty("back", out var backProp) && backProp.GetBoolean();

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
        var payloadJson = body.GetRawText();

        if (step == 1)
        {
            var req = JsonSerializer.Deserialize<OnboardingClientStep1Request>(payloadJson, Json) ?? new OnboardingClientStep1Request();
            req.Advance = advance;
            return await OnboardingClientStep1Saver.SaveAsync(req, api, geocoder, culture, cancellationToken);
        }

        if (step is >= 2 and <= 6)
        {
            OnboardingClientStepBridge.Apply(step, draft, payloadJson);

            if (advance && !skipOptional)
            {
                var issues = ValidateStep(step, draft);
                if (issues.Count > 0)
                {
                    return (StatusCodes.Status400BadRequest, new { message = culture[issues[0].MessageKey] });
                }
            }

            try
            {
                await OnboardingProfileDraftPersistence.SaveProfileAsync(draft, api, cancellationToken);
                api.InvalidateMeCache();
                if (step == 6)
                {
                    await api.SaveOnboardingDreamJobAsync(
                        string.IsNullOrWhiteSpace(draft.Dream) ? null : draft.Dream.Trim(),
                        cancellationToken);
                }
            }
            catch (ApiErrorException ex)
            {
                return (StatusCodes.Status502BadGateway, new { message = ex.Message });
            }
            catch (Exception)
            {
                return (StatusCodes.Status502BadGateway, new { message = culture["Common.Error.TryAgain"] });
            }

            if (goBack)
            {
                if (step <= 1)
                {
                    return (StatusCodes.Status400BadRequest, new { message = culture["Onboarding.NotSaved"] });
                }

                try
                {
                    await OnboardingClientProgressSaver.SaveStickyAsync(api, step - 1, ct: cancellationToken);
                    api.InvalidateMeCache(JobsyApiClient.OnboardingCacheKey);
                    return (StatusCodes.Status200OK, new { redirectUrl = OnboardingRoutes.StepUrl(step - 1) });
                }
                catch (Exception)
                {
                    return (StatusCodes.Status502BadGateway, new { message = culture["Onboarding.NotSaved"] });
                }
            }

            if (!advance)
            {
                return (StatusCodes.Status200OK, new { saved = true });
            }

            var nextStep = step + 1;
            try
            {
                await OnboardingClientProgressSaver.SaveStickyAsync(
                    api,
                    nextStep,
                    stepCompleted: !skipOptional,
                    stepSkipped: skipOptional,
                    ct: cancellationToken);
                api.InvalidateMeCache(JobsyApiClient.OnboardingCacheKey);
            }
            catch (Exception)
            {
                return (StatusCodes.Status502BadGateway, new { message = culture["Onboarding.NotSaved"] });
            }

            return (StatusCodes.Status200OK, new { redirectUrl = OnboardingRoutes.StepUrl(nextStep) });
        }

        if (step is >= 7 and <= 10)
        {
            if (step == 7 && !draft.HasConsent)
            {
                draft.ConsentRequested = OnboardingClientStepBridge.ParseStep7Consent(payloadJson);
                if (advance)
                {
                    var consentIssues = OnboardingWizardValidation.ValidateConsent(draft.ConsentRequested);
                    if (consentIssues.Count > 0)
                    {
                        return (StatusCodes.Status400BadRequest, new { message = culture[consentIssues[0].MessageKey] });
                    }
                }

                if (draft.ConsentRequested)
                {
                    try
                    {
                        var updated = await api.AcceptTestAiConsentAsync(cancellationToken);
                        draft.HasConsent = updated?.TestAiConsentAt is not null;
                        if (!draft.HasConsent)
                        {
                            return (StatusCodes.Status502BadGateway, new { message = culture["Onboarding.Consent.Failed"] });
                        }
                    }
                    catch (Exception)
                    {
                        return (StatusCodes.Status502BadGateway, new { message = culture["Onboarding.Consent.Failed"] });
                    }
                }
            }

            var testsV2 = await featureFlags.IsEnabledAsync(PlatformFeature.TestsV2, cancellationToken);
            var answers = OnboardingClientStepBridge.ParseMiniAnswers(payloadJson);
            var version = await ResolveQuickScanVersionAsync(step, api, testsV2, cancellationToken);
            var merged = await MergeMiniAnswersAsync(step, api, answers, cancellationToken);

            if (merged.Count > 0 || draft.HasConsent || step > 7)
            {
                try
                {
                    await PersistMiniAnswersAsync(step, merged, version, api, cancellationToken);
                }
                catch (TestSaveClientException ex)
                {
                    return (StatusCodes.Status502BadGateway, new { message = ex.Message });
                }
                catch (ApiErrorException ex)
                {
                    return (StatusCodes.Status502BadGateway, new { message = ex.Message });
                }
                catch (Exception)
                {
                    return (StatusCodes.Status502BadGateway, new { message = culture["Common.Error.TryAgain"] });
                }
            }

            if (goBack)
            {
                if (step <= 1)
                {
                    return (StatusCodes.Status400BadRequest, new { message = culture["Onboarding.NotSaved"] });
                }

                try
                {
                    await OnboardingClientProgressSaver.SaveStickyAsync(api, step - 1, ct: cancellationToken);
                    api.InvalidateMeCache(JobsyApiClient.OnboardingCacheKey);
                    return (StatusCodes.Status200OK, new { redirectUrl = OnboardingRoutes.StepUrl(step - 1) });
                }
                catch (Exception)
                {
                    return (StatusCodes.Status502BadGateway, new { message = culture["Onboarding.NotSaved"] });
                }
            }

            if (!advance)
            {
                return (StatusCodes.Status200OK, new { saved = true });
            }

            if (!OnboardingMiniBlockCompletion.IsMiniBlockComplete(step, version, merged))
            {
                return (StatusCodes.Status400BadRequest, new { message = culture["Onboarding.NotSaved"] });
            }

            try
            {
                if (step == 10)
                {
                    await OnboardingClientProgressSaver.SaveStickyAsync(
                        api,
                        10,
                        stepCompleted: true,
                        finishReached: true,
                        ct: cancellationToken);
                    api.InvalidateMeCache();
                    try
                    {
                        await api.UpdateMyProfileAsync(openForWork: true, ct: cancellationToken);
                    }
                    catch (Exception)
                    {
                        // Kompas unlock must not fail if open-for-work sync hiccups; user can fix in profile.
                    }

                    return (StatusCodes.Status200OK, new { redirectUrl = OnboardingRoutes.KompasReadyPath });
                }

                await OnboardingClientProgressSaver.SaveStickyAsync(
                    api,
                    step + 1,
                    stepCompleted: true,
                    ct: cancellationToken);
                api.InvalidateMeCache(JobsyApiClient.OnboardingCacheKey);
                return (StatusCodes.Status200OK, new { redirectUrl = OnboardingRoutes.StepUrl(step + 1) });
            }
            catch (Exception)
            {
                return (StatusCodes.Status502BadGateway, new { message = culture["Onboarding.NotSaved"] });
            }
        }

        return (StatusCodes.Status404NotFound, new { message = "Onbekende stap." });
    }

    public static async Task<(int StatusCode, object Body)> BeginWelcomeAsync(
        JobsyApiClient api,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        try
        {
            var state = await api.GetMyOnboardingAsync(cancellationToken);
            var step = Math.Clamp(state?.CurrentStep ?? 1, 1, 10);
            await OnboardingClientProgressSaver.SaveStickyAsync(
                api,
                step,
                source: WelcomeDismissedSource,
                ct: cancellationToken);
            api.InvalidateMeCache(JobsyApiClient.OnboardingCacheKey);
            return (StatusCodes.Status200OK, new { redirectUrl = OnboardingRoutes.StepUrl(1) });
        }
        catch (Exception)
        {
            return (StatusCodes.Status502BadGateway, new { message = culture["Common.Error.TryAgain"] });
        }
    }

    public static async Task<(int StatusCode, object Body)> CompleteKompasAsync(
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        try
        {
            var passportOn = await featureFlags.IsEnabledAsync(PlatformFeature.CandidatePassport, cancellationToken);
            if (passportOn)
            {
                await api.CompleteMyOnboardingAsync(cancellationToken);
                return (StatusCodes.Status200OK, new { redirectUrl = FeatureRoutes.CandidatePassportPath });
            }

            await api.CompleteMyOnboardingAsync(cancellationToken);
            return (StatusCodes.Status200OK, new { redirectUrl = FeatureRoutes.CandidateKompasHubPath });
        }
        catch (Exception)
        {
            return (StatusCodes.Status502BadGateway, new { message = culture["Common.Error.TryAgain"] });
        }
    }

    private static IReadOnlyList<OnboardingValidationIssue> ValidateStep(int step, OnboardingProfileDraft draft)
        => step switch
        {
            2 => OnboardingWizardValidation.ValidateAvailability(
                draft.MinHours,
                draft.MaxHours,
                draft.Availability.Count,
                draft.Immediate,
                draft.AvailableFrom,
                requirePresetSelection: draft.Presets.Count == 0 && draft.Availability.Count == 0),
            3 => OnboardingWizardValidation.ValidateTransport(draft.Transport, draft.MaxTravel),
            6 => OnboardingWizardValidation.ValidateDream(draft.Dream),
            _ => Array.Empty<OnboardingValidationIssue>(),
        };

    private static async Task<int> ResolveQuickScanVersionAsync(
        int step,
        JobsyApiClient api,
        bool testsV2,
        CancellationToken cancellationToken)
    {
        int? version = step switch
        {
            7 => (await api.GetMyCompetenciesAsync(cancellationToken))?.QuickScanVersion,
            8 => (await api.GetMyCareerInterestsAsync(cancellationToken))?.QuickScanVersion,
            9 => (await api.GetMyCultureAsync(cancellationToken))?.QuickScanVersion,
            10 => (await api.GetMyValuesAsync(cancellationToken))?.QuickScanVersion,
            _ => null,
        };

        return OnboardingMiniBlockCompletion.ResolveQuickScanVersion(version, testsV2);
    }

    private static async Task<Dictionary<int, int>> MergeMiniAnswersAsync(
        int step,
        JobsyApiClient api,
        Dictionary<int, int> incoming,
        CancellationToken cancellationToken)
    {
        var baseline = await LoadServerMiniAnswersAsync(step, api, cancellationToken);
        foreach (var (id, value) in incoming)
        {
            baseline[id] = value;
        }

        return baseline;
    }

    private static async Task<Dictionary<int, int>> LoadServerMiniAnswersAsync(
        int step,
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        Dictionary<string, int>? answers = step switch
        {
            7 => (await api.GetMyCompetenciesAsync(cancellationToken))?.Answers,
            8 => (await api.GetMyCareerInterestsAsync(cancellationToken))?.Answers,
            9 => (await api.GetMyCultureAsync(cancellationToken))?.Answers,
            10 => (await api.GetMyValuesAsync(cancellationToken))?.Answers,
            _ => null,
        };

        var map = new Dictionary<int, int>();
        if (answers is null)
        {
            return map;
        }

        foreach (var (key, value) in answers)
        {
            if (int.TryParse(key, out var id) && value is >= 1 and <= 5)
            {
                map[id] = value;
            }
        }

        return map;
    }

    private static async Task PersistMiniAnswersAsync(
        int step,
        IReadOnlyDictionary<int, int> merged,
        int version,
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        var kind = OnboardingMiniBlockCompletion.AssessmentKindForWizardStep(step);
        var payload = OnboardingMiniBlockCompletion.ScopeToMiniIds(kind, version, merged);
        switch (step)
        {
            case 7:
                await api.SaveMyCompetenciesAsync(payload, false, cancellationToken);
                break;
            case 8:
                await api.SaveMyCareerInterestsAsync(payload, false, cancellationToken);
                break;
            case 9:
                await api.SaveMyCultureAsync(payload, false, cancellationToken);
                break;
            case 10:
                await api.SaveMyValuesAsync(payload, false, cancellationToken);
                break;
        }
    }
}
