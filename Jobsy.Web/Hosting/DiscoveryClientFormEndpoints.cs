using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Discovery;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Discovery;
using Jobsy.Core.Features;
using Jobsy.Web.Localization;
using Jobsy.Web.Onboarding;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Web.Hosting;

public static class DiscoveryClientFormEndpoints
{
    public static IEndpointRouteBuilder MapDiscoveryClientFormEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(DiscoverySsrUrls.Path, PostAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Candidate" });

        return app;
    }

    private static async Task<IResult> PostAsync(
        HttpContext http,
        IAntiforgery antiforgery,
        JobsyApiClient api,
        IGeocodingClient geocoder,
        IFeatureFlags featureFlags,
        CultureState culture,
        DiscoverySsrDepthStore depthStore,
        DiscoveryOverviewCache overviewCache,
        CancellationToken cancellationToken)
    {
        if (!http.Request.HasFormContentType)
        {
            return Results.BadRequest();
        }

        var form = await http.Request.ReadFormAsync(cancellationToken);
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return SeeOther(DiscoverySsrUrls.WithError(DiscoverySsrUrls.Path, culture["Common.Error.TryAgain"]));
        }

        var action = form["discoveryAction"].ToString();
        var userId = http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

        if (string.Equals(action, "pick-depth", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(form["shedStep"].ToString(), out var shedStep) || shedStep is < 7 or > 10)
            {
                return SeeOther(DiscoverySsrUrls.WithError(DiscoverySsrUrls.Path, culture["Onboarding.NotSaved"]));
            }

            if (!int.TryParse(form["level"].ToString(), out var level)
                || !JourneyTestFlow.TryParseDepth(level.ToString(System.Globalization.CultureInfo.InvariantCulture), out level))
            {
                return SeeOther(DiscoverySsrUrls.WithError(DiscoverySsrUrls.Shed(shedStep), culture["Onboarding.NotSaved"]));
            }

            depthStore.Set(userId, shedStep, level);
            return SeeOther(DiscoverySsrUrls.Shed(shedStep));
        }

        if (string.Equals(action, "dive-deeper", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(form["shedStep"].ToString(), out var shedStep) || shedStep is < 7 or > 10)
            {
                shedStep = 7;
            }

            var kind = OnboardingWizardCatalog.TestKindForStep(shedStep)!.Value;
            var answered = await CountAnsweredAsync(api, overviewCache, kind, cancellationToken);
            var band = await DiscoverySsrMiniLoader.LoadBandAsync(
                shedStep,
                OnboardingWizardCatalog.MiniLevelCount,
                api,
                featureFlags,
                culture,
                cancellationToken);
            var reached = JourneyTestFlow.ReachedLevel(answered, kind, band.QuickScanVersion);
            var selected = depthStore.GetOrDefault(
                userId,
                shedStep,
                JourneyTestFlow.DefaultSelectedLevel(answered, kind, band.QuickScanVersion));

            if (selected > reached)
            {
                return SeeOther(DiscoverySsrUrls.Step(shedStep, selected, 1));
            }

            try
            {
                _ = await api.SaveMyOnboardingProgressAsync(shedStep, stepCompleted: true, ct: cancellationToken);
            }
            catch
            {
                return SeeOther(DiscoverySsrUrls.WithError(DiscoverySsrUrls.Shed(shedStep), culture["Common.Error.TryAgain"]));
            }

            if (shedStep >= 10)
            {
                return SeeOther(DiscoverySsrUrls.WithStap("klaar"));
            }

            return SeeOther(DiscoverySsrUrls.Step(shedStep + 1));
        }

        if (string.Equals(action, "mini-continue", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(form["step"].ToString(), out var step) || step is < 7 or > 10)
            {
                return SeeOther(DiscoverySsrUrls.Overview());
            }

            if (step >= 10)
            {
                return SeeOther(DiscoverySsrUrls.Shed(10));
            }

            return SeeOther(DiscoverySsrUrls.Shed(step));
        }

        if (string.Equals(action, "mini-pick", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(form["step"].ToString(), out var step) || step is < 7 or > 10)
            {
                return SeeOther(DiscoverySsrUrls.Overview());
            }

            var diepteRaw = form["diepte"].ToString();
            var targetLevel = OnboardingWizardCatalog.MiniLevelCount;
            if (JourneyTestFlow.TryParseDepth(diepteRaw, out var parsedDepth))
            {
                targetLevel = parsedDepth;
            }

            var q = OnboardingSsrMiniNavigation.ReadQuestionFromRequest(http);
            var obMiniPick = form["obMiniPick"].ToString();
            if (string.IsNullOrWhiteSpace(obMiniPick))
            {
                return SeeOther(DiscoverySsrUrls.WithError(DiscoverySsrUrls.Step(step, targetLevel, q), culture["Onboarding.NotSaved"]));
            }

            form.TryGetValue("consentRequested", out var consentRequested);
            form.TryGetValue("obPayload", out var obPayload);
            var pickBody = OnboardingMiniPostBody.BuildMiniBlockBody(
                http,
                step,
                consentRequested.ToString(),
                obMiniPick,
                obPayload.ToString());
            var pickMerged = OnboardingMiniPostBody.MergeNavFlags(pickBody, advance: false, goBack: false, skipOptional: false);
            var (pickStatus, _) = await OnboardingClientStepBffSaver.SaveStepAsync(
                step,
                pickMerged,
                api,
                geocoder,
                featureFlags,
                culture,
                cancellationToken);
            if (pickStatus is >= 400)
            {
                return SeeOther(DiscoverySsrUrls.WithError(DiscoverySsrUrls.Step(step, targetLevel, q), culture["Common.Error.TryAgain"]));
            }

            var band = await DiscoverySsrMiniLoader.LoadBandAsync(step, targetLevel, api, featureFlags, culture, cancellationToken);
            var total = band.Questions.Count;
            if (total > 0 && q < total)
            {
                return SeeOther(DiscoverySsrUrls.Step(step, targetLevel, q + 1));
            }

            return SeeOther(DiscoverySsrUrls.Shed(step));
        }

        if (string.Equals(action, "overview-continue", StringComparison.OrdinalIgnoreCase))
        {
            var fase = form["fase"].ToString();
            return SeeOther(DiscoverySsrUrls.Overview(string.IsNullOrWhiteSpace(fase) ? null : fase));
        }

        return SeeOther(DiscoverySsrUrls.Overview());
    }

    private static async Task<int> CountAnsweredAsync(
        JobsyApiClient api,
        DiscoveryOverviewCache overviewCache,
        OnboardingWizardCatalog.OnboardingTestKind kind,
        CancellationToken cancellationToken)
    {
        var panel = await overviewCache.GetPanelAsync(cancellationToken);
        if (panel is not null)
        {
            return kind switch
            {
                OnboardingWizardCatalog.OnboardingTestKind.Competency => panel.CompetencyAnswered,
                OnboardingWizardCatalog.OnboardingTestKind.Career => panel.CareerAnswered,
                OnboardingWizardCatalog.OnboardingTestKind.Culture => panel.CultureAnswered,
                _ => panel.ValuesAnswered
            };
        }

        Dictionary<string, int>? answers = kind switch
        {
            OnboardingWizardCatalog.OnboardingTestKind.Competency => (await api.GetMyCompetenciesAsync(cancellationToken))?.Answers,
            OnboardingWizardCatalog.OnboardingTestKind.Career => (await api.GetMyCareerInterestsAsync(cancellationToken))?.Answers,
            OnboardingWizardCatalog.OnboardingTestKind.Culture => (await api.GetMyCultureAsync(cancellationToken))?.Answers,
            _ => (await api.GetMyValuesAsync(cancellationToken))?.Answers
        };
        return answers?.Count(a => a.Value is >= 1 and <= 5) ?? 0;
    }

    private static IResult SeeOther(string url) => Results.Redirect(url, permanent: false);
}
