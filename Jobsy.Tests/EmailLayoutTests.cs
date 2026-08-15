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

        Assert.Contains("lobsy.png", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Lobsy", html);
        Assert.Contains("Hallo", html);
        Assert.Contains("Body tekst", html);
        Assert.Contains("Voorbeeld", html);
        Assert.Contains(EmailLayout.BrandNavy, html);
    }

    [Fact]
    public void PrimaryButton_is_escaped_and_uses_navy_cta()
    {
        var btn = EmailLayout.PrimaryButton("https://lobsy.nl/vacancies/abc\" onclick=x", "Klik hier");
        Assert.Contains("Klik hier", btn);
        Assert.Contains(EmailLayout.BrandNavy, btn);
        Assert.DoesNotContain("onclick=x", btn);
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
