using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Tests;

public class CandidateProfileServiceTests
{
    [Fact]
    public void GetProfile_returns_basics_tests_scores_and_settings()
    {
        var svc = new CandidateProfileService();
        var profile = svc.GetProfile();

        Assert.False(string.IsNullOrWhiteSpace(profile.Basics.DisplayName));
        Assert.InRange(profile.ProfileCompletenessPercent, 1, 100);
        Assert.NotEmpty(profile.Tests);
        Assert.NotEmpty(profile.ScoreBars);
        Assert.Equal(4, profile.Tests.Count);
        Assert.Contains(profile.Tests, t => t.Id == "competence");
        Assert.Contains(profile.Tests, t => t.Id == "career");
        Assert.Contains(profile.Tests, t => t.Id == "culture");
        Assert.Contains(profile.Tests, t => t.Id == "values");
        Assert.DoesNotContain(profile.Tests, t => t.Id == "fit");
        Assert.Contains(profile.ScoreBars, s => s.Percent is > 0 and <= 100);
        Assert.True(profile.Settings.HideContactUntilMatch);
    }

    [Fact]
    public void Dna_test_cards_cover_not_started_free_and_deep_stages()
    {
        var profile = new CandidateProfileService().GetProfile();
        Assert.Contains(profile.Tests, t => t.Stage == CandidateDnaTestStage.NotStarted);
        Assert.Contains(profile.Tests, t => t.Stage == CandidateDnaTestStage.FreeCompleted);
        Assert.Contains(profile.Tests, t => t.Stage == CandidateDnaTestStage.DeepCompleted);
        Assert.All(profile.Tests, t => Assert.False(string.IsNullOrWhiteSpace(t.FreeTestHref)));
        Assert.Contains(profile.Tests, t => t.Id == "culture" && t.Title == "Cultuurfit" && t.SupportsDeepAnalysis);
        Assert.Contains(profile.Tests, t => t.Id == "values" && t.SupportsDeepAnalysis && t.Title == "Waarden & Drijfveren");
        Assert.DoesNotContain(profile.Tests, t => t.Title.Contains("DISC", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(profile.Tests, t => t.SupportsDeepAnalysis && t.Stage == CandidateDnaTestStage.DeepCompleted);
        Assert.Contains(profile.Tests, t => t.SupportsDeepAnalysis && t.Stage == CandidateDnaTestStage.NotStarted);
        Assert.Contains(profile.Tests, t => t.SupportsDeepAnalysis && t.Stage == CandidateDnaTestStage.FreeCompleted);
    }

    [Fact]
    public void UpdateSettings_and_open_for_work_are_persisted_in_service()
    {
        var svc = new CandidateProfileService();
        var before = svc.GetProfile();
        Assert.True(before.Basics.OpenForWork);

        var afterOpen = svc.SetOpenForWork(false);
        Assert.False(afterOpen.Basics.OpenForWork);
        Assert.False(svc.GetProfile().Basics.OpenForWork);

        var settings = afterOpen.Settings;
        settings.EmailNotifications = false;
        settings.PushNotifications = true;
        var afterSettings = svc.UpdateSettings(settings);
        Assert.False(afterSettings.Settings.EmailNotifications);
        Assert.True(afterSettings.Settings.PushNotifications);
    }

    [Fact]
    public void Profiel_page_is_gateway_to_kompas()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateProfile.razor"));
        Assert.Contains("@page \"/profiel\"", page);
        Assert.Contains("CandidateProfileService", page);
        Assert.Contains("profile-hub-kompas", page);
        Assert.Contains("/candidate/profile", page);
        Assert.Contains("ProfileHub.OpenKompas", page);
        Assert.Contains("ProfileHub.GatewayLead", page);
        Assert.Contains("profile-hub--gateway", page);
        Assert.DoesNotContain("profile-hub__grid", page);
        Assert.DoesNotContain("profile-hub-gateway__links", page);
        Assert.DoesNotContain("ProfileHub.ActionFreeStart", page);
        Assert.DoesNotContain("profile-hub-dna-scores", page);
        Assert.DoesNotContain("ProfileHub.EditBasics", page);
        Assert.DoesNotContain("DISC", page);

        var service = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Services/CandidateProfileService.cs"));
        Assert.Contains("Title = \"Cultuurfit\"", service);
        Assert.Contains("Title = \"Waarden & Drijfveren\"", service);
        Assert.Contains("Schwartz", service);
        Assert.DoesNotContain("DISC", service);

        var kompas = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateKompas.razor"));
        Assert.Contains("Kompas.TabTests", kompas);
        Assert.Contains("TestsOverviewPanel", kompas);
        Assert.Contains("DnaPanel", kompas);

        var detail = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/TestDetail.razor"));
        Assert.Contains("/profiel/tests/{TestKey}", detail);
        Assert.Contains("FreeStartHref", detail);
        Assert.Contains("StartDeepAnalysisCheckoutAsync", detail);
        Assert.Contains("/candidate/values", File.ReadAllText(Path.Combine(root, "Jobsy.Core/Rules/AssessmentTestCatalog.cs")));

        var nav = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Navigation/RoleNavCatalog.cs"));
        Assert.Contains("\"/profiel\"", nav);
        Assert.Contains("\"/carriere\"", nav);
        Assert.Contains("NavIcons.Career", nav);

        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".profile-hub", css);
        Assert.Contains(".profile-hub-kompas", css);
    }
}
