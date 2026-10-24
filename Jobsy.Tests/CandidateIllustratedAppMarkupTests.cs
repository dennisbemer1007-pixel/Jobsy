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
        var shells = File.ReadAllText(Path.Combine(Root, "Jobsy.Web/Applications/ApplicationsSsrShells.cs"));
        Assert.Contains("sollicitaties-page", shells, StringComparison.Ordinal);
        Assert.Contains("sollicitaties-hero-strip", shells, StringComparison.Ordinal);
        Assert.Contains("sollicitaties-intro", shells, StringComparison.Ordinal);
        Assert.DoesNotContain("sollicitaties-detail", shells, StringComparison.Ordinal);
        Assert.Contains("sollicitaties-layout", shells, StringComparison.Ordinal);
        Assert.Contains("sollicitaties-empty-card", shells, StringComparison.Ordinal);
    }

    [Fact]
    public void Bewaard_has_shared_rail_and_illustrated_placeholder()
    {
        var liked = File.ReadAllText(Path.Combine(Root, "Jobsy.Web/Components/Pages/Candidate/CandidateLikedSsr.razor"));
        var shells = File.ReadAllText(Path.Combine(Root, "Jobsy.Web/Saved/SavedSsrShells.cs"));
        Assert.Contains("[NoBlazorRuntime]", liked, StringComparison.Ordinal);
        Assert.Contains("saved-page-ssr", liked, StringComparison.Ordinal);
        Assert.Contains("kb-saved-rail", shells, StringComparison.Ordinal);
        Assert.Contains("kb-saved-card__media--illustrated", shells, StringComparison.Ordinal);
        Assert.Contains("SavedNoPhotoArt", shells, StringComparison.Ordinal);
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
        var shells = File.ReadAllText(Path.Combine(Root, "Jobsy.Web/Career/CareerSsrShells.cs"));
        var css = File.ReadAllText(Path.Combine(Root, "Jobsy.Web/wwwroot/css/features/candidate-illustrated-app.css"));
        Assert.Contains("career-page--illustrated", shells, StringComparison.Ordinal);
        Assert.Contains("DroombaanProfessionWide", shells, StringComparison.Ordinal);
        Assert.Contains(".career-page--illustrated .career-scene--desktop", css, StringComparison.Ordinal);
    }
}
