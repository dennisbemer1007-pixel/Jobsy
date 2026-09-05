using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
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
        var cache = discover.Headers.CacheControl?.ToString() ?? "";
        Assert.Contains("private", cache, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("public", cache, StringComparison.OrdinalIgnoreCase);

        var catalog = await client.GetAsync("api/vacancies/discover?transport=Fiets&maxMinutes=90");
        Assert.Equal(HttpStatusCode.OK, catalog.StatusCode);
        var webItems = await catalog.Content.ReadFromJsonAsync<List<Jobsy.Web.Models.VacancyListItem>>(JsonOpts);
        Assert.NotNull(webItems);
        var pin = Assert.Single(webItems!, v => v.Id == _factory.VacancyId);
        Assert.True(double.IsFinite(pin.Latitude) && pin.Latitude != 0);
        Assert.True(double.IsFinite(pin.Longitude) && pin.Longitude != 0);
        Assert.False(string.IsNullOrWhiteSpace(pin.Title));

        var detail = await client.GetAsync($"api/vacancies/{_factory.VacancyId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);

        var wages = await client.GetAsync("api/wages");
        Assert.Equal(HttpStatusCode.OK, wages.StatusCode);

        var kvk = await client.GetAsync("api/kvk/12345678/establishments");
        Assert.Equal(HttpStatusCode.OK, kvk.StatusCode);
    }

    [Fact]
    public async Task Guest_can_read_public_crawl_index_without_pii()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("api/site/crawl-index");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var vacancies = json.GetProperty("vacancies");
        Assert.True(vacancies.GetArrayLength() >= 1);
        var match = vacancies.EnumerateArray().Single(v => v.GetProperty("id").GetGuid() == _factory.VacancyId);
        Assert.False(match.TryGetProperty("title", out _));
        Assert.False(match.TryGetProperty("description", out _));
        Assert.False(match.TryGetProperty("email", out _));
        Assert.False(match.TryGetProperty("companyName", out _));

        var companies = json.GetProperty("companyPaths")
            .EnumerateArray()
            .Select(x => x.GetString())
            .ToList();
        Assert.Contains("/12345678", companies);
    }

    [Fact]
    public async Task Guest_can_read_public_branding_without_company_pii()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("api/site/branding");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("companyName").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("slogan").GetString()));
        Assert.False(json.TryGetProperty("vatBufferIban", out _));
        Assert.False(json.TryGetProperty("kvkNumber", out _));
        Assert.False(json.TryGetProperty("address", out _));
        Assert.False(json.TryGetProperty("email", out _));

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("api/settings/company")).StatusCode);
    }

    [Fact]
    public async Task Admin_company_slogan_is_served_on_public_branding()
    {
        var admin = AdminClient();
        var guest = _factory.CreateClient();
        var current = await admin.GetFromJsonAsync<JsonElement>("api/settings/company", JsonOpts);
        Assert.True(current.ValueKind == JsonValueKind.Object);

        static string? Read(JsonElement e, string name)
            => e.TryGetProperty(name, out var p) && p.ValueKind != JsonValueKind.Null ? p.GetString() : null;

        var originalSlogan = Read(current, "slogan");
        var unique = "Pantser-test " + Guid.NewGuid().ToString("N")[..8];
        var put = await admin.PutAsJsonAsync("api/settings/company", new
        {
            companyName = Read(current, "companyName"),
            slogan = unique,
            address = Read(current, "address"),
            postalCode = Read(current, "postalCode"),
            city = Read(current, "city"),
            country = Read(current, "country"),
            kvkNumber = Read(current, "kvkNumber"),
            vatNumber = Read(current, "vatNumber"),
            phone = Read(current, "phone"),
            email = Read(current, "email")
        });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var branding = await guest.GetFromJsonAsync<JsonElement>("api/site/branding", JsonOpts);
        Assert.Equal(unique, branding.GetProperty("slogan").GetString());

        var restore = await admin.PutAsJsonAsync("api/settings/company", new
        {
            companyName = Read(current, "companyName"),
            slogan = originalSlogan,
            address = Read(current, "address"),
            postalCode = Read(current, "postalCode"),
            city = Read(current, "city"),
            country = Read(current, "country"),
            kvkNumber = Read(current, "kvkNumber"),
            vatNumber = Read(current, "vatNumber"),
            phone = Read(current, "phone"),
            email = Read(current, "email")
        });
        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
    }

    [Fact]
    public async Task Guest_can_read_public_map_view_without_pii()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("api/vacancies/map-view");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(json.GetProperty("pinCount").GetInt32() >= 1);
        Assert.InRange(json.GetProperty("lat").GetDouble(), 50, 54);
        Assert.InRange(json.GetProperty("lng").GetDouble(), 3, 8);
        Assert.InRange(json.GetProperty("zoom").GetDouble(), 8, 13);
        Assert.False(json.TryGetProperty("title", out _));
        Assert.False(json.TryGetProperty("companyName", out _));
        Assert.False(json.TryGetProperty("email", out _));
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
        Assert.False(pending.UploadedCvAvailable);
        Assert.Equal(0, pending.CandidateReferenceCount);
        Assert.Null(pending.CandidateName);
        Assert.Null(pending.CandidateEmail);
        Assert.Null(pending.CandidateCity);
        Assert.Equal(4.2, pending.DistanceKm);
        Assert.Equal(19, pending.CandidateAgeYears);
        Assert.False(string.IsNullOrWhiteSpace(pending.AvailabilitySummary));
        Assert.False(string.IsNullOrWhiteSpace(pending.SnapshotAvailabilityJson));
        Assert.Contains("avond", pending.SnapshotAvailabilityJson, StringComparison.OrdinalIgnoreCase);
        Assert.False(pending.WorkPermitConfirmed); // gated until accept
        Assert.Equal("Sterke motivatie voor deze rol.", pending.Motivation);
        Assert.Null(pending.StudentNumber);
        Assert.Null(pending.SchoolEmail);
        Assert.Null(pending.StudyProgram);

        var accepted = Assert.Single(list!, a => a.Id == _factory.AcceptedApplicationId);
        Assert.True(accepted.PiiRevealed);
        Assert.True(accepted.CvPdfAvailable);
        Assert.True(accepted.UploadedCvAvailable);
        Assert.Equal(2, accepted.CandidateReferenceCount);
        Assert.Equal("Kandidaat Test", accepted.CandidateName);
        Assert.Null(accepted.CandidateEmail);
        Assert.Null(accepted.CandidatePhone);
        Assert.Null(accepted.MatchBreakdownJson);
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
    public async Task Uploaded_cv_download_enforces_owner_verify_company_scope_and_post_accept()
    {
        var employer = EmployerClient();
        var candidate = CandidateClient();
        var guest = _factory.CreateClient();

        var pendingEmployer = await employer.GetAsync($"api/applications/{_factory.PendingApplicationId}/uploaded-cv");
        Assert.Equal(HttpStatusCode.Forbidden, pendingEmployer.StatusCode);

        var acceptedEmployer = await employer.GetAsync($"api/applications/{_factory.AcceptedApplicationId}/uploaded-cv");
        Assert.Equal(HttpStatusCode.OK, acceptedEmployer.StatusCode);
        Assert.Equal("application/pdf", acceptedEmployer.Content.Headers.ContentType?.MediaType);
        var acceptedBytes = await acceptedEmployer.Content.ReadAsByteArrayAsync();
        Assert.True(acceptedBytes.Length > 8);

        var pendingOwner = await candidate.GetAsync($"api/applications/{_factory.PendingApplicationId}/uploaded-cv");
        Assert.Equal(HttpStatusCode.OK, pendingOwner.StatusCode);

        var otherCandidatesCv = await candidate.GetAsync($"api/applications/{_factory.AcceptedApplicationId}/uploaded-cv");
        Assert.Equal(HttpStatusCode.Forbidden, otherCandidatesCv.StatusCode);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await guest.GetAsync($"api/applications/{_factory.AcceptedApplicationId}/uploaded-cv")).StatusCode);

        var foreignEmail = await _factory.SeedForeignBranchEmployerAsync();
        var foreign = Authed(foreignEmail);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await foreign.GetAsync($"api/applications/{_factory.AcceptedApplicationId}/uploaded-cv")).StatusCode);

        var unverifiedId = await _factory.SeedUnverifiedApplicationWithUploadedCvAsync();
        var unverified = await candidate.GetAsync($"api/applications/{unverifiedId}/uploaded-cv");
        Assert.Equal(HttpStatusCode.BadRequest, unverified.StatusCode);
    }

    [Fact]
    public async Task Employer_lobsy_cv_pdf_hides_direct_contact_until_hired()
    {
        var (vacancyId, applicationId) = await _factory.SeedVacancyWithPendingApplicationAsync();
        var client = EmployerClient();

        var pendingPdf = await client.GetAsync($"api/applications/{applicationId}/lobsy-cv.pdf");
        Assert.Equal(HttpStatusCode.Forbidden, pendingPdf.StatusCode);

        var accept = await client.PostAsJsonAsync(
            $"api/applications/{applicationId}/react",
            new ReactToApplicationRequest(ApplicationStatus.Accepted));
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);

        var acceptedPdfResponse = await client.GetAsync($"api/applications/{applicationId}/lobsy-cv.pdf");
        Assert.Equal(HttpStatusCode.OK, acceptedPdfResponse.StatusCode);
        Assert.Equal("application/pdf", acceptedPdfResponse.Content.Headers.ContentType?.MediaType);
        var acceptedPdf = await acceptedPdfResponse.Content.ReadAsByteArrayAsync();
        Assert.True(acceptedPdf.Length > 500);

        var acceptedList = await client.GetFromJsonAsync<List<JsonElement>>(
            $"api/applications?vacancyId={vacancyId}",
            JsonOpts);
        var acceptedRow = Assert.Single(acceptedList!, a => a.GetProperty("id").GetGuid() == applicationId);
        Assert.True(
            !acceptedRow.TryGetProperty("candidateEmail", out var acceptedEmail)
            || acceptedEmail.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined);
        Assert.True(
            !acceptedRow.TryGetProperty("candidatePhone", out var acceptedPhone)
            || acceptedPhone.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined);

        var hire = await client.PostAsJsonAsync(
            $"api/applications/vacancies/{vacancyId}/fulfill/{applicationId}",
            new FulfillVacancyRequest(RejectOtherApplications: false));
        Assert.Equal(HttpStatusCode.OK, hire.StatusCode);

        var hiredList = await client.GetFromJsonAsync<List<JsonElement>>(
            $"api/applications?vacancyId={vacancyId}",
            JsonOpts);
        var hired = Assert.Single(hiredList!, a => a.GetProperty("id").GetGuid() == applicationId);
        Assert.Equal(nameof(ApplicationStatus.Hired), hired.GetProperty("status").GetString());
        Assert.Equal(_factory.CandidateEmail, hired.GetProperty("candidateEmail").GetString());
        Assert.Equal("0611122233", hired.GetProperty("candidatePhone").GetString());

        var hiredPdfResponse = await client.GetAsync($"api/applications/{applicationId}/lobsy-cv.pdf");
        Assert.Equal(HttpStatusCode.OK, hiredPdfResponse.StatusCode);
        var hiredPdf = await hiredPdfResponse.Content.ReadAsByteArrayAsync();
        Assert.True(hiredPdf.Length > 500);
        Assert.True(hiredPdf.Length > acceptedPdf.Length);
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
    public async Task Guest_can_submit_feedback_but_cannot_list_or_automate()
    {
        var client = _factory.CreateClient();
        var submit = await client.PostAsJsonAsync("api/feedback", new
        {
            type = "Bug",
            description = "Kaart blijft leeg",
            pageUrl = "https://lobsy.test/",
            browserInfo = "TestAgent",
            deviceInfo = "CI"
        });
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
        var created = await submit.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("Bug", created.GetProperty("type").GetString());
        Assert.False(created.TryGetProperty("screenshotBytes", out _));
        Assert.False(created.TryGetProperty("screenshotDataUrl", out _));

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("api/feedback")).StatusCode);
        var id = created.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync($"api/feedback/{id}/automate", new { prompt = "x" })).StatusCode);
    }

    [Fact]
    public async Task Employer_cannot_list_feedback()
    {
        var client = EmployerClient();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/feedback")).StatusCode);
    }

    [Fact]
    public async Task Admin_can_list_feedback_generate_prompt_and_store_pr_via_webhook()
    {
        var candidate = CandidateClient();
        var submit = await candidate.PostAsJsonAsync("api/feedback", new
        {
            type = "Feature",
            description = "Sla filters op",
            pageUrl = "https://lobsy.test/candidate/liked",
            browserInfo = "Firefox",
            deviceInfo = "Linux"
        });
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
        var created = await submit.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var id = created.GetProperty("id").GetGuid();
        Assert.Equal("Candidate", created.GetProperty("userRole").GetString());

        var admin = AdminClient();
        var list = await admin.GetFromJsonAsync<List<JsonElement>>("api/feedback", JsonOpts);
        Assert.Contains(list!, item => item.GetProperty("id").GetGuid() == id);
        Assert.All(list!, item =>
        {
            Assert.False(item.TryGetProperty("screenshotBytes", out _));
            Assert.False(item.TryGetProperty("description", out _));
        });

        var prompt = await admin.PostAsJsonAsync($"api/feedback/{id}/prompt", new { prompt = (string?)null });
        Assert.Equal(HttpStatusCode.OK, prompt.StatusCode);
        var promptBody = await prompt.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Contains("Sla filters op", promptBody.GetProperty("prompt").GetString());

        var automate = await admin.PostAsJsonAsync($"api/feedback/{id}/automate", new { prompt = "Maak filter-presets." });
        Assert.Equal(HttpStatusCode.OK, automate.StatusCode);
        var automateBody = await automate.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.False(automateBody.GetProperty("launched").GetBoolean());
        Assert.Equal("InProgress", automateBody.GetProperty("feedback").GetProperty("status").GetString());

        var attach = await admin.PostAsJsonAsync(
            $"api/feedback/{id}/pull-request",
            new { pullRequestUrl = "https://github.com/lobsy/lobsy/pull/7" });
        Assert.Equal(HttpStatusCode.OK, attach.StatusCode);
        var attached = await attach.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("https://github.com/lobsy/lobsy/pull/7", attached.GetProperty("pullRequestUrl").GetString());
    }

    [Fact]
    public async Task Feedback_screenshot_is_stored_and_served_only_to_admin()
    {
        const string jpegB64 = "AAD/2Q==";
        var guest = _factory.CreateClient();
        var submit = await guest.PostAsJsonAsync("api/feedback", new
        {
            type = "Error",
            description = "Printscreen van de homepage",
            pageUrl = "https://lobsy.test/home",
            browserInfo = "Mozilla/5.0",
            deviceInfo = "1440×900",
            screenshotDataUrl = "data:image/jpeg;base64," + jpegB64
        });
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
        var created = await submit.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var id = created.GetProperty("id").GetGuid();
        Assert.True(created.GetProperty("hasScreenshot").GetBoolean());
        Assert.False(created.TryGetProperty("screenshotDataUrl", out _));

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await guest.GetAsync($"api/feedback/{id}/screenshot")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await EmployerClient().GetAsync($"api/feedback/{id}/screenshot")).StatusCode);

        var admin = AdminClient();
        var shot = await admin.GetAsync($"api/feedback/{id}/screenshot");
        Assert.Equal(HttpStatusCode.OK, shot.StatusCode);
        Assert.Equal("image/jpeg", shot.Content.Headers.ContentType?.MediaType);
        var bytes = await shot.Content.ReadAsByteArrayAsync();
        Assert.Equal(Convert.FromBase64String(jpegB64), bytes);

        var prompt = await admin.PostAsJsonAsync($"api/feedback/{id}/prompt", new { prompt = (string?)null });
        var promptBody = await prompt.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var text = promptBody.GetProperty("prompt").GetString();
        Assert.Contains("Printscreen van de homepage", text);
        Assert.Contains("/home", text);
        Assert.Contains("bijgevoegd", text);
        Assert.Contains("fix/feedback-", text);
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

    // ─── Branch react / hire happy + exception paths ─────────────────────────

    [Fact]
    public async Task Branch_can_accept_contact_and_hire_verified_application()
    {
        var (vacancyId, applicationId) = await _factory.SeedVacancyWithPendingApplicationAsync();
        var client = EmployerClient();

        var accept = await client.PostAsJsonAsync(
            $"api/applications/{applicationId}/react",
            new ReactToApplicationRequest(ApplicationStatus.Accepted));
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        var accepted = await accept.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal(nameof(ApplicationStatus.Accepted), accepted.GetProperty("status").GetString());

        var contact = await client.PostAsync(
            $"api/applications/{applicationId}/contact",
            null);
        Assert.Equal(HttpStatusCode.OK, contact.StatusCode);
        var contacting = await contact.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal(nameof(ApplicationStatus.EmployerContacting), contacting.GetProperty("status").GetString());

        var hire = await client.PostAsJsonAsync(
            $"api/applications/vacancies/{vacancyId}/fulfill/{applicationId}",
            new FulfillVacancyRequest(RejectOtherApplications: true));
        Assert.Equal(HttpStatusCode.OK, hire.StatusCode);

        var apps = await client.GetFromJsonAsync<List<JsonElement>>(
            $"api/applications?vacancyId={vacancyId}",
            JsonOpts);
        Assert.NotNull(apps);
        var hired = Assert.Single(apps!, a => a.GetProperty("id").GetGuid() == applicationId);
        Assert.Equal(nameof(ApplicationStatus.Hired), hired.GetProperty("status").GetString());
        Assert.True(hired.GetProperty("piiRevealed").GetBoolean());
    }

    [Fact]
    public async Task Branch_can_reject_pending_application_and_cannot_hire_rejected()
    {
        var (vacancyId, pendingId) = await _factory.SeedVacancyWithPendingApplicationAsync();
        var client = EmployerClient();

        var reject = await client.PostAsJsonAsync(
            $"api/applications/{pendingId}/react",
            new ReactToApplicationRequest(ApplicationStatus.Rejected));
        Assert.Equal(HttpStatusCode.OK, reject.StatusCode);

        var hire = await client.PostAsJsonAsync(
            $"api/applications/vacancies/{vacancyId}/fulfill/{pendingId}",
            new FulfillVacancyRequest(RejectOtherApplications: false));
        Assert.Equal(HttpStatusCode.BadRequest, hire.StatusCode);
    }

    [Fact]
    public async Task Branch_contact_before_accept_is_rejected()
    {
        var (_, pendingId) = await _factory.SeedVacancyWithPendingApplicationAsync();
        var client = EmployerClient();

        var contact = await client.PostAsync($"api/applications/{pendingId}/contact", null);
        Assert.Equal(HttpStatusCode.BadRequest, contact.StatusCode);
    }

    // ─── Extended roles: nav + smoke authz ───────────────────────────────────

    [Fact]
    public void Role_nav_catalog_covers_all_login_roles_with_how_lobsy()
    {
        Assert.DoesNotContain(RoleNavCatalog.Candidate, n => n.Href.Contains("hoe-werkt", StringComparison.Ordinal));
        Assert.Contains(RoleNavCatalog.Candidate, n => n.Href == "/candidate/vacancies");
        Assert.DoesNotContain(RoleNavCatalog.Branch, n => n.Href == "/candidate/vacancies");
        Assert.DoesNotContain(RoleNavCatalog.Enterprise, n => n.Href == "/candidate/vacancies");
        Assert.DoesNotContain(RoleNavCatalog.Branch, n => n.Href == "/hoe-werkt-lobsy");
        Assert.DoesNotContain(RoleNavCatalog.Regional, n => n.Href == "/hoe-werkt-lobsy");
        Assert.DoesNotContain(RoleNavCatalog.Enterprise, n => n.Href == "/hoe-werkt-lobsy");
        Assert.DoesNotContain(RoleNavCatalog.Intermediary, n => n.Href == "/hoe-werkt-lobsy");
        Assert.DoesNotContain(RoleNavCatalog.SalesManager, n => n.Href == "/hoe-werkt-lobsy");
        Assert.DoesNotContain(RoleNavCatalog.Ambassadeur, n => n.Href == "/hoe-werkt-lobsy");
        Assert.DoesNotContain(RoleNavCatalog.Admin, n => n.TitleKey == "Nav.HowLobsyWorks");

        Assert.Equal("/candidate/hoe-werkt-lobsy", RoleNavCatalog.HowLobsyHrefFor(NavPrincipal(JobsyRoles.Candidate)));
        Assert.Equal("/hoe-werkt-lobsy", RoleNavCatalog.HowLobsyHrefFor(NavPrincipal(JobsyRoles.BranchManager)));
        Assert.Equal("/hoe-werkt-lobsy", RoleNavCatalog.HowLobsyHrefFor(NavPrincipal(JobsyRoles.RegionalManager)));
        Assert.Equal("/hoe-werkt-lobsy", RoleNavCatalog.HowLobsyHrefFor(NavPrincipal(JobsyRoles.EnterpriseManager)));
        Assert.Equal("/hoe-werkt-lobsy", RoleNavCatalog.HowLobsyHrefFor(NavPrincipal(JobsyRoles.Intermediary)));
        Assert.Equal("/hoe-werkt-lobsy", RoleNavCatalog.HowLobsyHrefFor(NavPrincipal(JobsyRoles.SalesManager)));
        Assert.Equal("/hoe-werkt-lobsy", RoleNavCatalog.HowLobsyHrefFor(NavPrincipal(JobsyRoles.Ambassadeur)));
        Assert.Null(RoleNavCatalog.HowLobsyHrefFor(NavPrincipal(JobsyRoles.Admin)));

        Assert.Contains(RoleNavCatalog.Ambassadeur, n => n.Href == "/ambassadeur/toolkit");
        Assert.Contains(RoleNavCatalog.SalesManager, n => n.Href == "/salesmanager/toolkit");
        Assert.Contains(RoleNavCatalog.Enterprise, n => n.Href == "/employer/organization" && n.DesktopOnly);
    }

    private static ClaimsPrincipal NavPrincipal(string role)
        => new(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test"));

    [Fact]
    public async Task Regional_enterprise_intermediary_can_load_manage_and_metrics()
    {
        foreach (var email in new[]
                 {
                     _factory.RegionalEmail,
                     _factory.EnterpriseEmail,
                     _factory.IntermediaryEmail
                 })
        {
            var client = Authed(email);
            var manage = await client.GetAsync("api/vacancies/manage");
            Assert.Equal(HttpStatusCode.OK, manage.StatusCode);
            var metrics = await client.GetAsync("api/metrics/summary?period=day");
            Assert.Equal(HttpStatusCode.OK, metrics.StatusCode);
            var mine = await client.GetAsync("api/companies/mine");
            Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        }
    }

    [Fact]
    public async Task Sales_and_ambassadeur_can_load_dashboards()
    {
        var sales = Authed(_factory.SalesEmail);
        var salesDash = await sales.GetAsync("api/sales-managers/me/dashboard");
        Assert.Equal(HttpStatusCode.OK, salesDash.StatusCode);

        var amb = Authed(_factory.AmbassadeurEmail);
        var ambDash = await amb.GetAsync("api/ambassadeurs/me/dashboard");
        Assert.Equal(HttpStatusCode.OK, ambDash.StatusCode);
    }

    [Fact]
    public async Task Every_login_role_can_open_home_and_core_reads()
    {
        // Guest
        var guest = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await guest.GetAsync("api/vacancies/discover?transport=Fiets&maxMinutes=60")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await guest.GetAsync("api/wages")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync("api/me/profile")).StatusCode);

        // Candidate home + saved/applications
        var candidate = CandidateClient();
        Assert.Equal(HttpStatusCode.OK, (await candidate.GetAsync("api/me/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await candidate.GetAsync("api/me/applications")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await candidate.GetAsync("api/me/metrics/summary?period=week")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await candidate.GetAsync("api/me/likes")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await candidate.GetAsync("api/me/shares")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await candidate.GetAsync("api/notifications/unread-count")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await candidate.GetAsync("api/metrics/summary?period=week")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await candidate.GetAsync("api/vacancies/manage")).StatusCode);

        // Branch / regional / enterprise / intermediary employer home
        foreach (var email in new[]
                 {
                     _factory.EmployerEmail,
                     _factory.RegionalEmail,
                     _factory.EnterpriseEmail,
                     _factory.IntermediaryEmail
                 })
        {
            var client = Authed(email);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/metrics/summary?period=week")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/metrics/vacancy-performance?period=week&take=3")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/vacancies/manage")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/companies/mine")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/tokens/balance")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/salary-tables")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/notifications/unread-count")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("api/admin/users")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.OK, (await EmployerClient().GetAsync("api/applications")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Authed(_factory.EnterpriseEmail).GetAsync("api/company-users")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Authed(_factory.IntermediaryEmail).GetAsync("api/metrics/client-performance?period=week")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await EmployerClient().GetAsync("api/company-users")).StatusCode);

        // Admin home + settings/finance/logging
        var admin = AdminClient();
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("api/metrics/summary?period=week")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("api/metrics/vacancy-performance?period=week&take=3")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("api/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("api/admin/vacancies")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("api/admin/companies")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("api/integrations/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("api/platform-logs")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("api/settings/token-pricing")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("api/feedback")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("api/notifications/unread-count")).StatusCode);

        // Sales + ambassadeur home + toolkit/finance reads
        var sales = Authed(_factory.SalesEmail);
        Assert.Equal(HttpStatusCode.OK, (await sales.GetAsync("api/sales-managers/me/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await sales.GetAsync("api/sales-managers/me/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await sales.GetAsync("api/sales-managers/me/invoices")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await sales.GetAsync("api/sales-commercial/catalog")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await sales.GetAsync("api/notifications/unread-count")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await sales.GetAsync("api/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await sales.GetAsync("api/vacancies/manage")).StatusCode);

        var amb = Authed(_factory.AmbassadeurEmail);
        Assert.Equal(HttpStatusCode.OK, (await amb.GetAsync("api/ambassadeurs/me/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await amb.GetAsync("api/ambassadeurs/me/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await amb.GetAsync("api/ambassadeurs/me/invoices")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await amb.GetAsync("api/notifications/unread-count")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await amb.GetAsync("api/vacancies/manage")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await amb.GetAsync("api/admin/users")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await AdminClient().PostAsync("api/dashboard/refresh?period=week", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await EmployerClient().PostAsync("api/dashboard/refresh?period=week", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Authed(_factory.IntermediaryEmail).PostAsync("api/dashboard/refresh?period=week", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await sales.PostAsync("api/dashboard/refresh", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await amb.PostAsync("api/dashboard/refresh", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await CandidateClient().PostAsync("api/dashboard/refresh", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().PostAsync("api/dashboard/refresh", null)).StatusCode);
    }

    [Fact]
    public async Task Intermediary_cannot_re_role_existing_branch_manager()
    {
        var response = await Authed(_factory.IntermediaryEmail).PostAsJsonAsync("api/company-users/invite", new
        {
            email = _factory.EmployerEmail,
            fullName = "Hijack",
            role = "Intermediary",
            primaryCompanyId = _factory.CompanyId
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var branch = await db.Users.AsNoTracking().SingleAsync(u => u.Email == _factory.EmployerEmail);
        Assert.Equal(UserRole.BranchManager, branch.Role);
    }

    [Fact]
    public async Task Intermediary_company_users_are_peer_only()
    {
        var users = await Authed(_factory.IntermediaryEmail)
            .GetFromJsonAsync<List<CompanyUserDto>>("api/company-users", JsonOpts);
        Assert.NotNull(users);
        Assert.All(users!, u => Assert.Equal("Intermediary", u.Role));
        Assert.DoesNotContain(users!, u =>
            u.Email.Equals(_factory.EmployerEmail, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(users!, u =>
            u.Email.Equals(_factory.EnterpriseEmail, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Intermediary_cannot_mutate_client_company_contact()
    {
        var response = await Authed(_factory.IntermediaryEmail).PutAsJsonAsync(
            $"api/companies/{_factory.CompanyId}/contact-preference",
            new
            {
                directContactEnabled = true,
                contactPreferMail = true,
                contactPreferPhone = false,
                contactPreferWhatsApp = false,
                contactEmail = "x@example.com",
                contactPhone = (string?)null,
                contactWhatsApp = (string?)null
            });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Intermediary_cannot_read_client_billing_history()
    {
        var response = await Authed(_factory.IntermediaryEmail)
            .GetAsync($"api/companies/{_factory.CompanyId}/billing-history");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Intermediary_cannot_update_existing_branch_manager()
    {
        var response = await Authed(_factory.IntermediaryEmail).PutAsJsonAsync(
            $"api/company-users/{_factory.EmployerId}",
            new
            {
                fullName = "Hijack",
                role = "Intermediary",
                primaryCompanyId = _factory.CompanyId,
                isActive = true
            });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Branch_cannot_approve_publish()
    {
        var response = await EmployerClient()
            .PostAsync($"api/vacancies/{_factory.VacancyId}/approve-publish", content: null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Regional_cannot_create_vacancy()
    {
        var response = await Authed(_factory.RegionalEmail).PostAsJsonAsync("api/vacancies", new
        {
            companyId = _factory.CompanyId,
            title = "Regio mag dit niet"
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Analytics_impressions_require_cookie_consent()
    {
        var guest = _factory.CreateClient();
        var denied = await guest.PostAsJsonAsync("api/analytics/impressions", new
        {
            vacancyIds = new[] { _factory.VacancyId },
            anonymousKey = "anon-" + Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.OK, denied.StatusCode);
        var deniedBody = await denied.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal(0, deniedBody.GetProperty("recorded").GetInt32());

        guest.DefaultRequestHeaders.Add("X-Jobsy-Cookie-Consent", "analytics");
        var allowed = await guest.PostAsJsonAsync("api/analytics/impressions", new
        {
            vacancyIds = new[] { _factory.VacancyId },
            anonymousKey = "anon-" + Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        var allowedBody = await allowed.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(allowedBody.GetProperty("recorded").GetInt32() >= 1);
    }

    [Fact]
    public async Task Regional_cannot_react_to_applications()
    {
        var (_, pendingId) = await _factory.SeedVacancyWithPendingApplicationAsync();
        var client = Authed(_factory.RegionalEmail);
        var react = await client.PostAsJsonAsync(
            $"api/applications/{pendingId}/react",
            new ReactToApplicationRequest(ApplicationStatus.Accepted));
        Assert.Equal(HttpStatusCode.Forbidden, react.StatusCode);
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
    public Guid IntermediaryOrgId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000002");
    public Guid VacancyId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000010");
    public Guid LowMatchVacancyId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000011");
    public Guid NightShiftVacancyId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000012");
    public Guid CandidateId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000020");
    public Guid EmployerId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000021");
    public Guid AdminId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000022");
    public Guid RegionalId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000023");
    public Guid EnterpriseId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000024");
    public Guid IntermediaryId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000025");
    public Guid SalesId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000026");
    public Guid AmbassadeurId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000027");
    public Guid PendingApplicationId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000030");
    public Guid AcceptedApplicationId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000031");
    public Guid SalaryTableId { get; } = Guid.Parse("c1000000-0000-0000-0000-000000000040");

    public string CandidateEmail => "kandidaat@jobsy.local";
    public string EmployerEmail => "branch@jobsy.local";
    public string AdminEmail => "admin@jobsy.local";
    public string RegionalEmail => "regio@jobsy.local";
    public string EnterpriseEmail => "enterprise@jobsy.local";
    public string IntermediaryEmail => "intermediair@jobsy.local";
    public string SalesEmail => "sales@jobsy.local";
    public string AmbassadeurEmail => "ambassadeur@jobsy.local";

    private readonly string _dbName = "RoleFunctional-" + Guid.NewGuid();
    private bool _seeded;
    private int _extraSeedCounter;

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
        db.Companies.Add(new Company
        {
            Id = IntermediaryOrgId,
            Name = "Test Intermediair",
            KvkNumber = "87654321",
            Address = "Intermediairweg 1, Naaldwijk",
            Location = new GeoPoint(52.01, 4.21)
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
            },
            new User
            {
                Id = RegionalId,
                Email = RegionalEmail,
                FullName = "Regional Manager",
                Role = UserRole.RegionalManager,
                IsActive = true,
                CompanyId = CompanyId
            },
            new User
            {
                Id = EnterpriseId,
                Email = EnterpriseEmail,
                FullName = "Enterprise Manager",
                Role = UserRole.EnterpriseManager,
                IsActive = true,
                CompanyId = CompanyId
            },
            new User
            {
                Id = IntermediaryId,
                Email = IntermediaryEmail,
                FullName = "Intermediary",
                Role = UserRole.Intermediary,
                IsActive = true,
                CompanyId = IntermediaryOrgId
            },
            new User
            {
                Id = SalesId,
                Email = SalesEmail,
                FullName = "Sales Manager",
                Role = UserRole.SalesManager,
                IsActive = true
            },
            new User
            {
                Id = AmbassadeurId,
                Email = AmbassadeurEmail,
                FullName = "Ambassadeur",
                Role = UserRole.Ambassadeur,
                IsActive = true
            });

        db.UserCompanies.AddRange(
            new UserCompany { UserId = RegionalId, CompanyId = CompanyId },
            new UserCompany { UserId = EnterpriseId, CompanyId = CompanyId },
            new UserCompany { UserId = IntermediaryId, CompanyId = IntermediaryOrgId },
            new UserCompany { UserId = IntermediaryId, CompanyId = CompanyId },
            new UserCompany { UserId = EmployerId, CompanyId = CompanyId });

        db.SalesManagerProfiles.Add(new SalesManagerProfile
        {
            Id = Guid.Parse("c1000000-0000-0000-0000-000000000050"),
            UserId = SalesId,
            CompanyName = "Sales Demo BV",
            TrackingCode = "SM-TEST01",
            AgreementSignedAt = DateTime.UtcNow.AddDays(-10),
            AgreementVersion = "v1",
            OnboardingCompletedAt = DateTime.UtcNow.AddDays(-10),
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow
        });

        db.AmbassadeurProfiles.Add(new AmbassadeurProfile
        {
            Id = Guid.Parse("c1000000-0000-0000-0000-000000000051"),
            UserId = AmbassadeurId,
            CompanyName = "Ambassadeur Demo",
            TrackingCode = "AM-TEST01",
            AgreementSignedAt = DateTime.UtcNow.AddDays(-10),
            AgreementVersion = "v1",
            OnboardingCompletedAt = DateTime.UtcNow.AddDays(-10),
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow
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
                SuitableFor65Plus = true,
                RequireEmailVerification = true
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
                RequireEmailVerification = true,
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
                HasUploadedCv = true,
                CandidateReferenceCount = 3,
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
                SnapshotPhoneNumber = "0619988777",
                Status = ApplicationStatus.Accepted,
                EmailVerifiedAt = DateTime.UtcNow.AddHours(-3),
                WorkPermitConfirmed = true,
                MatchPercent = 85,
                MatchBreakdownJson = """{"TotalPercent":85,"TravelScore":36,"HoursScore":25,"DayPartsScore":24}""",
                ViaSafetyNet = false,
                Motivation = "Accepted candidate motivation.",
                HasUploadedCv = true,
                CandidateReferenceCount = 2,
                RespondedAt = DateTime.UtcNow.AddHours(-2),
                CreatedAt = DateTime.UtcNow.AddHours(-4)
            });

        var pendingCv = "%PDF-1.4 pending-cv"u8.ToArray();
        var acceptedCv = "%PDF-1.4 accepted-cv"u8.ToArray();
        db.ApplicationUploadedCvs.AddRange(
            new ApplicationUploadedCv
            {
                ApplicationId = PendingApplicationId,
                FileName = "pending-cv.pdf",
                ContentType = "application/pdf",
                Content = pendingCv,
                SizeBytes = pendingCv.Length
            },
            new ApplicationUploadedCv
            {
                ApplicationId = AcceptedApplicationId,
                FileName = "accepted-cv.pdf",
                ContentType = "application/pdf",
                Content = acceptedCv,
                SizeBytes = acceptedCv.Length
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

    /// <summary>
    /// Isolated vacancy + pending verified application for react/hire tests (does not mutate shared seed).
    /// </summary>
    public async Task<(Guid VacancyId, Guid ApplicationId)> SeedVacancyWithPendingApplicationAsync()
    {
        EnsureSeeded();
        var n = Interlocked.Increment(ref _extraSeedCounter);
        var vacancyId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            Title = $"Reactie-flow vacature {n}",
            Description = "Geïsoleerde vacature voor accept/hire regressie.",
            HourlyWage = 14.50m,
            StartDate = today.AddDays(-1),
            EndDate = today.AddMonths(1),
            Status = VacancyStatus.Active,
            CompanyId = CompanyId,
            Location = new GeoPoint(52.0, 4.2),
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.Winkel,
            WorkTypeLabels = "Winkel",
            SalaryTableId = SalaryTableId,
            PublishedAtUtc = DateTime.UtcNow.AddHours(-2),
            MinHoursPerWeek = 8,
            MaxHoursPerWeek = 16,
            FlexibleTimes = true,
            FlexibleScheduleSource = nameof(FlexibleScheduleSource.Manual),
            MaxApplications = 5,
            CategoryId = VacancyCategoryDefaults.RegulierId,
            RequireEmailVerification = true
        });
        db.Applications.Add(new Application
        {
            Id = applicationId,
            VacancyId = vacancyId,
            CandidateUserId = CandidateId,
            CandidateName = "Kandidaat Test",
            CandidateEmail = CandidateEmail,
            CandidateCity = "Naaldwijk",
            PreferredTransport = "Fiets",
            EstimatedTravelMinutes = 10,
            DistanceKm = 2.5,
            CandidateAgeYears = 26,
            SnapshotPhoneNumber = "0611122233",
                SnapshotWhatsAppAllowed = true,
                Status = ApplicationStatus.Pending,
            EmailVerifiedAt = DateTime.UtcNow.AddMinutes(-30),
            WorkPermitConfirmed = true,
            MatchPercent = 80,
            Motivation = "Extra sollicitatie voor reactie-flow.",
            CreatedAt = DateTime.UtcNow.AddMinutes(-40)
        });
        await db.SaveChangesAsync();
        return (vacancyId, applicationId);
    }

    public async Task<string> SeedForeignBranchEmployerAsync()
    {
        EnsureSeeded();
        var n = Interlocked.Increment(ref _extraSeedCounter);
        var companyId = Guid.NewGuid();
        var email = $"foreign-branch-{n}@jobsy.local";
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = $"Ander Filiaal {n}",
            KvkNumber = "87654321",
            Address = "Elders 1",
            Location = new GeoPoint(52.1, 4.3)
        });
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = "Andere Branch",
            Role = UserRole.BranchManager,
            IsActive = true,
            CompanyId = companyId,
            ConsentVersion = PrivacyConstants.CurrentConsentVersion
        });
        await db.SaveChangesAsync();
        return email;
    }

    public async Task<Guid> SeedUnverifiedApplicationWithUploadedCvAsync()
    {
        EnsureSeeded();
        var n = Interlocked.Increment(ref _extraSeedCounter);
        var vacancyId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var cv = "%PDF-1.4 unverified-cv"u8.ToArray();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            Title = $"Unverified CV vacature {n}",
            Description = "Geïsoleerde vacature voor CV-download vóór verificatie.",
            HourlyWage = 14.50m,
            StartDate = today.AddDays(-1),
            EndDate = today.AddMonths(1),
            Status = VacancyStatus.Active,
            CompanyId = CompanyId,
            Location = new GeoPoint(52.0, 4.2),
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.Winkel,
            WorkTypeLabels = "Winkel",
            SalaryTableId = SalaryTableId,
            PublishedAtUtc = DateTime.UtcNow.AddHours(-1),
            MinHoursPerWeek = 8,
            MaxHoursPerWeek = 16,
            FlexibleTimes = true,
            FlexibleScheduleSource = nameof(FlexibleScheduleSource.Manual),
            MaxApplications = 5,
            CategoryId = VacancyCategoryDefaults.RegulierId,
            RequireEmailVerification = true
        });
        db.Applications.Add(new Application
        {
            Id = applicationId,
            VacancyId = vacancyId,
            CandidateUserId = CandidateId,
            CandidateName = "Kandidaat Test",
            CandidateEmail = CandidateEmail,
            Status = ApplicationStatus.Pending,
            EmailVerifiedAt = null,
            HasUploadedCv = true,
            CreatedAt = DateTime.UtcNow
        });
        db.ApplicationUploadedCvs.Add(new ApplicationUploadedCv
        {
            ApplicationId = applicationId,
            FileName = $"unverified-{n}.pdf",
            ContentType = "application/pdf",
            Content = cv,
            SizeBytes = cv.Length
        });
        await db.SaveChangesAsync();
        return applicationId;
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
