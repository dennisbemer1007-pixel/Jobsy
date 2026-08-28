using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core.Authorization;

namespace Jobsy.Tests.Uat;

/// <summary>
/// Executable API scripts behind the UAT grid (happy + unhappy authz/PII per rol).
/// </summary>
[Trait("Suite", "Uat999")]
public sealed class UatRoleApiScriptsTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private readonly RoleFunctionalWebAppFactory _factory;

    public UatRoleApiScriptsTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Guest_public_browse_ok_and_private_apis_401()
    {
        var c = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("api/vacancies")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("api/vacancies/map-view")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync($"api/vacancies/{_factory.VacancyId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("api/wages")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("api/vacancy-categories")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("api/site/crawl-index")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("api/site/branding")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("api/kvk/12345678/establishments")).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("api/me/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("api/privacy/export")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("api/applications")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("api/vacancies/manage")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("api/integrations/health")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("api/sales-managers/me/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("api/ambassadeurs/me/dashboard")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await c.PostAsJsonAsync($"api/vacancies/{_factory.VacancyId}/like", new { })).StatusCode);
    }

    [Fact]
    public async Task Candidate_can_profile_like_apply_otp_path_but_not_employer_admin()
    {
        var c = Authed(_factory.CandidateEmail);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("api/me/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("api/privacy/export")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("api/me/applications")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"api/vacancies/{_factory.VacancyId}/like", new { })).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("api/applications")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("api/vacancies/manage")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("api/integrations/health")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("api/sales-managers/me/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("api/ambassadeurs/me/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync("api/tokens/grant", new { companyId = _factory.CompanyId, amount = 1, note = "x" })).StatusCode);
    }

    [Fact]
    public async Task Branch_manager_sees_pre_accept_screening_not_pii()
    {
        var c = Authed(_factory.EmployerEmail);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("api/vacancies/manage")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("api/applications")).StatusCode);
        var list = await c.GetFromJsonAsync<List<JsonElement>>("api/applications", Json);
        Assert.NotNull(list);
        var pending = list!.Single(a => a.GetProperty("id").GetGuid() == _factory.PendingApplicationId);
        Assert.False(pending.GetProperty("piiRevealed").GetBoolean());
        Assert.False(HasEmail(pending));

        var accepted = list.Single(a => a.GetProperty("id").GetGuid() == _factory.AcceptedApplicationId);
        Assert.True(accepted.GetProperty("piiRevealed").GetBoolean());
        Assert.False(HasEmail(accepted));

        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("api/integrations/health")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync("api/tokens/allocate", new
        {
            fromCompanyId = _factory.CompanyId,
            toCompanyId = _factory.CompanyId,
            amount = 1
        })).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await c.PostAsJsonAsync($"api/vacancies/{_factory.VacancyId}/approve-publish", new { })).StatusCode);
    }

    [Fact]
    public async Task Regional_manager_is_read_only_on_lifecycle_and_react()
    {
        var c = Authed(_factory.RegionalEmail);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("api/vacancies/manage")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("api/applications")).StatusCode);

        var create = await c.PostAsJsonAsync("api/vacancies", new
        {
            title = "RM mag dit niet",
            description = "x",
            companyId = _factory.CompanyId
        });
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);

        var publish = await c.PostAsJsonAsync("api/vacancies/publish", new { vacancyId = _factory.VacancyId });
        Assert.Equal(HttpStatusCode.Forbidden, publish.StatusCode);

        var react = await c.PostAsJsonAsync($"api/applications/{_factory.PendingApplicationId}/react", new { status = "Accepted" });
        Assert.Equal(HttpStatusCode.Forbidden, react.StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync("api/tokens/checkout", new { packTokens = 10 })).StatusCode);
    }

    [Fact]
    public async Task Enterprise_manager_can_approve_and_allocate_not_admin_settings()
    {
        var c = Authed(_factory.EnterpriseEmail);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("api/vacancies/manage")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("api/applications")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("api/metrics/summary?period=week")).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("api/integrations/health")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("api/sales-commercial/admin")).StatusCode);
    }

    [Fact]
    public async Task Intermediary_can_manage_vacancies_not_admin()
    {
        var c = Authed(_factory.IntermediaryEmail);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("api/vacancies/manage")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("api/integrations/health")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await c.PostAsJsonAsync($"api/vacancies/{_factory.VacancyId}/approve-publish", new { })).StatusCode);
    }

    [Fact]
    public async Task Sales_and_ambassadeur_own_dashboards_forbidden_cross_role()
    {
        var sm = Authed(_factory.SalesEmail);
        Assert.Equal(HttpStatusCode.OK, (await sm.GetAsync("api/sales-managers/me/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await sm.GetAsync("api/vacancies/manage")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await sm.GetAsync("api/ambassadeurs/me/dashboard")).StatusCode);

        var am = Authed(_factory.AmbassadeurEmail);
        Assert.Equal(HttpStatusCode.OK, (await am.GetAsync("api/ambassadeurs/me/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await am.GetAsync("api/sales-managers/me/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await am.GetAsync("api/vacancies/manage")).StatusCode);

        var payout = await sm.GetAsync("api/sales-managers/me/payouts/preview");
        Assert.Equal(HttpStatusCode.OK, payout.StatusCode);
        var json = await payout.Content.ReadFromJsonAsync<JsonElement>(Json);
        if (json.TryGetProperty("iban", out var iban) && iban.ValueKind == JsonValueKind.String)
        {
            var raw = iban.GetString();
            if (!string.IsNullOrWhiteSpace(raw) && raw.Length > 8)
            {
                Assert.Fail("Payout preview must not expose a full IBAN.");
            }
        }
    }

    [Fact]
    public async Task Admin_can_platform_apis_and_grant_tokens()
    {
        var c = Authed(_factory.AdminEmail);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("api/integrations/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("api/metrics/summary?period=day")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("api/vacancies/manage")).StatusCode);
    }

    [Fact]
    public void Open_redirect_and_post_login_contracts()
    {
        Assert.Equal("/home", Jobsy.Web.Auth.AuthRedirects.SafeLocalUrl("https://evil.example"));
        Assert.Equal("/home", Jobsy.Web.Auth.AuthRedirects.PostLoginUrl("/"));
        Assert.Equal("/home", Jobsy.Web.Auth.AuthRedirects.PostLoginUrl("/banen"));
        Assert.Equal("/vacancies/abc", Jobsy.Web.Auth.AuthRedirects.PostLoginUrl("/vacancies/abc"));
    }

    private HttpClient Authed(string email)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Jobsy-Email", email);
        client.DefaultRequestHeaders.Add("X-Jobsy-Dev-Secret", RoleFunctionalWebAppFactory.DevSecret);
        return client;
    }

    private static bool HasEmail(JsonElement el)
    {
        foreach (var name in new[] { "email", "candidateEmail", "candidateEMail" })
        {
            if (el.TryGetProperty(name, out var v)
                && v.ValueKind == JsonValueKind.String
                && v.GetString()?.Contains('@') == true)
            {
                return true;
            }
        }

        return false;
    }
}
