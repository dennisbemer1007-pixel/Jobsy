using Jobsy.Core.Email;

namespace Jobsy.Tests;

public class EmailLayoutTests
{
    [Fact]
    public void Wrap_includes_logo_brand_and_inner_html()
    {
        var html = EmailLayout.Wrap(
            EmailLayout.Heading("Hallo") + EmailLayout.Paragraph("Body tekst"),
            "https://lobsy.nl",
            preheader: "Voorbeeld");

        Assert.Contains("lobsy-128.png", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://lobsy.nl/images/brand/lobsy-128.png", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cid:", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/images/brand/lobsy.png", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Lobsy", html);
        Assert.Contains("Hallo", html);
        Assert.Contains("Body tekst", html);
        Assert.Contains("Voorbeeld", html);
        Assert.Contains(EmailLayout.BrandNavy, html);
    }

    [Fact]
    public void LogoUrl_points_at_the_small_png_not_the_site_mark()
    {
        var url = EmailLayout.LogoUrl("https://lobsy.nl");
        Assert.Equal("https://lobsy.nl/images/brand/lobsy-128.png?v=20260823-hosted", url);
        Assert.Contains(EmailLayout.LogoRelativePath, url);
    }

    [Fact]
    public void PrimaryButton_is_escaped_and_uses_navy_cta()
    {
        var btn = EmailLayout.PrimaryButton("https://lobsy.nl/vacancies/abc\" onclick=x", "Klik hier");
        Assert.Contains("Klik hier", btn);
        Assert.Contains(EmailLayout.BrandNavy, btn);
        Assert.Contains("&quot;", btn);
        Assert.DoesNotContain("href=\"https://lobsy.nl/vacancies/abc\" onclick", btn);
        Assert.Contains("https://lobsy.nl/vacancies/abc", btn);
    }

    [Fact]
    public void Deep_links_point_to_expected_routes()
    {
        var baseUrl = "https://lobsy.nl/";
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        Assert.Equal(
            "https://lobsy.nl/vacancies/11111111-1111-1111-1111-111111111111",
            EmailLayout.VacancyUrl(baseUrl, id));
        Assert.Equal(
            "https://lobsy.nl/candidate/applications",
            EmailLayout.CandidateApplicationsUrl(baseUrl));
        Assert.Equal(
            "https://lobsy.nl/branch/vacancies/new?edit=11111111-1111-1111-1111-111111111111",
            EmailLayout.EditVacancyUrl(baseUrl, id));
        Assert.Equal(
            "https://lobsy.nl/employer/vacancies?boost=highlight&id=11111111-1111-1111-1111-111111111111",
            EmailLayout.HighlightVacancyUrl(baseUrl, id));
        Assert.Equal(
            "https://lobsy.nl/employer/vacancies?boost=pushbom&id=11111111-1111-1111-1111-111111111111",
            EmailLayout.PushBomVacancyUrl(baseUrl, id));
        Assert.Equal("https://lobsy.nl/login", EmailLayout.LoginUrl(baseUrl));
        Assert.Equal("https://lobsy.nl/register/activate", EmailLayout.RegisterActivateUrl(baseUrl));
        Assert.Equal("https://lobsy.nl/privacy/data", EmailLayout.PrivacyDataUrl(baseUrl));
        Assert.Equal("https://lobsy.nl/employer/takeovers", EmailLayout.TakeoversUrl(baseUrl));
        Assert.Equal(
            "https://lobsy.nl/candidate/actions/set-unavailable",
            EmailLayout.SetUnavailableUrl(baseUrl));
    }

    [Fact]
    public void FactCard_and_KpiList_render_labels()
    {
        var facts = EmailLayout.FactCard([("Functie", "Bakker"), ("Afstand", "1,2 km")]);
        Assert.Contains("Functie", facts);
        Assert.Contains("Bakker", facts);
        Assert.Contains("1,2 km", facts);

        var kpis = EmailLayout.KpiList([("Views", "12"), ("Sollicitaties", "3")]);
        Assert.Contains("Views", kpis);
        Assert.Contains("12", kpis);
    }
}
