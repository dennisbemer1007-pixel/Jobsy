namespace Jobsy.Tests;

public class CandidateIllustratedAppMarkupTests
{
    private static string Root => TestRepo.FindRoot();

    [Fact]
    public void Vacature_detail_has_why_title_and_reason_dna()
    {
        var detail = File.ReadAllText(Path.Combine(Root, "Jobsy.Web/Components/Pages/VacancyDetail.razor"));
        Assert.Contains("Kb.Fit.WhyTitle", detail, StringComparison.Ordinal);
        Assert.Contains("kb-dna--detail", detail, StringComparison.Ordinal);
        Assert.Contains("kb-dna__reason", detail, StringComparison.Ordinal);
        Assert.Contains("Fit.CheckVacancy", detail, StringComparison.Ordinal);
        Assert.Contains("kb-fit-panel--card", detail, StringComparison.Ordinal);
        Assert.Contains("kb-detail--warm", detail, StringComparison.Ordinal);
        Assert.Contains("KbDetailFactChips", detail, StringComparison.Ordinal);
        Assert.Contains("kb-detail__photo--illustrated", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Match_page_has_desktop_side_panel()
    {
        var match = File.ReadAllText(Path.Combine(Root, "Jobsy.Web/Components/Pages/Candidate/MatchPage.razor"));
        Assert.Contains("match-page--illustrated", match, StringComparison.Ordinal);
        Assert.Contains("MatchDesktopSide", match, StringComparison.Ordinal);
        Assert.Contains("match-page__grid", match, StringComparison.Ordinal);
    }

    [Fact]
    public void Applications_has_desktop_detail_rail()
    {
        var apps = File.ReadAllText(Path.Combine(Root, "Jobsy.Web/Components/Pages/Candidate/Applications.razor"));
        Assert.Contains("kb-apps--illustrated", apps, StringComparison.Ordinal);
        Assert.Contains("kb-apps__detail-rail", apps, StringComparison.Ordinal);
        Assert.Contains("data-candidate-applications", apps, StringComparison.Ordinal);
        Assert.Contains("data-apps-detail-panel", apps, StringComparison.Ordinal);
    }

    [Fact]
    public void Bewaard_has_shared_rail_and_illustrated_placeholder()
    {
        var liked = File.ReadAllText(Path.Combine(Root, "Jobsy.Web/Components/Pages/Candidate/Liked.razor"));
        Assert.Contains("kb-saved-rail", liked, StringComparison.Ordinal);
        Assert.Contains("kb-saved-card__media--illustrated", liked, StringComparison.Ordinal);
        Assert.Contains("SavedNoPhotoArt", liked, StringComparison.Ordinal);
    }

    [Fact]
    public void Settings_combined_layout_and_mail_panel()
    {
        var settings = File.ReadAllText(Path.Combine(Root, "Jobsy.Web/Components/Pages/Candidate/CandidateSettings.razor"));
        Assert.Contains("cand-settings", settings, StringComparison.Ordinal);
        Assert.Contains("CandidateMailSettingsPanel", settings, StringComparison.Ordinal);
        Assert.Contains("Profile.Reset.Title", settings, StringComparison.Ordinal);
        Assert.Contains("Profile.UnsubscribeTitle", settings, StringComparison.Ordinal);
    }

    [Fact]
    public void Account_signup_uses_illustrated_stage()
    {
        var signup = File.ReadAllText(Path.Combine(Root, "Jobsy.Web/Components/Pages/Public/AccountMaken.razor"));
        var code = File.ReadAllText(Path.Combine(Root, "Jobsy.Web/Components/Pages/Public/AccountMakenCode.razor"));
        Assert.Contains("au-stage--illustrated", signup, StringComparison.Ordinal);
        Assert.Contains("Signup.Eyebrow", signup, StringComparison.Ordinal);
        Assert.Contains("au-stage--illustrated", code, StringComparison.Ordinal);
    }

    [Fact]
    public void Career_page_uses_opleiding_band_not_climb_scene_css()
    {
        var career = File.ReadAllText(Path.Combine(Root, "Jobsy.Web/Components/Pages/Candidate/CareerDashboard.razor"));
        var css = File.ReadAllText(Path.Combine(Root, "Jobsy.Web/wwwroot/css/features/candidate-illustrated-app.css"));
        Assert.Contains("career-page--illustrated", career, StringComparison.Ordinal);
        Assert.Contains("CarriereBand", career, StringComparison.Ordinal);
        Assert.Contains(".career-page--illustrated .career-scene--desktop", css, StringComparison.Ordinal);
    }
}
