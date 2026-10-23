using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Discovery;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;
using Jobsy.Web.Models;
using Jobsy.Web.Onboarding;
using Jobsy.Web.Services;

namespace Jobsy.Web.Discovery;

internal static class DiscoverySsrData
{
    private static readonly int[] DiepTestSteps = [7, 8, 9, 10];
    internal sealed record PageLoad(
        string Screen,
        DiscoverySsrShells.OverviewModel? Overview,
        DiscoverySsrShells.ShedModel? Shed,
        DiscoverySsrShells.TestModel? Test,
        string? ErrorMessage);

    internal sealed record Bootstrap(
        OnboardingState? State,
        MeProfile Profile,
        OnboardingProfileDraft Draft);

    public static async Task<Bootstrap?> LoadBootstrapAsync(
        JobsyApiClient api,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var state = await api.GetMyOnboardingAsync(cancellationToken);
            var profile = await api.GetMyProfileAsync(cancellationToken) ?? new MeProfile();
            var draft = OnboardingProfileDraftHydrator.FromProfile(profile);
            return new Bootstrap(state, profile, draft);
        }
        catch
        {
            return null;
        }
    }

    public static async Task<PageLoad> LoadAsync(
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        CultureState culture,
        DiscoveryOverviewCache overviewCache,
        DiscoverySsrDepthStore depthStore,
        string? userId,
        Bootstrap bootstrap,
        string? stapRaw,
        string? diepteRaw,
        string? faseRaw,
        string? qRaw,
        string? errorRaw,
        CancellationToken cancellationToken = default)
    {
        var error = string.IsNullOrWhiteSpace(errorRaw) ? null : errorRaw;
        var state = bootstrap.State;
        var draft = bootstrap.Draft;
        var screen = DiscoverySsrJourneyState.ResolveScreen(stapRaw, state);

        if (string.Equals(screen, "overview", StringComparison.OrdinalIgnoreCase)
            || (string.IsNullOrWhiteSpace(stapRaw) && DiscoverySsrJourneyState.IsJourneyComplete(state, draft)))
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
            return new PageLoad("redirect-start", null, null, null, null);
        }

        if (string.Equals(screen, "start", StringComparison.OrdinalIgnoreCase))
        {
            return new PageLoad("redirect-start", null, null, null, null);
        }

        return new PageLoad("error", null, null, null, culture["Common.Error.TryAgain"]);
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
        var openPhase = DiscoverySsrJourneyState.ResolveOpenPhaseId(draft, state, panel);
        var activePhase = string.IsNullOrWhiteSpace(faseRaw)
            ? openPhase
            : faseRaw.Trim().ToLowerInvariant();
        var youPhase = openPhase;
        var progress = ComputeYouAreHereProgressFrac(activePhase, state, panel);
        var stops = BuildPhaseStops(culture, draft, panel, openPhase);
        var sections = BuildPanelSections(culture, draft, state, panel, activePhase);
        var tests = BuildTestRows(culture, panel);
        var phaseMeta = activePhase switch
        {
            "beach" => ("Discovery.Zone.Beach", "Discovery.Overview.Phase.BeachLead", DiscoverySsrShells.Halte("strand")),
            "kust" => ("Discovery.Zone.Coast", "Discovery.Overview.Phase.CoastLead", DiscoverySsrShells.Halte("kust")),
            "rots" => ("Discovery.Zone.Rocks", "Discovery.Overview.Phase.RocksLead", DiscoverySsrShells.Halte("rotsen")),
            "diep" => ("Discovery.Zone.Deep", "Discovery.Overview.Phase.DeepLead", DiscoverySsrShells.Halte("diepte")),
            _ => ("Discovery.Zone.Light", "Discovery.Overview.Phase.LightLead", DiscoverySsrShells.Halte("licht"))
        };

        return new DiscoverySsrShells.OverviewModel(
            stops,
            activePhase,
            youPhase,
            progress,
            phaseMeta.Item1,
            phaseMeta.Item2,
            phaseMeta.Item3,
            sections,
            tests,
            error);
    }

    private static List<DiscoverySsrShells.OverviewPanelSection> BuildPanelSections(
        CultureState culture,
        OnboardingProfileDraft draft,
        OnboardingState? state,
        CandidateDiscoveryOverviewPanelDto? panel,
        string phaseId)
    {
        var completed = DiscoverySsrJourneyState.CompletedStepNumbers(state);
        var list = new List<DiscoverySsrShells.OverviewPanelSection>();

        if (phaseId == "kust")
        {
            foreach (var step in new[] { 1, 2 })
            {
                var info = JourneyCatalog.Steps.First(s => s.Number == step);
                list.Add(new DiscoverySsrShells.OverviewPanelSection(
                    culture[info.TitleKey],
                    DiscoverySsrOverviewSummaries.StepOverviewSummary(step, culture, draft, panel),
                    DiscoverySsrUrls.Step(step),
                    completed.Contains(step)));
            }
        }
        else if (phaseId == "rots")
        {
            foreach (var step in new[] { 3, 4, 5, 6 })
            {
                var info = JourneyCatalog.Steps.First(s => s.Number == step);
                list.Add(new DiscoverySsrShells.OverviewPanelSection(
                    culture[info.TitleKey],
                    DiscoverySsrOverviewSummaries.StepOverviewSummary(step, culture, draft, panel),
                    DiscoverySsrUrls.Step(step),
                    completed.Contains(step)));
            }
        }
        else if (phaseId == "diep")
        {
            foreach (var step in new[] { 7, 8, 9, 10 })
            {
                var kind = OnboardingWizardCatalog.TestKindForStep(step)!.Value;
                list.Add(new DiscoverySsrShells.OverviewPanelSection(
                    culture[DiscoverySsrOverviewSummaries.OverviewRouteTestTitleKey(kind)],
                    DiscoverySsrOverviewSummaries.StepOverviewSummary(step, culture, draft, panel),
                    DiscoverySsrUrls.Step(step),
                    true));
            }
        }
        else if (phaseId == "beach" && !draft.HasConsent)
        {
            list.Add(new DiscoverySsrShells.OverviewPanelSection(
                culture["Discovery.Overview.Consent"],
                culture["Discovery.Overview.New"],
                null,
                true));
        }

        return list;
    }

    private static List<DiscoverySsrShells.OverviewTestRow> BuildTestRows(
        CultureState culture,
        CandidateDiscoveryOverviewPanelDto? panel)
    {
        var rows = new List<DiscoverySsrShells.OverviewTestRow>();
        foreach (var step in new[] { 7, 8, 9, 10 })
        {
            var kind = OnboardingWizardCatalog.TestKindForStep(step)!.Value;
            var answered = DiscoverySsrJourneyState.AnsweredCount(kind, panel);
            var full = OnboardingWizardCatalog.FullLevelCount(kind);
            var mini = OnboardingWizardCatalog.MiniLevelCount;
            var complete = answered >= full;
            var progressText = complete
                ? null
                : answered >= mini
                    ? culture.Format("Discovery.Overview.TestAnsweredCount", answered, full)
                    : answered > 0
                        ? culture.Format("Discovery.Overview.TestAnsweredCount", answered, full)
                        : null;
            var accent = kind switch
            {
                OnboardingWizardCatalog.OnboardingTestKind.Competency => DiscoveryOverviewIllustrationAssets.TestAccent("competency"),
                OnboardingWizardCatalog.OnboardingTestKind.Career => DiscoveryOverviewIllustrationAssets.TestAccent("career"),
                OnboardingWizardCatalog.OnboardingTestKind.Culture => DiscoveryOverviewIllustrationAssets.TestAccent("culture"),
                _ => DiscoveryOverviewIllustrationAssets.TestAccent("values")
            };
            rows.Add(new DiscoverySsrShells.OverviewTestRow(
                step,
                DiscoverySsrOverviewSummaries.OverviewRouteTestTitleKey(kind),
                accent,
                complete || answered >= mini,
                progressText,
                "/candidate/paspoort?tab=tests&from=reis",
                complete ? null : DiscoverySsrUrls.Step(step),
                answered >= mini ? DiscoverySsrUrls.Shed(step) : null));
        }

        return rows;
    }

    private static List<DiscoverySsrShells.OverviewStop> BuildPhaseStops(
        CultureState culture,
        OnboardingProfileDraft draft,
        CandidateDiscoveryOverviewPanelDto? panel,
        string openPhaseId)
    {
        string PhaseStatus(string phaseId)
        {
            if (phaseId == "beach")
            {
                return draft.HasConsent ? culture["Discovery.Overview.Done"] : culture["Discovery.Overview.StatusOneStep"];
            }

            var remaining = RemainingInPhase(phaseId);
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

            return RemainingInPhase(phaseId) == 0 ? "is-done" : "is-progress";
        }

        bool done(string phaseId) => RemainingInPhase(phaseId) == 0;

        int RemainingInPhase(string phaseId)
        {
            if (phaseId is "beach")
            {
                return draft.HasConsent ? 0 : 1;
            }

            if (phaseId is "light")
            {
                return 0;
            }

            if (phaseId is "diep" && panel is not null)
            {
                return DiepTestSteps.Count(s => !DiscoverySsrOverviewSummaries.IsBasicsStepFilled(s, culture, draft, panel));
            }

            var steps = phaseId switch
            {
                "kust" => new[] { 1, 2 },
                "rots" => new[] { 3, 4, 5, 6 },
                _ => Array.Empty<int>()
            };
            return steps.Count(s => !DiscoverySsrOverviewSummaries.IsBasicsStepFilled(s, culture, draft, panel));
        }
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

    private static double ComputeYouAreHereProgressFrac(
        string phaseId,
        OnboardingState? state,
        CandidateDiscoveryOverviewPanelDto? panel)
    {
        if (phaseId is "beach" or "light")
        {
            return 0.45;
        }

        var steps = phaseId switch
        {
            "kust" => new[] { 1, 2 },
            "rots" => new[] { 3, 4, 5, 6 },
            "diep" => new[] { 7, 8, 9, 10 },
            _ => Array.Empty<int>()
        };
        if (steps.Length == 0)
        {
            return 0.45;
        }

        var completed = DiscoverySsrJourneyState.CompletedStepNumbers(state);
        var done = steps.Count(completed.Contains);
        if (phaseId == "diep" && panel is not null)
        {
            done = DiepTestSteps.Count(s => DiscoverySsrJourneyState.IsTestMiniDone(s, panel));
        }

        if (done >= steps.Length)
        {
            return 1;
        }

        return 0.25 + 0.55 * (done / (double)steps.Length);
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
