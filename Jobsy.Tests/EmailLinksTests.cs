using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;

namespace Jobsy.Tests;

public class EmailLinksTests
{
    [Fact]
    public void For_requires_base_url()
    {
        Assert.Throws<ArgumentException>(() => EmailLinks.For(null!));
        Assert.Throws<ArgumentException>(() => EmailLinks.For(""));
        Assert.Throws<ArgumentException>(() => EmailLinks.For("   "));
    }

    [Fact]
    public void Dependency_A_and_C_Map_is_banenkaart()
    {
        var links = EmailLinks.For("https://lobsy.nl");
        Assert.Equal("https://lobsy.nl/banenkaart", links.Map);
    }

    [Fact]
    public void Dependency_B_employer_routes_use_werkgever_shell()
    {
        var links = EmailLinks.For("https://lobsy.nl/");
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Assert.Equal("https://lobsy.nl/werkgever/sollicitaties", links.EmployerApplications());
        Assert.Equal($"https://lobsy.nl/werkgever/vacatures/nieuw?edit={id}", links.EmployerVacancyEdit(id));
        Assert.Equal("https://lobsy.nl/werkgever/tokens", links.EmployerTokens);
        Assert.Equal("https://lobsy.nl/werkgever/overnames", links.EmployerTakeovers);
        Assert.Equal("https://lobsy.nl/werkgever/koppelingen?tab=api", links.EmployerApiSettings);
        Assert.Equal("https://lobsy.nl/werkgever/organisatie/team", links.EmployerTeam);
        Assert.Equal("https://lobsy.nl/werkgever", links.EmployerHome);
    }

    [Fact]
    public void Dependency_D_sales_onboarding_is_sales_start()
    {
        var links = EmailLinks.For("https://lobsy.nl");
        Assert.Equal("https://lobsy.nl/sales/start", links.SalesOnboarding);
    }

    [Fact]
    public void Dependency_G_admin_emails_path()
    {
        var links = EmailLinks.For("https://lobsy.nl");
        Assert.Equal("https://lobsy.nl/admin/content/emails", links.AdminEmails);
        Assert.Equal("https://lobsy.nl/admin/personal-data-access-log", links.AdminPersonalDataAccessLog);
    }

    [Fact]
    public void Candidate_and_token_links()
    {
        var links = EmailLinks.For("https://lobsy.nl");
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Assert.Equal("https://lobsy.nl/candidate/applications", links.CandidateApplications);
        Assert.Equal($"https://lobsy.nl/vacancies/{id}", links.Vacancy(id));
        Assert.Equal("https://lobsy.nl/login", links.Login);
        Assert.Equal("https://lobsy.nl/privacy/data", links.PrivacyData);
        Assert.Equal("https://lobsy.nl/candidate/actions/set-unavailable", links.SetUnavailable);
        Assert.Contains("hiredApplicationId=", links.WithdrawOthers(id));
        Assert.Equal("https://lobsy.nl/account/wachtwoord-instellen?t=abc", links.SetPassword("abc"));
        Assert.Equal("https://lobsy.nl/koppeling/sleutel?t=abc", links.ApiKeyReveal("abc"));
        Assert.Equal("https://lobsy.nl/toestemming?t=abc", links.ParentalConsent("abc"));
    }
}
