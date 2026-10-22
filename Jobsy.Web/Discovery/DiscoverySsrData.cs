using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Features;
using Jobsy.Web.Components.Candidate.Discovery;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Onboarding;
using Jobsy.Web.Services;

namespace Jobsy.Web.Discovery;

internal static class DiscoverySsrData
{
    internal sealed record PageLoad(
        string Screen,
        DiscoverySsrShells.OverviewModel? Overview,
        DiscoverySsrShells.ShedModel? Shed,
        DiscoverySsrShells.TestModel? Test,
        string? ErrorMessage);

    public static async Task<PageLoad> LoadAsync(
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        CultureState culture,
        DiscoveryOverviewCache overviewCache,
        DiscoverySsrDepthStore depthStore,
        string? userId,
        string? stapRaw,
        string? diepteRaw,
        string? faseRaw,
        string? qRaw,
        string? errorRaw,
        CancellationToken cancellationToken = default)
    {
        var error = string.IsNullOrWhiteSpace(errorRaw) ? null : errorRaw;
        OnboardingState? state;
        MeProfile profile;
        try
        {
            state = await api.GetMyOnboardingAsync(cancellationToken);
            profile = await api.GetMyProfileAsync(cancellationToken) ?? new MeProfile();
        }
        catch
        {
            return new PageLoad("error", null, null, null, culture["Common.Error.TryAgain"]);
        }

        var draft = OnboardingProfileDraftHydrator.FromProfile(profile);
        var screen = ResolveScreen(stapRaw, state);

        if (string.Equals(screen, "overview", StringComparison.OrdinalIgnoreCase)
            || (string.IsNullOrWhiteSpace(stapRaw) && IsJourneyComplete(state, draft)))
        {
            var overview = await BuildOverviewAsync(
                overviewCache,
                culture,
                draft,
                state,
                faseRaw,
                error,
                cancellationToken);
            return new PageLoad("overview", overview, null, null, error);
        }

        if (JourneyTestFlow.TryParseShedStep(screen, out var shedStep))
        {
            var shed = await BuildShedAsync(
                api,
                overviewCache,
                culture,
                depthStore,
                userId ?? "",
                shedStep,
                state,
                cancellationToken);
            return new PageLoad("shed", null, shed, null, error);
        }

        if (int.TryParse(screen, out var step) && step is >= 7 and <= 10)
        {
            var target = OnboardingWizardCatalog.MiniLevelCount;
            if (JourneyTestFlow.TryParseDepth(diepteRaw, out var depth))
            {
                target = depth;
            }

            var test = await BuildTestAsync(
                api,
                featureFlags,
                culture,
                step,
                target,
                qRaw,
                cancellationToken);
            return new PageLoad(step.ToString(), null, null, test, error);
        }

        if (string.Equals(screen, "klaar", StringComparison.OrdinalIgnoreCase))
        {
            return new PageLoad("klaar", null, null, null, error);
        }

        if (int.TryParse(screen, out var profileStep) && profileStep is >= 1 and <= 6)
        {
            return new PageLoad(
                profileStep.ToString(),
                null,
                null,
                null,
                culture["Discovery.Overview.Start"]);
        }

        return new PageLoad("start", null, null, null, error);
    }

    private static bool IsJourneyComplete(OnboardingState? state, OnboardingProfileDraft draft)
    {
        if (state?.Steps.Any(s => s.Step == 10 && s.CompletedAtUtc is not null) == true)
        {
            return true;
        }

        return draft.HasConsent && (state?.CurrentStep ?? 0) >= 10;
    }

    private static string ResolveScreen(string? stapRaw, OnboardingState? state)
    {
        if (!string.IsNullOrWhiteSpace(stapRaw))
        {
            return stapRaw.Trim().ToLowerInvariant();
        }

        var step = Math.Clamp(state?.CurrentStep ?? 1, 1, 10);
        var showStart = step == 1
            && (state?.Steps.Count == 0
                || state!.Steps.All(item => item.Step == 1 && item.CompletedAtUtc is null));
        return showStart ? "start" : "overview";
    }

    private static async Task<DiscoverySsrShells.OverviewModel> BuildOverviewAsync(
        DiscoveryOverviewCache overviewCache,
        CultureState culture,
        OnboardingProfileDraft draft,
        OnboardingState? state,
        string? faseRaw,
        string? error,
        CancellationToken cancellationToken)
    {
        var panel = await overviewCache.GetPanelAsync(cancellationToken);
        var activePhase = string.IsNullOrWhiteSpace(faseRaw) ? ResolveOpenPhaseId(draft, state) : faseRaw.Trim().ToLowerInvariant();
        var youPhase = activePhase;
        var stops = BuildPhaseStops(culture, draft, state, activePhase);
        var sections = BuildPanelSections(culture, draft, activePhase);
        var phaseMeta = activePhase switch
        {
            "beach" => ("Discovery.Zone.Beach", "Discovery.Overview.Phase.BeachLead", DiscoverySsrShells.Halte("strand")),
            "kust" => ("Discovery.Zone.Coast", "Discovery.Overview.Phase.CoastLead", DiscoverySsrShells.Halte("kust")),
            "rots" => ("Discovery.Zone.Rocks", "Discovery.Overview.Phase.RocksLead", DiscoverySsrShells.Halte("rotsen")),
            "diep" => ("Discovery.Zone.Deep", "Discovery.Overview.Phase.DeepLead", DiscoverySsrShells.Halte("diepte")),
            _ => ("Discovery.Zone.Light", "Discovery.Overview.Phase.LightLead", DiscoverySsrShells.Halte("licht"))
        };

        _ = panel;
        return new DiscoverySsrShells.OverviewModel(
            stops,
            activePhase,
            youPhase,
            0.45,
            phaseMeta.Item1,
            phaseMeta.Item2,
            phaseMeta.Item3,
            sections,
            error);
    }

    private static List<(string TitleKey, string Summary, int? StepHref)> BuildPanelSections(
        CultureState culture,
        OnboardingProfileDraft draft,
        string phaseId)
    {
        var list = new List<(string, string, int?)>();
        if (phaseId == "kust")
        {
            list.Add(("Discovery.Overview.Section.Start", culture["Discovery.Overview.SummaryEmpty"], 1));
            list.Add(("Discovery.Overview.Section.Passport", culture["Discovery.Overview.SummaryEmpty"], 2));
        }
        else if (phaseId == "rots")
        {
            for (var s = 3; s <= 6; s++)
            {
                list.Add(($"Discovery.Step{s}.Title", culture["Discovery.Overview.SummaryEmpty"], s));
            }
        }
        else if (phaseId == "diep")
        {
            for (var s = 7; s <= 10; s++)
            {
                list.Add((JourneyTestFlow.TestTitleKey(s), culture["Discovery.Overview.TestTodo"], s));
            }
        }
        else if (phaseId == "beach" && !draft.HasConsent)
        {
            list.Add(("Discovery.Overview.Consent", culture["Discovery.Overview.SummaryEmpty"], null));
        }

        return list;
    }

    private static List<DiscoverySsrShells.OverviewStop> BuildPhaseStops(
        CultureState culture,
        OnboardingProfileDraft draft,
        OnboardingState? state,
        string openPhaseId)
    {
        string PhaseStatus(string phaseId)
        {
            if (phaseId == "beach")
            {
                return draft.HasConsent ? culture["Discovery.Overview.Done"] : culture["Discovery.Overview.StatusOneStep"];
            }

            var remaining = RemainingStepsInPhase(phaseId, draft, state);
            if (remaining == 0)
            {
                return culture["Discovery.Overview.Done"];
            }

            if (remaining == 1)
            {
                return culture["Discovery.Rail.PhaseOneStep"];
            }

            return DiscoverySsrShells.PhaseStepsLabel(culture, remaining);
        }

        string PhaseClass(string phaseId)
        {
            if (phaseId == openPhaseId)
            {
                return "is-open";
            }

            var remaining = RemainingStepsInPhase(phaseId, draft, state);
            return remaining == 0 ? "is-done" : "is-progress";
        }

        bool done(string phaseId) => RemainingStepsInPhase(phaseId, draft, state) == 0;
        var phases = new[] { ("beach", "Discovery.Zone.Beach", "strand"), ("kust", "Discovery.Zone.Coast", "kust"), ("rots", "Discovery.Zone.Rocks", "rotsen"), ("diep", "Discovery.Zone.Deep", "diepte"), ("light", "Discovery.Zone.Light", "licht") };
        return phases.Select(p => new DiscoverySsrShells.OverviewStop(
            p.Item1,
            p.Item2,
            DiscoverySsrShells.Halte(p.Item3),
            PhaseStatus(p.Item1),
            PhaseClass(p.Item1),
            done(p.Item1),
            p.Item1 == openPhaseId && !done(p.Item1))).ToList();
    }

    private static int RemainingStepsInPhase(string phaseId, OnboardingProfileDraft draft, OnboardingState? state)
    {
        if (phaseId == "beach")
        {
            return draft.HasConsent ? 0 : 1;
        }

        if (phaseId == "light")
        {
            return 0;
        }

        var completed = state?.Steps.Where(s => s.CompletedAtUtc is not null).Select(s => s.Step).ToHashSet() ?? [];
        var steps = phaseId switch
        {
            "kust" => new[] { 1, 2 },
            "rots" => new[] { 3, 4, 5, 6 },
            "diep" => new[] { 7, 8, 9, 10 },
            _ => Array.Empty<int>()
        };
        return steps.Count(s => !completed.Contains(s));
    }

    private static string ResolveOpenPhaseId(OnboardingProfileDraft draft, OnboardingState? state)
    {
        if (!draft.HasConsent)
        {
            return "beach";
        }

        if (RemainingStepsInPhase("kust", draft, state) > 0)
        {
            return "kust";
        }

        if (RemainingStepsInPhase("rots", draft, state) > 0)
        {
            return "rots";
        }

        if (RemainingStepsInPhase("diep", draft, state) > 0)
        {
            return "diep";
        }

        return "light";
    }

    private static async Task<DiscoverySsrShells.ShedModel> BuildShedAsync(
        JobsyApiClient api,
        DiscoveryOverviewCache overviewCache,
        CultureState culture,
        DiscoverySsrDepthStore depthStore,
        string userId,
        int shedStep,
        OnboardingState? state,
        CancellationToken cancellationToken)
    {
        var kind = OnboardingWizardCatalog.TestKindForStep(shedStep)!.Value;
        var answered = await CountAnsweredAsync(api, overviewCache, kind, cancellationToken);
        var version = AssessmentTestVersions.V1;
        var options = JourneyTestFlow.BuildDepthOptions(kind, answered, version);
        var selected = depthStore.GetOrDefault(
            userId,
            shedStep,
            JourneyTestFlow.DefaultSelectedLevel(answered, kind, version));
        var fullyDone = JourneyTestFlow.IsFullyDone(answered, kind, version);
        var reached = JourneyTestFlow.ReachedLevel(answered, kind, version);
        var testName = culture[OnboardingWizardCatalog.TestNameKey(kind)];
        var lines = JourneyTestFlow.ImpressionLinesForTest(
            kind,
            state?.Impression,
            k => culture[k],
            " " + culture["Discovery.Shed.Provisional"]).ToList();
        var primary = !fullyDone && selected > reached
            ? culture["Discovery.Shed.DiveDeeper"]
            : shedStep >= 10
                ? culture["Discovery.Shed.ToLight"]
                : culture.Format("Discovery.Shed.NextTest", culture[OnboardingWizardCatalog.TestNameKey(
                    OnboardingWizardCatalog.TestKindForStep(shedStep + 1)!.Value)]);
        return new DiscoverySsrShells.ShedModel(
            shedStep,
            testName,
            lines,
            options,
            selected,
            fullyDone,
            primary,
            ShowDiveDeeper: true);
    }

    private static async Task<DiscoverySsrShells.TestModel> BuildTestAsync(
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        CultureState culture,
        int step,
        int targetLevel,
        string? qRaw,
        CancellationToken cancellationToken)
    {
        var band = await DiscoverySsrMiniLoader.LoadBandAsync(step, targetLevel, api, featureFlags, culture, cancellationToken);
        int? qParsed = int.TryParse(qRaw, out var qv) ? qv : null;
        var active = OnboardingSsrMiniNavigation.ResolveActiveQuestionIndex(band.Questions, band.Answers, qParsed);
        var seed = new OnboardingClientStepSeed
        {
            MiniQuestions = band.Questions,
            MiniAnswers = band.Answers
        };
        return new DiscoverySsrShells.TestModel(
            step,
            targetLevel,
            active + 1,
            seed,
            band.BandComplete,
            JourneyTestFlow.TestTitleKey(step),
            JourneyTestFlow.TestHintKey(step));
    }

    private static async Task<int> CountAnsweredAsync(
        JobsyApiClient api,
        DiscoveryOverviewCache? overviewCache,
        OnboardingWizardCatalog.OnboardingTestKind kind,
        CancellationToken cancellationToken)
    {
        if (overviewCache is not null)
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
}
