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
        Assert.Equal(3, profile.Tests.Count);
        Assert.Contains(profile.Tests, t => t.Id == "competence");
        Assert.Contains(profile.Tests, t => t.Id == "career");
        Assert.Contains(profile.Tests, t => t.Id == "culture");
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
        Assert.All(profile.Tests, t => Assert.True(t.SupportsDeepAnalysis));
        Assert.All(profile.Tests, t => Assert.False(string.IsNullOrWhiteSpace(t.DeepAnalysisHref)));
        Assert.Contains(profile.Tests, t => t.Id == "culture" && t.Title == "Cultuurfit");
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
    public void Profiel_page_and_nav_are_wired()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/CandidateProfile.razor"));
        Assert.Contains("@page \"/profiel\"", page);
        Assert.Contains("CandidateProfileService", page);
        Assert.Contains("profile-hub__grid", page);
        Assert.Contains("ProfileHub.ActionFreeStart", page);
        Assert.Contains("ProfileHub.ActionFreeRetake", page);
        Assert.Contains("ProfileHub.ActionDeepStart", page);
        Assert.Contains("ProfileHub.ActionDeepEdit", page);
        Assert.Contains("profile-hub-dna-scores", page);
        Assert.DoesNotContain("Bekijk of herhaal", page);
        Assert.DoesNotContain("Open kompas", page);
        Assert.DoesNotContain("DISC", page);
        Assert.DoesNotContain("ProfileHub.AddTests", page);
        Assert.DoesNotContain("profile-hub-card--scores", page);

        var service = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Services/CandidateProfileService.cs"));
        Assert.Contains("Title = \"Cultuurfit\"", service);
        Assert.DoesNotContain("DISC", service);

        var nav = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Navigation/RoleNavCatalog.cs"));
        Assert.Contains("\"/profiel\"", nav);
        Assert.Contains("\"/carriere\"", nav);
        Assert.Contains("NavIcons.Career", nav);

        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".profile-hub", css);
        Assert.Contains(".profile-hub__grid", css);
        Assert.Contains(".profile-hub-tests__actions", css);
    }
}
