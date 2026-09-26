using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Auth;

namespace Jobsy.Tests;

public class CandidateOnboardingWizardTests
{
    [Fact]
    public void Catalog_mini_question_ids_cover_each_dimension()
    {
        Assert.Equal(5, OnboardingWizardCatalog.CompetencyQuestionIds.Length);
        Assert.Equal(6, OnboardingWizardCatalog.CareerQuestionIds.Length);
        Assert.Equal(5, OnboardingWizardCatalog.CultureQuestionIds.Length);
        Assert.Equal(5, OnboardingWizardCatalog.ValuesQuestionIds.Length);
        Assert.Equal(10, OnboardingWizardCatalog.StepCount);

        Assert.Contains(1, OnboardingWizardCatalog.CompetencyQuestionIds);
        Assert.Contains(21, OnboardingWizardCatalog.CompetencyQuestionIds);
        Assert.Contains(22, OnboardingWizardCatalog.CareerQuestionIds);
        Assert.Equal(
            [
                CulturePersonalityCatalog.Autonomy,
                CulturePersonalityCatalog.Informal,
                CulturePersonalityCatalog.Collaboration,
                CulturePersonalityCatalog.Flexibility,
                CulturePersonalityCatalog.PeopleFirst
            ],
            OnboardingWizardCatalog.CultureDimensionCodes);
    }

    [Fact]
    public void Impression_library_and_education_line_are_reviewable()
    {
        Assert.Contains("21 vragen", OnboardingImpressionLibrary.ResultLabel, StringComparison.Ordinal);
        Assert.Equal(
            "HAVO – E&M",
            OnboardingWizardCatalog.FormatEducationLine("HAVO", "E&M"));
        Assert.Equal("Geen", OnboardingWizardCatalog.FormatEducationLine("Geen", "iets"));
        Assert.Contains("Samenwerken", OnboardingImpressionLibrary.MatchWhyLine("Samenwerken", "Verkoper", 12));
    }

    [Fact]
    public void Step_analytics_marks_started_completed_skipped()
    {
        var utc = new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);
        var json = OnboardingStepAnalytics.MarkStarted("[]", 3, utc);
        json = OnboardingStepAnalytics.MarkCompleted(json, 3, utc.AddMinutes(1));
        json = OnboardingStepAnalytics.MarkSkipped(json, 4, utc.AddMinutes(2));
        var events = OnboardingStepAnalytics.Parse(json);
        Assert.Equal(2, events.Count);
        Assert.NotNull(events.First(e => e.Step == 3).CompletedAtUtc);
        Assert.NotNull(events.First(e => e.Step == 4).SkippedAtUtc);
    }

    [Fact]
    public void Provisional_scores_use_draft_answers_until_completed_wins()
    {
        var answers = new Dictionary<int, int>
        {
            [1] = 5, [6] = 4, [11] = 3, [16] = 5, [21] = 4
        };
        var json = CompetencyTestCatalog.SerializeAnswers(answers);
        var provisional = ProvisionalAssessmentScores.ResolveCompetency(
            CandidateCompetencyStatuses.Draft, json, null, null, null, null, null);
        Assert.True(provisional.IsProvisional);
        Assert.NotNull(provisional.Scores);
        Assert.True(provisional.Scores!.IsComplete);

        var completed = ProvisionalAssessmentScores.ResolveCompetency(
            CandidateCompetencyStatuses.Completed, json, 80, 70, 60, 50, 40);
        Assert.False(completed.IsProvisional);
        Assert.Equal(80, completed.Scores!.Samenwerken);
    }

    [Fact]
    public void Values_and_culture_provisional_score_from_mini_items()
    {
        var valuesAnswers = new Dictionary<int, int>
        {
            [1] = 5, [6] = 4, [11] = 3, [16] = 2, [21] = 5
        };
        var valuesJson = SchwartzValuesCatalog.SerializeAnswers(valuesAnswers);
        var values = ProvisionalAssessmentScores.ResolveValues(
            CandidateCompetencyStatuses.Draft, valuesJson, null, null, null, null, null);
        Assert.True(values.IsProvisional);
        Assert.True(values.Scores is { IsComplete: true });

        var cultureAnswers = new Dictionary<int, int>
        {
            [1] = 5, [3] = 4, [5] = 5, [7] = 3, [11] = 4
        };
        var cultureJson = CulturePersonalityCatalog.SerializeAnswers(cultureAnswers);
        var culture = ProvisionalAssessmentScores.ResolveCulture(
            CandidateCompetencyStatuses.Draft, cultureJson, null);
        Assert.True(culture.IsProvisional);
        Assert.True(culture.Scores is { IsComplete: true });
        Assert.Equal(ProvisionalAssessmentScores.NeutralFillPercent, culture.Scores!.Innovation);
    }

    [Fact]
    public void Kompas_counts_provisional_tests_as_half()
    {
        var full = KompasProfileCompleteness.Percent(true, true, true, true, true, true);
        Assert.Equal(100, full);

        var provisional = KompasProfileCompleteness.Percent(
            profileBasicsFilled: true,
            competencyCompleted: false,
            careerCompleted: false,
            cultureCompleted: false,
            valuesCompleted: false,
            hasEducationOrBackground: true,
            competencyProvisional: true,
            careerProvisional: true,
            cultureProvisional: true,
            valuesProvisional: true);
        Assert.Equal(67, provisional); // 2 full + 4*0.5 = 4 / 6 ≈ 67
    }

    [Fact]
    public void Auth_redirect_and_device_session_skip_complete_profiles()
    {
        Assert.Equal("/candidate/start", AuthRedirects.CandidateHowToPath);
        Assert.Equal("/candidate/hoe-werkt-lobsy", AuthRedirects.CandidateHowToGuidePath);
        Assert.Equal("/candidate/start", AuthRedirects.CandidatePostLoginUrl(true));

        var incomplete = new User
        {
            Role = UserRole.Candidate,
            FullName = "Test",
            PreferencesJson = """{"maxTravelMinutes":30,"preferredTransport":"Fiets"}"""
        };
        Assert.True(DeviceSessionService.ShouldShowCandidateOnboarding(incomplete));

        var complete = new User
        {
            Role = UserRole.Candidate,
            FullName = "Test",
            PreferencesJson = """{"maxTravelMinutes":30,"preferredTransport":"Fiets","educations":["MBO"]}"""
        };
        Assert.False(DeviceSessionService.ShouldShowCandidateOnboarding(complete));

        var howtoDone = new User
        {
            Role = UserRole.Candidate,
            CandidateHowToCompletedAt = DateTime.UtcNow,
            FullName = "Test"
        };
        Assert.False(DeviceSessionService.ShouldShowCandidateOnboarding(howtoDone));
    }

    [Fact]
    public void Education_chips_and_extract_level_support_havo_line()
    {
        Assert.Contains("HAVO", OnboardingWizardCatalog.EducationChips);
        Assert.Contains("MBO 4", OnboardingWizardCatalog.EducationChips);
        Assert.Equal("HAVO", EducationLevelLabels.ExtractLevel("HAVO – E&M"));
        Assert.Equal("MBO", EducationLevelLabels.ToVacancyBucket("MBO 2"));
        Assert.True(EducationLevelLabels.CandidateMeetsRequirement(["MBO 3"], "MBO"));
    }

    [Fact]
    public void Wizard_page_and_resume_card_are_wired()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var wizard = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/OnboardingWizard.razor"));
        Assert.Contains("@page \"/candidate/start\"", wizard, StringComparison.Ordinal);
        Assert.Contains("LikertScaleQuestion", wizard, StringComparison.Ordinal);
        Assert.Contains("SaveOnboardingDreamJobAsync", wizard, StringComparison.Ordinal);
        Assert.Contains("CompleteMyOnboardingAsync", wizard, StringComparison.Ordinal);
        Assert.Contains("lobsyPwaInstall", wizard, StringComparison.Ordinal);
        Assert.Contains("Onboarding.Later", wizard, StringComparison.Ordinal);

        var layout = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Layout/MainLayout.razor"));
        Assert.Contains("candidate/start", layout, StringComparison.Ordinal);

        var home = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateHomePanel.razor"));
        Assert.Contains("CandidateOnboardingResumeCard", home, StringComparison.Ordinal);

        var migration = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/Data/Migrations/20260926153316_SyncOnboardingModelSnapshot.cs"));
        Assert.Contains("CandidateOnboardings", migration, StringComparison.Ordinal);
        Assert.Contains("AvailableFromDate", migration, StringComparison.Ordinal);

        var library = File.ReadAllText(Path.Combine(root, "Jobsy.Core/Rules/OnboardingImpressionLibrary.cs"));
        Assert.Contains("OnboardingImpressionLibrary", library, StringComparison.Ordinal);
    }

    [Fact]
    public void Competency_serialize_answers_roundtrips_for_draft_merge()
    {
        var existing = new Dictionary<int, int> { [1] = 4, [2] = 3 };
        var merged = new Dictionary<int, int>(existing) { [6] = 5 };
        var json = CompetencyTestCatalog.SerializeAnswers(merged);
        var parsed = CompetencyTestCatalog.ParseAnswersJson(json);
        Assert.Equal(4, parsed[1]);
        Assert.Equal(3, parsed[2]);
        Assert.Equal(5, parsed[6]);
        Assert.DoesNotContain(11, parsed.Keys); // never asked twice / not forced
    }
}
