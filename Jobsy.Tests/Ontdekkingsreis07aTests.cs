using Bunit;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Components.Candidate.Discovery;
using Jobsy.Web.Components.Pages.Candidate;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class OntdekkingsreisCatalogTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    [InlineData(4, 3)]
    [InlineData(5, 4)]
    [InlineData(6, 4)]
    [InlineData(7, 7)]
    [InlineData(8, 8)]
    [InlineData(9, 9)]
    [InlineData(10, 10)]
    public void MapV2ToV3_table(int v2, int v3)
        => Assert.Equal(v3, OnboardingWizardCatalog.MapV2ToV3(v2));

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    [InlineData(4, 5)]
    [InlineData(5, 6)]
    [InlineData(6, 6)]
    [InlineData(7, 7)]
    [InlineData(8, 8)]
    [InlineData(9, 9)]
    [InlineData(10, 10)]
    public void MapV3ToV2_table(int v3, int v2)
        => Assert.Equal(v2, OnboardingWizardCatalog.MapV3ToV2(v3));

    [Fact]
    public void V1_through_v2_to_v3()
    {
        Assert.Equal(1, OnboardingWizardCatalog.MapToV3(1, 1));
        Assert.Equal(2, OnboardingWizardCatalog.MapToV3(2, 1));
        Assert.Equal(3, OnboardingWizardCatalog.MapToV3(3, 1));
        Assert.Equal(4, OnboardingWizardCatalog.MapToV3(5, 1));
        Assert.Equal(7, OnboardingWizardCatalog.MapToV3(6, 1));
    }

    [Fact]
    public void V3_remaining_minutes_cover_twelve_minute_budget()
    {
        Assert.Equal(10, OnboardingWizardCatalog.V3StepCount);
        Assert.Equal(12, OnboardingWizardCatalog.V3TotalMinutesEstimate);
        Assert.Equal(12, OnboardingWizardCatalog.V3RemainingMinutes(1));
        Assert.Equal(1, OnboardingWizardCatalog.V3RemainingMinutes(10));
    }

    [Fact]
    public void StepEvent_carries_optional_wizard_version()
    {
        var utc = new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);
        var json = OnboardingStepAnalytics.MarkStarted("[]", 2, utc, OnboardingWizardCatalog.WizardVersionV3);
        json = OnboardingStepAnalytics.MarkCompleted(json, 2, utc.AddMinutes(1), OnboardingWizardCatalog.WizardVersionV3);
        var ev = OnboardingStepAnalytics.Parse(json).Single(e => e.Step == 2);
        Assert.Equal(OnboardingWizardCatalog.WizardVersionV3, ev.WizardVersion);
        Assert.NotNull(ev.CompletedAtUtc);
    }
}

public class OntdekkingsreisServiceMigrationTests
{
    [Fact]
    public async Task Incomplete_v2_becomes_v3_when_passport_on()
    {
        await using var db = CreateDb();
        var userId = await SeedUserAsync(db);
        db.CandidateOnboardings.Add(new CandidateOnboarding
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CurrentStep = 3,
            WizardVersion = OnboardingWizardCatalog.WizardVersionV2,
            StartedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            StepsJson = "[]"
        });
        await db.SaveChangesAsync();

        var svc = CreateService(db, passportOn: true);
        var state = await svc.GetAsync(userId);
        Assert.Equal(OnboardingWizardCatalog.WizardVersionV3, state.WizardVersion);
        Assert.Equal(2, state.CurrentStep);

        var row = await db.CandidateOnboardings.SingleAsync(o => o.UserId == userId);
        Assert.Equal(OnboardingWizardCatalog.WizardVersionV3, row.WizardVersion);
        Assert.Equal(2, row.CurrentStep);
    }

    [Fact]
    public async Task Incomplete_v3_reads_as_v2_when_passport_off()
    {
        await using var db = CreateDb();
        var userId = await SeedUserAsync(db);
        db.CandidateOnboardings.Add(new CandidateOnboarding
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CurrentStep = 3,
            WizardVersion = OnboardingWizardCatalog.WizardVersionV3,
            StartedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            StepsJson = "[]"
        });
        await db.SaveChangesAsync();

        var svc = CreateService(db, passportOn: false);
        var state = await svc.GetAsync(userId);
        Assert.Equal(OnboardingWizardCatalog.WizardVersionV2, state.WizardVersion);
        Assert.Equal(4, state.CurrentStep);
    }

    [Fact]
    public async Task Completed_rows_are_unchanged()
    {
        await using var db = CreateDb();
        var userId = await SeedUserAsync(db);
        var done = DateTime.UtcNow.AddDays(-1);
        db.CandidateOnboardings.Add(new CandidateOnboarding
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CurrentStep = 10,
            WizardVersion = OnboardingWizardCatalog.WizardVersionV2,
            CompletedAtUtc = done,
            StartedAtUtc = done.AddHours(-1),
            UpdatedAtUtc = done,
            StepsJson = "[]"
        });
        await db.SaveChangesAsync();

        var svc = CreateService(db, passportOn: true);
        var state = await svc.GetAsync(userId);
        Assert.Equal(OnboardingWizardCatalog.WizardVersionV2, state.WizardVersion);
        Assert.Equal(10, state.CurrentStep);
        Assert.True(state.IsComplete);
    }

    [Fact]
    public async Task SaveProgress_writes_step_event_with_version()
    {
        await using var db = CreateDb();
        var userId = await SeedUserAsync(db);
        var svc = CreateService(db, passportOn: true);
        await svc.GetAsync(userId);
        await svc.SaveProgressAsync(userId, new CandidateOnboardingProgressRequest(2, StepCompleted: true));
        var row = await db.CandidateOnboardings.SingleAsync(o => o.UserId == userId);
        var ev = OnboardingStepAnalytics.Parse(row.StepsJson).First(e => e.Step == 2);
        Assert.Equal(OnboardingWizardCatalog.WizardVersionV3, ev.WizardVersion);
        Assert.NotNull(ev.CompletedAtUtc);
    }

    private static CandidateOnboardingService CreateService(JobsyDbContext db, bool passportOn)
        => new(
            db,
            new FakeMatches(),
            new FakeInsightsQueue(),
            new FakeCareerPlans(),
            new FixedFlags(passportOn));

    private static async Task<Guid> SeedUserAsync(JobsyDbContext db)
    {
        var id = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = id,
            Email = $"{id:N}@test.local",
            FullName = "Test Candidate",
            Role = UserRole.Candidate,
            PreferencesJson = "{}"
        });
        await db.SaveChangesAsync();
        return id;
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class FixedFlags(bool passport) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(true, passport));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(feature == PlatformFeature.CandidatePassport ? passport : true);

        public void Invalidate() { }
    }

    private sealed class FakeMatches : ICandidateMatchSnapshotService
    {
        public Task<(IReadOnlyList<CandidateMatchedVacancyDto> Matches, string InsightsStatus)> GetAsync(
            Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<(IReadOnlyList<CandidateMatchedVacancyDto>, string)>(([], "ready"));

        public Task SaveComputedAsync(Guid userId, IReadOnlyList<CandidateMatchedVacancyDto> matches, string inputFingerprint, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<string> ComputeInputFingerprintAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult("");

        public Task<IReadOnlyList<CandidateMatchedVacancyDto>> ComputeLiveAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CandidateMatchedVacancyDto>>([]);

        public void InvalidateContextCache(Guid userId) { }

        public Task MarkInputsStaleAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeInsightsQueue : ICandidateInsightsQueue
    {
        public bool TryEnqueue(Guid userId) => true;
        public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken) => ValueTask.FromResult(Guid.Empty);
        public void MarkCompleted(Guid userId) { }
    }

    private sealed class FakeCareerPlans : ICandidateCareerPlanService
    {
        public Task<HorizonCareerPathPlanView?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<HorizonCareerPathPlanView?>(null);

        public Task SaveDreamAsync(Guid userId, string? dreamTitle, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task ClearDreamAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<string?> GetDreamTitleAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task EnqueueGenerationIfNeededAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task TryGeneratePendingAsync(Guid userId, HorizonCareerProfileSnapshot snapshot, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<HorizonCareerPathPlanView> GenerateAndSaveAsync(
            Guid userId,
            string? dreamTitle,
            HorizonCareerProfileSnapshot snapshot,
            string? catalogKey = null,
            string? dreamSource = null,
            string? planLanguage = null,
            bool force = false,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<HorizonCareerPathPlanView?> CompleteStepAsync(Guid userId, string stepKey, CancellationToken cancellationToken = default)
            => Task.FromResult<HorizonCareerPathPlanView?>(null);

        public Task<HorizonCareerPathPlanView?> UncompleteStepAsync(Guid userId, string stepKey, CancellationToken cancellationToken = default)
            => Task.FromResult<HorizonCareerPathPlanView?>(null);

        public Task<IReadOnlyList<ArchivedCareerPlanView>> ListArchivedAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ArchivedCareerPlanView>>([]);

        public Task<HorizonCareerPathPlanView?> RestoreArchivedAsync(Guid userId, Guid planId, CancellationToken cancellationToken = default)
            => Task.FromResult<HorizonCareerPathPlanView?>(null);

        public Task<CareerDreamOptionsView> GetDreamOptionsAsync(Guid userId, string? query, CancellationToken cancellationToken = default)
            => Task.FromResult(new CareerDreamOptionsView([], []));
    }
}

public class OntdekkingsreisRouteTests
{
    [Fact]
    public void DiscoveryJourney_requires_CandidatePassport_with_start_fallback()
    {
        var page = typeof(DiscoveryJourney);
        var attr = page.GetCustomAttributes(typeof(RequiresFeatureAttribute), inherit: true)
            .OfType<RequiresFeatureAttribute>()
            .Single(a => a.Feature == PlatformFeature.CandidatePassport);
        Assert.Equal("/candidate/start", attr.FallbackPath);

        var route = page.GetCustomAttributes(typeof(Microsoft.AspNetCore.Components.RouteAttribute), inherit: true)
            .OfType<Microsoft.AspNetCore.Components.RouteAttribute>()
            .Single();
        Assert.Equal("/candidate/ontdekkingsreis", route.Template);
    }

    [Fact]
    public void OnboardingWizard_redirects_to_journey_when_flag_on()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var wizard = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/OnboardingWizard.razor"));
        Assert.Contains("CandidatePassport", wizard, StringComparison.Ordinal);
        Assert.Contains("/candidate/ontdekkingsreis", wizard, StringComparison.Ordinal);
        Assert.Contains("replace: true", wizard, StringComparison.Ordinal);
    }

    [Fact]
    public void Extraction_parity_wizard_uses_shared_step_components()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var wizard = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/OnboardingWizard.razor"));
        var dir = Path.Combine(root, "Jobsy.Web/Components/Candidate/Onboarding");
        foreach (var name in new[]
                 {
                     "PersonalStep.razor", "AvailabilityStep.razor", "TransportStep.razor",
                     "WorkHistoryStep.razor", "EducationStep.razor", "DreamJobStep.razor",
                     "TestConsentStep.razor", "OnboardingMiniTest.razor", "OnboardingProfileDraft.cs"
                 })
        {
            Assert.True(File.Exists(Path.Combine(dir, name)), name);
        }

        Assert.Contains("<PersonalStep", wizard, StringComparison.Ordinal);
        Assert.Contains("<AvailabilityStep", wizard, StringComparison.Ordinal);
        Assert.Contains("<TransportStep", wizard, StringComparison.Ordinal);
        Assert.Contains("<WorkHistoryStep", wizard, StringComparison.Ordinal);
        Assert.Contains("<EducationStep", wizard, StringComparison.Ordinal);
        Assert.Contains("<DreamJobStep", wizard, StringComparison.Ordinal);
        Assert.Contains("<TestConsentStep", wizard, StringComparison.Ordinal);
        Assert.Contains("<OnboardingMiniTest", wizard, StringComparison.Ordinal);

        var personal = File.ReadAllText(Path.Combine(dir, "PersonalStep.razor"));
        Assert.Contains("ob-wizard__field-row", personal, StringComparison.Ordinal);
        Assert.Contains("ob-wizard__birth", personal, StringComparison.Ordinal);
        Assert.Contains("Onboarding.Step1.Title", personal, StringComparison.Ordinal);
    }
}

public class JourneyLobsterSceneBunitTests : BunitContext
{
    public JourneyLobsterSceneBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void JourneyLobster_plate_count_and_cracks_and_aria()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<JourneyLobster>(0);
            builder.AddAttribute(1, "PlatesShed", 3);
            builder.AddAttribute(2, "Label", "Lobsy de kreeft");
            builder.CloseComponent();
        });

        var svg = cut.Find("svg.journey-lob");
        Assert.Equal("img", svg.GetAttribute("role"));
        Assert.Equal("Lobsy de kreeft", svg.GetAttribute("aria-label"));
        Assert.NotNull(cut.Find(".journey-lob__cracks"));
        Assert.DoesNotContain("M40 70C36 46", cut.Markup, StringComparison.Ordinal);

        var cut2 = Render(builder =>
        {
            builder.OpenComponent<JourneyLobster>(0);
            builder.AddAttribute(1, "PlatesShed", 6);
            builder.AddAttribute(2, "Label", "Lobsy de kreeft");
            builder.CloseComponent();
        });
        Assert.Empty(cut2.FindAll(".journey-lob__cracks"));
    }

    [Fact]
    public void JourneyScene_depth_class_and_aria_hidden()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<JourneyScene>(0);
            builder.AddAttribute(1, "Depth", 2);
            builder.AddAttribute(2, "IsMobile", false);
            builder.CloseComponent();
        });

        var scene = cut.Find(".journey-scene");
        Assert.Contains("journey-scene--depth-2", scene.ClassList);
        Assert.Equal("true", scene.GetAttribute("aria-hidden"));
        Assert.Contains("linear-gradient", scene.GetAttribute("style") ?? "", StringComparison.Ordinal);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }
}
