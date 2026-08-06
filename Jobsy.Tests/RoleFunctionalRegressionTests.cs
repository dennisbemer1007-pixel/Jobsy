using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Navigation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

/// <summary>
/// Functionele regressie per rol tegen de echte API-pipeline (in-memory DB + DevelopmentAuth).
/// Dekking: Gast browse-vrijheid, Kandidaat apply/Gulden Middenweg/motivatie,
/// BranchManager applicants/react, Admin metrics/authz.
/// </summary>
public class RoleFunctionalRegressionTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public RoleFunctionalRegressionTests(RoleFunctionalWebAppFactory factory)
    {
        _factory = factory;
    }

    // ─── Gast / niet-ingelogd ───────────────────────────────────────────────

    [Fact]
    public async Task Guest_can_browse_public_vacancies_and_discover()
    {
        var client = _factory.CreateClient();

        var list = await client.GetAsync("api/vacancies");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var vacancies = await list.Content.ReadFromJsonAsync<List<JsonElement>>(JsonOpts);
        Assert.NotNull(vacancies);
        Assert.Contains(vacancies!, v => v.GetProperty("id").GetGuid() == _factory.VacancyId);

        var discover = await client.GetAsync(
            $"api/vacancies/discover?originLat=52.09&originLng=4.31&transport=Fiets&maxMinutes=60");
        Assert.Equal(HttpStatusCode.OK, discover.StatusCode);

        var detail = await client.GetAsync($"api/vacancies/{_factory.VacancyId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);

        var wages = await client.GetAsync("api/wages");
        Assert.Equal(HttpStatusCode.OK, wages.StatusCode);

        var kvk = await client.GetAsync("api/kvk/12345678/establishments");
        Assert.Equal(HttpStatusCode.OK, kvk.StatusCode);
    }

    [Fact]
    public async Task Guest_discover_filters_categories_and_suitable_for_65plus_without_leaking_internals()
    {
        var client = _factory.CreateClient();

        var categories = await client.GetFromJsonAsync<List<JsonElement>>("api/vacancy-categories", JsonOpts);
        Assert.NotNull(categories);
        Assert.True(categories!.Count >= 6);
        Assert.DoesNotContain(categories, c => c.GetProperty("slug").GetString() == "highlight");
        Assert.Contains(categories, c => c.GetProperty("slug").GetString() == "regulier");
        Assert.Contains(categories, c =>
            c.GetProperty("slug").GetString() == "65plus"
            && c.GetProperty("name").GetString() == VacancyCategoryDefaults.SuitableFor65PlusLabel);
        Assert.Contains(categories, c =>
            c.GetProperty("slug").GetString() == "uitzendbureau"
            && c.GetProperty("name").GetString() == VacancyCategoryDefaults.UitzendbureauLabel);

        // Legacy query param still works.
        var filter65 = await client.GetAsync("api/vacancies/discover?suitableFor65Plus=true&transport=Fiets&maxMinutes=90");
        Assert.Equal(HttpStatusCode.OK, filter65.StatusCode);
        var items65 = await filter65.Content.ReadFromJsonAsync<List<JsonElement>>(JsonOpts);
        Assert.NotNull(items65);
        Assert.Contains(items65!, v => v.GetProperty("id").GetGuid() == _factory.VacancyId);
        Assert.Contains(items65!, v => v.GetProperty("id").GetGuid() == _factory.NightShiftVacancyId);
        Assert.DoesNotContain(items65!, v => v.GetProperty("id").GetGuid() == _factory.LowMatchVacancyId);

        // Unified vacancy-type filter: 65+ category also returns flagged Regulier vacancies.
        var by65Category = await client.GetAsync(
            $"api/vacancies/discover?categoryId={VacancyCategoryDefaults.SeniorLightId:D}&transport=Fiets&maxMinutes=90");
        Assert.Equal(HttpStatusCode.OK, by65Category.StatusCode);
        var items65Cat = await by65Category.Content.ReadFromJsonAsync<List<JsonElement>>(JsonOpts);
        Assert.Contains(items65Cat!, v => v.GetProperty("id").GetGuid() == _factory.VacancyId);
        Assert.Contains(items65Cat!, v => v.GetProperty("id").GetGuid() == _factory.NightShiftVacancyId);
        Assert.DoesNotContain(items65Cat!, v => v.GetProperty("id").GetGuid() == _factory.LowMatchVacancyId);

        var byCategory = await client.GetAsync(
            $"api/vacancies/discover?categoryId={VacancyCategoryDefaults.RegulierId:D}&transport=Fiets&maxMinutes=90");
        Assert.Equal(HttpStatusCode.OK, byCategory.StatusCode);
        var regulier = await byCategory.Content.ReadFromJsonAsync<List<JsonElement>>(JsonOpts);
        Assert.Contains(regulier!, v => v.GetProperty("id").GetGuid() == _factory.VacancyId);
        Assert.All(regulier!, v =>
        {
            Assert.False(v.TryGetProperty("categoryFields", out var fields) && fields.ValueKind is JsonValueKind.Object);
            Assert.True(!v.TryGetProperty("categoryPublishCostTokens", out var cost)
                        || cost.ValueKind is JsonValueKind.Null
                        || cost.ValueKind is JsonValueKind.Undefined);
        });

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("api/vacancy-categories/admin")).StatusCode);
    }

    [Fact]
    public async Task Guest_is_blocked_from_candidate_employer_and_admin_apis()
    {
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("api/me/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("api/me/applications")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("api/applications")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("api/vacancies/manage")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("api/metrics/summary?period=day")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("api/metrics/vacancy-performance?period=day")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("api/integrations/health")).StatusCode);

        var apply = await client.PostAsJsonAsync("api/applications", new
        {
            vacancyId = _factory.VacancyId,
            preferredTransport = "Fiets",
            estimatedTravelMinutes = 15,
            acceptedTerms = true,
            workPermitConfirmed = true
        });
        Assert.Equal(HttpStatusCode.Unauthorized, apply.StatusCode);
    }

    [Fact]
    public void Guest_bottom_nav_is_anonymous_catalog()
    {
        // Product: RoleNavCatalog.Anonymous is empty — guest uses public routes (/ login/register)
        // outside the authenticated bottom-nav shell.
        var items = RoleNavCatalog.ForUser(new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity()));
        Assert.Empty(items);
    }

    // ─── Kandidaat ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Employer_with_outdated_consent_must_reaccept_current_version()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var employer = await db.Users.FirstAsync(u => u.Id == _factory.EmployerId);
            employer.ConsentVersion = "2026-07-29";
            employer.TermsAcceptedAt = DateTime.UtcNow.AddDays(-30);
            await db.SaveChangesAsync();
        }

        var client = EmployerClient();
        var before = await client.GetFromJsonAsync<JsonElement>("api/me/profile", JsonOpts);
        Assert.True(before.GetProperty("needsConsentReaccept").GetBoolean());
        Assert.Equal("2026-07-29", before.GetProperty("consentVersion").GetString());
        Assert.Equal(PrivacyConstants.CurrentConsentVersion, before.GetProperty("currentConsentVersion").GetString());

        var accept = await client.PostAsync("api/me/accept-consent", null);
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        var after = await accept.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.False(after.GetProperty("needsConsentReaccept").GetBoolean());
        Assert.Equal(PrivacyConstants.CurrentConsentVersion, after.GetProperty("consentVersion").GetString());
    }

    [Fact]
    public async Task Candidate_profile_does_not_require_account_consent_reaccept()
    {
        var client = CandidateClient();
        var me = await client.GetFromJsonAsync<JsonElement>("api/me/profile", JsonOpts);
        Assert.False(me.GetProperty("needsConsentReaccept").GetBoolean());
    }

    [Fact]
    public async Task Candidate_can_load_profile_and_update_hours_schedule()
    {
        var client = CandidateClient();

        var profile = await client.GetAsync("api/me/profile");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        var me = await profile.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("Candidate", me.GetProperty("role").GetString());
        Assert.True(me.GetProperty("hasDateOfBirth").GetBoolean());

        var update = await client.PutAsJsonAsync("api/me/profile", new
        {
            openForWork = true,
            dateOfBirth = "2000-05-01",
            preferences = new
            {
                roles = new[] { "Winkel" },
                maxTravelMinutes = 40,
                preferredTransport = "Fiets",
                minHoursPerWeek = 12m,
                maxHoursPerWeek = 24m,
                flexibleTimes = false,
                availability = new Dictionary<string, string[]>
                {
                    ["Ma"] = ["Ochtend", "Middag"],
                    ["Di"] = ["Ochtend"]
                },
                aboutMe = "Ik wil graag lokaal werken.",
                employers = new[] { new { employerName = "AH", role = "Vulploeg", years = 1 } },
                educations = Array.Empty<string>(),
                drivingLicenses = Array.Empty<string>()
            },
            homeLatitude = 52.09,
            homeLongitude = 4.31
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var apps = await client.GetAsync("api/me/applications");
        Assert.Equal(HttpStatusCode.OK, apps.StatusCode);

        var likes = await client.GetAsync("api/me/likes");
        Assert.Equal(HttpStatusCode.OK, likes.StatusCode);
    }

    [Fact]
    public async Task Candidate_apply_gulden_middenweg_then_safety_net_then_otp()
    {
        var client = CandidateClient();

        // Force low match: huge travel minutes vs preference, mismatched hours/dayparts.
        var low = await client.PostAsJsonAsync("api/applications", new
        {
            vacancyId = _factory.LowMatchVacancyId,
            preferredTransport = "Fiets",
            estimatedTravelMinutes = 90,
            acceptedTerms = true,
            workPermitConfirmed = true,
            motivation = "Ik wil dit toch graag proberen ondanks de afstand.",
            confirmLowMatchSafetyNet = false
        });
        Assert.Equal(HttpStatusCode.OK, low.StatusCode);
        var lowBody = await low.Content.ReadFromJsonAsync<ApplyResultDto>(JsonOpts);
        Assert.NotNull(lowBody);
        Assert.True(lowBody!.RequiresSafetyNetConfirmation);
        Assert.True(lowBody.MatchPercent is < MatchScoreWeights.GuldenMiddenwegThreshold);
        Assert.False(lowBody.RequiresVerification);
        Assert.False(string.IsNullOrWhiteSpace(lowBody.SafetyNetMessage));

        // Confirm safety net → OTP flow starts.
        var confirmed = await client.PostAsJsonAsync("api/applications", new
        {
            vacancyId = _factory.LowMatchVacancyId,
            preferredTransport = "Fiets",
            estimatedTravelMinutes = 90,
            acceptedTerms = true,
            workPermitConfirmed = true,
            motivation = "Ik wil dit toch graag proberen ondanks de afstand.",
            confirmLowMatchSafetyNet = true
        });
        Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
        var confirmedBody = await confirmed.Content.ReadFromJsonAsync<ApplyResultDto>(JsonOpts);
        Assert.NotNull(confirmedBody);
        Assert.True(confirmedBody!.RequiresVerification);
        Assert.True(confirmedBody.VerificationCodeSent);
        Assert.True(confirmedBody.MatchPercent is int);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var app = await db.Applications.SingleAsync(a =>
            a.VacancyId == _factory.LowMatchVacancyId
            && a.CandidateUserId == _factory.CandidateId);
        Assert.True(app.ViaSafetyNet);
        Assert.Equal("Ik wil dit toch graag proberen ondanks de afstand.", app.Motivation);
        Assert.NotNull(app.MatchPercent);
        Assert.False(string.IsNullOrWhiteSpace(app.EmailVerificationCode));
        Assert.Null(app.EmailVerifiedAt);
    }

    [Fact]
    public async Task Candidate_legal_block_rejects_apply_for_youth_night_shift()
    {
        var client = CandidateClient();

        // Make candidate 15 years old for this check.
        var dob = await client.PutAsJsonAsync("api/me/date-of-birth", new { dateOfBirth = "2011-01-15" });
        Assert.Equal(HttpStatusCode.OK, dob.StatusCode);

        var apply = await client.PostAsJsonAsync("api/applications", new
        {
            vacancyId = _factory.NightShiftVacancyId,
            preferredTransport = "Fiets",
            estimatedTravelMinutes = 10,
            acceptedTerms = true,
            workPermitConfirmed = true,
            confirmLowMatchSafetyNet = true
        });
        Assert.Equal(HttpStatusCode.BadRequest, apply.StatusCode);
        var body = await apply.Content.ReadAsStringAsync();
        Assert.Contains("wettelijke", body, StringComparison.OrdinalIgnoreCase);

        // Restore adult DOB for other tests sharing the factory user.
        var restore = await client.PutAsJsonAsync("api/me/date-of-birth", new { dateOfBirth = "1998-06-15" });
        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
    }

    [Fact]
    public async Task Candidate_can_browse_categories_and_65plus_filter()
    {
        var client = CandidateClient();
        var categories = await client.GetFromJsonAsync<List<JsonElement>>("api/vacancy-categories", JsonOpts);
        Assert.NotNull(categories);
        Assert.Contains(categories!, c =>
            c.GetProperty("slug").GetString() == "65plus"
            && c.GetProperty("name").GetString() == VacancyCategoryDefaults.SuitableFor65PlusLabel);

        var discover = await client.GetAsync(
            $"api/vacancies/discover?categoryId={VacancyCategoryDefaults.SeniorLightId:D}&transport=Fiets&maxMinutes=90");
        Assert.Equal(HttpStatusCode.OK, discover.StatusCode);
        var items = await discover.Content.ReadFromJsonAsync<List<JsonElement>>(JsonOpts);
        Assert.Contains(items!, v => v.GetProperty("suitableFor65Plus").GetBoolean()
            || v.GetProperty("categoryId").GetGuid() == VacancyCategoryDefaults.SeniorLightId);
    }

    [Fact]
    public async Task Candidate_cannot_access_employer_or_admin_endpoints()
    {
        var client = CandidateClient();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/applications")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/vacancies/manage")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/metrics/summary?period=day")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/metrics/vacancy-performance?period=day")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/integrations/health")).StatusCode);
    }

    [Fact]
    public void Candidate_nav_contains_applications_and_profile()
    {
        var identity = new System.Security.Claims.ClaimsIdentity(
            [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, JobsyRoles.Candidate)],
            "test");
        var items = RoleNavCatalog.ForUser(new System.Security.Claims.ClaimsPrincipal(identity));
        Assert.Contains(items, i => i.Href.Contains("applications", StringComparison.OrdinalIgnoreCase)
                                    || i.Href.Contains("candidate", StringComparison.OrdinalIgnoreCase));
    }

    // ─── Werkgever (BranchManager) ──────────────────────────────────────────

    [Fact]
    public async Task Employer_can_manage_vacancies_and_see_match_sorted_applicants()
    {
        var client = EmployerClient();

        var manage = await client.GetAsync("api/vacancies/manage");
        Assert.Equal(HttpStatusCode.OK, manage.StatusCode);

        var apps = await client.GetAsync("api/applications");
        Assert.Equal(HttpStatusCode.OK, apps.StatusCode);
        var list = await apps.Content.ReadFromJsonAsync<List<EmployerApplicationDto>>(JsonOpts);
        Assert.NotNull(list);

        var pending = Assert.Single(list!, a => a.Id == _factory.PendingApplicationId);
        Assert.Equal(_factory.VacancyId, pending.VacancyId);
        Assert.NotNull(pending.MatchPercent);
        Assert.False(pending.PiiRevealed);
        Assert.False(pending.CvPdfAvailable);
        Assert.Null(pending.CandidateName);
        Assert.Null(pending.CandidateEmail);
        Assert.Null(pending.CandidateCity);
        Assert.Equal(4.2, pending.DistanceKm);
        Assert.Equal(19, pending.CandidateAgeYears);
        Assert.False(string.IsNullOrWhiteSpace(pending.AvailabilitySummary));
        Assert.False(pending.WorkPermitConfirmed); // gated until accept
        Assert.Equal("Sterke motivatie voor deze rol.", pending.Motivation);
        Assert.Null(pending.StudentNumber);
        Assert.Null(pending.SchoolEmail);
        Assert.Null(pending.StudyProgram);

        var accepted = Assert.Single(list!, a => a.Id == _factory.AcceptedApplicationId);
        Assert.True(accepted.PiiRevealed);
        Assert.True(accepted.CvPdfAvailable);
        Assert.Equal("Kandidaat Test", accepted.CandidateName);
        Assert.NotNull(accepted.CandidateEmail);
        Assert.NotNull(accepted.CandidateCity);
        Assert.True(accepted.WorkPermitConfirmed);
        Assert.True(accepted.MatchPercent >= 50);
    }

    [Fact]
    public async Task Employer_managed_vacancies_expose_category_pricing_and_admin_categories_are_forbidden()
    {
        var client = EmployerClient();
        var managed = await client.GetFromJsonAsync<List<JsonElement>>("api/vacancies/manage", JsonOpts);
        Assert.NotNull(managed);
        var first = Assert.Single(managed!, v => v.GetProperty("id").GetGuid() == _factory.VacancyId);
        Assert.Equal(VacancyCategoryDefaults.RegulierId, first.GetProperty("categoryId").GetGuid());
        Assert.True(first.GetProperty("suitableFor65Plus").GetBoolean());
        Assert.True(first.TryGetProperty("categoryPublishCostTokens", out var cost)
                    && cost.ValueKind == JsonValueKind.Number);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.GetAsync("api/vacancy-categories/admin")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PostAsJsonAsync("api/vacancy-categories", new
            {
                name = "Hack",
                colorHex = "#112233",
                publishCostTokens = 0
            })).StatusCode);
    }

    [Fact]
    public async Task Employer_cannot_fulfill_pending_application_before_accept()
    {
        var client = EmployerClient();
        var response = await client.PostAsJsonAsync(
            $"api/applications/vacancies/{_factory.VacancyId}/fulfill/{_factory.PendingApplicationId}",
            new { rejectOtherApplications = true, closeVacancy = true });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var apps = await client.GetFromJsonAsync<List<EmployerApplicationDto>>("api/applications", JsonOpts);
        var pending = Assert.Single(apps!, a => a.Id == _factory.PendingApplicationId);
        Assert.Equal("Pending", pending.Status);
        Assert.False(pending.PiiRevealed);
        Assert.Null(pending.CandidateName);
    }

    [Fact]
    public async Task Employer_cannot_access_admin_integrations()
    {
        var client = EmployerClient();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/integrations/health")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/admin/users")).StatusCode);
    }

    [Fact]
    public async Task Employer_can_load_vacancy_performance_board()
    {
        var client = EmployerClient();
        var response = await client.GetAsync("api/metrics/vacancy-performance?period=week&take=3");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("top", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("flop", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Employer_role_capabilities_match_policy_matrix()
    {
        Assert.True(JobsyRoles.CanManageVacancyLifecycle(UserRole.BranchManager));
        Assert.True(JobsyRoles.CanReactToApplications(UserRole.BranchManager));
        Assert.False(JobsyRoles.CanAllocateTokens(UserRole.BranchManager));
        Assert.False(JobsyRoles.CanManageVacancyLifecycle(UserRole.RegionalManager));
        Assert.False(JobsyRoles.CanReactToApplications(UserRole.RegionalManager));
        Assert.True(JobsyRoles.CanAllocateTokens(UserRole.EnterpriseManager));
    }

    // ─── Admin ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Admin_can_manage_vacancy_categories()
    {
        var client = AdminClient();
        var list = await client.GetFromJsonAsync<List<JsonElement>>("api/vacancy-categories/admin", JsonOpts);
        Assert.NotNull(list);
        Assert.True(list!.Count >= 7);

        var catalog = await client.GetFromJsonAsync<List<JsonElement>>("api/vacancy-categories/field-catalog", JsonOpts);
        Assert.NotNull(catalog);
        Assert.Contains(catalog!, f => f.GetProperty("key").GetString() == "contractType");
    }

    [Fact]
    public async Task Admin_can_load_metrics_and_integrations()
    {
        var client = AdminClient();

        var metrics = await client.GetAsync("api/metrics/summary?period=day");
        Assert.Equal(HttpStatusCode.OK, metrics.StatusCode);

        var vacancyPerf = await client.GetAsync("api/metrics/vacancy-performance?period=day&take=3");
        Assert.Equal(HttpStatusCode.OK, vacancyPerf.StatusCode);

        var health = await client.GetAsync("api/integrations/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);

        // Admin may use both /api/admin/* and employer-scoped manage/applications (platform-wide).
        var manage = await client.GetAsync("api/vacancies/manage");
        Assert.Equal(HttpStatusCode.OK, manage.StatusCode);

        var adminVacancies = await client.GetAsync("api/admin/vacancies");
        Assert.Equal(HttpStatusCode.OK, adminVacancies.StatusCode);

        var adminUsers = await client.GetAsync("api/admin/users");
        Assert.Equal(HttpStatusCode.OK, adminUsers.StatusCode);

        var apps = await client.GetAsync("api/applications");
        Assert.Equal(HttpStatusCode.OK, apps.StatusCode);
    }

    [Fact]
    public async Task Admin_cannot_use_candidate_profile_endpoints()
    {
        var client = AdminClient();
        // Admin is authenticated but not a Candidate — RequireCandidate → Forbidden.
        var profileUpdate = await client.PutAsJsonAsync("api/me/profile", new
        {
            openForWork = false,
            preferences = new { roles = Array.Empty<string>() }
        });
        Assert.Equal(HttpStatusCode.Forbidden, profileUpdate.StatusCode);
    }

    // ─── Cross-cutting matching rules (no HTTP) ─────────────────────────────

    [Fact]
    public void Matching_rules_gulden_middenweg_and_legal_tooltips_are_wired()
    {
        Assert.Equal(5, LegalTaskFlags.Catalog.Count);
        Assert.All(LegalTaskFlags.Catalog, t =>
        {
            Assert.False(string.IsNullOrWhiteSpace(t.Label));
            Assert.True(t.Tooltip.Length > 40);
        });

        var flexible = SchedulePayload.Flexible(FlexibleScheduleSource.ImportEmpty);
        Assert.Null(flexible.Validate());
        Assert.True(flexible.FlexibleTimes);

        var hours = new HoursRange(8, 16);
        Assert.Equal(HoursCategory.PartTimeSmall, hours.Category);
    }

    // ─── helpers ────────────────────────────────────────────────────────────

    private HttpClient CandidateClient() => Authed(_factory.CandidateEmail);
    private HttpClient EmployerClient() => Authed(_factory.EmployerEmail);
    private HttpClient AdminClient() => Authed(_factory.AdminEmail);

    private HttpClient Authed(string email)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Jobsy-Email", email);
        client.DefaultRequestHeaders.Add("X-Jobsy-Dev-Secret", RoleFunctionalWebAppFactory.DevSecret);
        return client;
    }
}

public sealed class RoleFunctionalWebAppFactory : WebApplicationFactory<Program>
{
    public const string DevSecret = "role-functional-secret";

    public Guid CompanyId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000001");
    public Guid VacancyId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000010");
    public Guid LowMatchVacancyId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000011");
    public Guid NightShiftVacancyId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000012");
    public Guid CandidateId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000020");
    public Guid EmployerId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000021");
    public Guid AdminId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000022");
    public Guid PendingApplicationId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000030");
    public Guid AcceptedApplicationId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000031");
    public Guid SalaryTableId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000040");

    public string CandidateEmail => "kandidaat@jobsy.local";
    public string EmployerEmail => "branch@jobsy.local";
    public string AdminEmail => "admin@jobsy.local";

    private readonly string _dbName = "RoleFunctional-" + Guid.NewGuid();
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("JobsyAuth:AllowDevelopmentAuth", "true");
        builder.UseSetting("JobsyAuth:DevelopmentAuthSecret", DevSecret);
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("Swagger:Enabled", "false");
        builder.UseSetting(
            "ConnectionStrings:JobsyDb",
            "Host=127.0.0.1;Port=5432;Database=JobsyTest;Username=postgres;Password=postgres");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();

            var efDescriptors = services
                .Where(d =>
                    d.ServiceType == typeof(JobsyDbContext)
                    || d.ServiceType == typeof(DbContextOptions<JobsyDbContext>)
                    || (d.ServiceType.IsGenericType
                        && d.ServiceType.GetGenericTypeDefinition().Name.Contains("DbContext", StringComparison.Ordinal))
                    || (d.ImplementationType?.FullName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
                    || (d.ServiceType.FullName?.Contains("EntityFrameworkCore", StringComparison.Ordinal) == true
                        && d.ServiceType.FullName.Contains("JobsyDbContext", StringComparison.Ordinal)))
                .ToList();
            foreach (var d in efDescriptors)
            {
                services.Remove(d);
            }

            foreach (var d in services.Where(d =>
                         d.ServiceType.IsGenericType
                         && d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)
                         && d.ServiceType.GenericTypeArguments[0] == typeof(JobsyDbContext)).ToList())
            {
                services.Remove(d);
            }

            services.AddDbContext<JobsyDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            services.RemoveAll<IVacancyContentModerationService>();
            services.AddSingleton<IVacancyContentModerationService>(new AllowAllModeration());
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        EnsureSeeded();
        base.ConfigureClient(client);
    }

    private void EnsureSeeded()
    {
        if (_seeded)
        {
            return;
        }

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        if (db.Users.Any())
        {
            _seeded = true;
            return;
        }

        var categories = new VacancyCategoryService(db);
        categories.EnsureDefaultsAsync().GetAwaiter().GetResult();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Companies.Add(new Company
        {
            Id = CompanyId,
            Name = "Test Vestiging Westland",
            KvkNumber = "12345678",
            Address = "Veilingweg 1, Naaldwijk",
            Location = new GeoPoint(52.0, 4.2)
        });

        var table = new CompanySalaryTable
        {
            Id = SalaryTableId,
            CompanyId = CompanyId,
            Name = "WML",
            IsActive = true,
            IsSystemWml = true
        };
        table.Rates.Add(new CompanySalaryRate
        {
            Id = Guid.NewGuid(),
            SalaryTableId = SalaryTableId,
            AgeYears = 21,
            HourlyRate = 14.50m,
            Label = "21+"
        });
        db.CompanySalaryTables.Add(table);

        db.Users.AddRange(
            new User
            {
                Id = CandidateId,
                Email = CandidateEmail,
                FullName = "Kandidaat Test",
                Role = UserRole.Candidate,
                IsActive = true,
                DateOfBirth = new DateOnly(1998, 6, 15),
                OpenForWork = true,
                HomeLocation = new GeoPoint(52.09, 4.31),
                PreferencesJson = JsonSerializer.Serialize(new
                {
                    roles = new[] { "Winkel" },
                    maxTravelMinutes = 30,
                    preferredTransport = "Fiets",
                    minHoursPerWeek = 12,
                    maxHoursPerWeek = 24,
                    flexibleTimes = false,
                    availability = new Dictionary<string, string[]>
                    {
                        ["Ma"] = new[] { "Ochtend", "Middag" },
                        ["Di"] = new[] { "Ochtend" },
                        ["Wo"] = new[] { "Middag" }
                    },
                    aboutMe = "Ervaren vulploegmedewerker.",
                    employers = new[] { new { employerName = "Jumbo", role = "Vulploeg", years = 2 } },
                    educations = Array.Empty<string>(),
                    drivingLicenses = Array.Empty<string>()
                })
            },
            new User
            {
                Id = EmployerId,
                Email = EmployerEmail,
                FullName = "Branch Manager",
                Role = UserRole.BranchManager,
                IsActive = true,
                CompanyId = CompanyId
            },
            new User
            {
                Id = AdminId,
                Email = AdminEmail,
                FullName = "Platform Admin",
                Role = UserRole.Admin,
                IsActive = true
            });

        var scheduleJson = JsonSerializer.Serialize(new SchedulePayload
        {
            FlexibleTimes = false,
            Slots = new Dictionary<string, List<string>>
            {
                ["Ma"] = ["Ochtend", "Middag"],
                ["Di"] = ["Ochtend", "Middag"],
                ["Wo"] = ["Middag"]
            }
        }.Normalize());

        db.Vacancies.AddRange(
            new Vacancy
            {
                Id = VacancyId,
                Title = "Vulploegmedewerker Naaldwijk",
                Description = "Lokale vacature voor functionele regressie.",
                HourlyWage = 14.50m,
                StartDate = today.AddDays(-1),
                EndDate = today.AddMonths(2),
                Status = VacancyStatus.Active,
                CompanyId = CompanyId,
                Location = new GeoPoint(52.0, 4.2),
                RequiredTransport = TransportMode.Bike,
                WorkTypes = WorkType.Winkel,
                WorkTypeLabels = "Winkel",
                SalaryTableId = SalaryTableId,
                PublishedAtUtc = DateTime.UtcNow.AddDays(-1),
                MinHoursPerWeek = 12,
                MaxHoursPerWeek = 24,
                ScheduleJson = scheduleJson,
                FlexibleTimes = false,
                LegalWorksAfter19 = false,
                LegalNightShift23To06 = false,
                LegalAdultSupervisorPresent = true,
                LegalHandlesMoneyOrClosing = false,
                LegalHeavyOrHazardousWork = false,
                MaxApplications = 10,
                CategoryId = VacancyCategoryDefaults.RegulierId,
                SuitableFor65Plus = true
            },
            new Vacancy
            {
                Id = LowMatchVacancyId,
                Title = "Nachtdienst Fulltime Logistiek",
                Description = "Lage match voor Gulden Middenweg test.",
                HourlyWage = 16.00m,
                StartDate = today.AddDays(-1),
                EndDate = today.AddMonths(2),
                Status = VacancyStatus.Active,
                CompanyId = CompanyId,
                Location = new GeoPoint(51.5, 5.5),
                RequiredTransport = TransportMode.Car,
                WorkTypes = WorkType.Logistiek,
                WorkTypeLabels = "Logistiek",
                SalaryTableId = SalaryTableId,
                PublishedAtUtc = DateTime.UtcNow.AddDays(-1),
                MinHoursPerWeek = 36,
                MaxHoursPerWeek = 40,
                ScheduleJson = JsonSerializer.Serialize(new SchedulePayload
                {
                    FlexibleTimes = false,
                    Slots = new Dictionary<string, List<string>>
                    {
                        ["Ma"] = ["Nacht"],
                        ["Di"] = ["Nacht"],
                        ["Wo"] = ["Nacht"],
                        ["Do"] = ["Nacht"],
                        ["Vr"] = ["Nacht"]
                    }
                }.Normalize()),
                FlexibleTimes = false,
                LegalWorksAfter19 = true,
                LegalNightShift23To06 = false,
                LegalAdultSupervisorPresent = true,
                LegalHandlesMoneyOrClosing = false,
                LegalHeavyOrHazardousWork = false,
                MaxApplications = 10,
                CategoryId = VacancyCategoryDefaults.InternshipId
            },
            new Vacancy
            {
                Id = NightShiftVacancyId,
                Title = "Nachtdienst 23-06",
                Description = "Wettelijke uitsluiting 15-17.",
                HourlyWage = 15.00m,
                StartDate = today.AddDays(-1),
                EndDate = today.AddMonths(1),
                Status = VacancyStatus.Active,
                CompanyId = CompanyId,
                Location = new GeoPoint(52.09, 4.31),
                RequiredTransport = TransportMode.Bike,
                WorkTypes = WorkType.Winkel,
                WorkTypeLabels = "Winkel",
                SalaryTableId = SalaryTableId,
                PublishedAtUtc = DateTime.UtcNow.AddDays(-1),
                MinHoursPerWeek = 8,
                MaxHoursPerWeek = 16,
                FlexibleTimes = true,
                FlexibleScheduleSource = nameof(FlexibleScheduleSource.Manual),
                LegalWorksAfter19 = true,
                LegalNightShift23To06 = true,
                LegalAdultSupervisorPresent = true,
                LegalHandlesMoneyOrClosing = false,
                LegalHeavyOrHazardousWork = false,
                MaxApplications = 10,
                CategoryId = VacancyCategoryDefaults.SeniorLightId
            });

        db.Applications.AddRange(
            new Application
            {
                Id = PendingApplicationId,
                VacancyId = VacancyId,
                CandidateUserId = CandidateId,
                CandidateName = "Kandidaat Test",
                CandidateEmail = CandidateEmail,
                CandidateCity = "Naaldwijk",
                PreferredTransport = "Fiets",
                EstimatedTravelMinutes = 18,
                DistanceKm = 4.2,
                CandidateAgeYears = 19,
                SnapshotAvailabilityJson = """{"flexibleTimes":false,"slots":{"ma":["avond"],"di":["avond"]},"minHours":8,"maxHours":16}""",
                Status = ApplicationStatus.Pending,
                EmailVerifiedAt = DateTime.UtcNow.AddHours(-1),
                WorkPermitConfirmed = true,
                MatchPercent = 78,
                MatchBreakdownJson = """{"TotalPercent":78,"TravelScore":32,"HoursScore":24,"DayPartsScore":22}""",
                ViaSafetyNet = false,
                Motivation = "Sterke motivatie voor deze rol.",
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new Application
            {
                Id = AcceptedApplicationId,
                VacancyId = VacancyId,
                CandidateUserId = Guid.Parse("c1000000-0000-0000-0000-000000000029"),
                CandidateName = "Kandidaat Test",
                CandidateEmail = "andere-kandidaat@jobsy.local",
                CandidateCity = "Delft",
                PreferredTransport = "Fiets",
                EstimatedTravelMinutes = 12,
                DistanceKm = 2.1,
                Status = ApplicationStatus.Accepted,
                EmailVerifiedAt = DateTime.UtcNow.AddHours(-3),
                WorkPermitConfirmed = true,
                MatchPercent = 85,
                MatchBreakdownJson = """{"TotalPercent":85,"TravelScore":36,"HoursScore":25,"DayPartsScore":24}""",
                ViaSafetyNet = false,
                Motivation = "Accepted candidate motivation.",
                RespondedAt = DateTime.UtcNow.AddHours(-2),
                CreatedAt = DateTime.UtcNow.AddHours(-4)
            });

        db.MinimumWageRates.Add(new MinimumWageRate
        {
            Id = Guid.NewGuid(),
            AgeYears = 21,
            HourlyRate = 14.06m,
            Label = "21+",
            EffectiveFrom = today.AddYears(-1)
        });

        db.SaveChanges();
        _seeded = true;
    }

    private sealed class AllowAllModeration : IVacancyContentModerationService
    {
        public Task<VacancyContentModerationResult> CheckAsync(
            string title,
            string description,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new VacancyContentModerationResult(true, null));
    }
}
