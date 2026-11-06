using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Jobsy.Api.Controllers;
using Jobsy.Api.Models;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

/// <summary>
/// Unverified companies/vacancies must be invisible on every anonymous public channel.
/// Also guards that new AllowAnonymous GET actions are classified here.
/// </summary>
public class PublicVisibilityEndpointTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Every AllowAnonymous GET on the listed controllers must appear here as
    /// "filtered" (publisher rule applies) or "safe" (no Lobsy company/vacancy data).
    /// </summary>
    public static readonly (string Controller, string Action, string Classification)[] AnonymousGetCatalog =
    [
        ("VacanciesController", "GetActive", "filtered"),
        ("VacanciesController", "GetMapView", "filtered"),
        ("VacanciesController", "GetPins", "filtered"),
        ("VacanciesController", "GetCard", "filtered"),
        ("VacanciesController", "GetCards", "filtered"),
        ("VacanciesController", "Discover", "filtered"),
        ("VacanciesController", "GetPublicImage", "filtered"),
        ("VacanciesController", "GetById", "filtered"),
        ("VacanciesController", "GetTravel", "filtered"),
        ("VacanciesController", "GetCultureFit", "filtered"),
        // errors 03: similar vacancies for a 410 page, straight off the public discovery index.
        ("VacanciesController", "GetSimilar", "filtered"),
        ("PublicCompaniesController", "GetByKvk", "filtered"),
        ("PublicCompaniesController", "GetByVestiging", "filtered"),
        ("PublicCompaniesController", "GetVacanciesByKvk", "filtered"),
        ("PublicCompaniesController", "GetVacanciesByVestiging", "filtered"),
        ("SiteController", "GetBranding", "safe"),
        ("SiteController", "GetLegal", "safe"),
        ("SiteController", "GetCrawlIndex", "filtered"),
        // errors 05: the maintenance flag and its expected end time only — no internal note.
        ("SiteController", "GetStatus", "safe"),
        ("EmployerFlyersController", "ResolvePublicRoute", "filtered"),
    ];

    public PublicVisibilityEndpointTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public void Every_allow_anonymous_get_is_classified()
    {
        var catalog = AnonymousGetCatalog
            .Select(x => (x.Controller, x.Action))
            .ToHashSet();

        var controllers = new[]
        {
            typeof(VacanciesController),
            typeof(PublicCompaniesController),
            typeof(SiteController),
            typeof(EmployerFlyersController),
            typeof(VacancyEngagementController)
        };

        var missing = new List<string>();
        var unexpected = new List<string>();

        foreach (var type in controllers)
        {
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (method.GetCustomAttribute<HttpGetAttribute>() is null
                    && method.GetCustomAttributes<HttpMethodAttribute>().All(a => a.HttpMethods.All(m => m != "GET")))
                {
                    // Also accept [HttpGet("…")] via HttpMethodAttribute
                    if (!method.GetCustomAttributes().Any(a =>
                            a is HttpMethodAttribute hma && hma.HttpMethods.Contains("GET")))
                    {
                        continue;
                    }
                }

                var allowAnon = method.GetCustomAttribute<AllowAnonymousAttribute>() is not null
                    || type.GetCustomAttribute<AllowAnonymousAttribute>() is not null;
                if (!allowAnon)
                {
                    continue;
                }

                // Skip if the method itself has Authorize that overrides (class-level AllowAnonymous + method Authorize)
                if (method.GetCustomAttribute<AuthorizeAttribute>() is not null
                    && method.GetCustomAttribute<AllowAnonymousAttribute>() is null)
                {
                    continue;
                }

                var key = (type.Name, method.Name);
                if (!catalog.Contains(key))
                {
                    missing.Add($"{type.Name}.{method.Name}");
                }
            }
        }

        foreach (var (controller, action, _) in AnonymousGetCatalog)
        {
            var type = controllers.FirstOrDefault(t => t.Name == controller);
            if (type is null || type.GetMethod(action) is null)
            {
                unexpected.Add($"{controller}.{action}");
            }
        }

        Assert.True(
            missing.Count == 0 && unexpected.Count == 0,
            "Anonymous GET classification drift.\nMissing: "
            + string.Join(", ", missing)
            + "\nStale catalog: "
            + string.Join(", ", unexpected));
    }

    [Fact]
    public async Task Unverified_company_and_vacancy_are_invisible_on_public_channels()
    {
        var (unverifiedCompanyId, unverifiedVacancyId, kvk, vestiging) =
            await SeedUnverifiedWithActiveVacancyAsync();

        var client = _factory.CreateClient();

        // Control (verified seed vacancy) is present.
        var controlDetail = await client.GetAsync($"api/vacancies/{_factory.VacancyId}");
        Assert.Equal(HttpStatusCode.OK, controlDetail.StatusCode);

        // Unverified vacancy detail / image / travel / culture-fit → 404.
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"api/vacancies/{unverifiedVacancyId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"api/vacancies/{unverifiedVacancyId}/image")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await client.GetAsync($"api/vacancies/{unverifiedVacancyId}/travel?originLat=52&originLng=4.2&transport=Fiets")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"api/vacancies/{unverifiedVacancyId}/culture-fit")).StatusCode);

        // Index-backed: pins / cards / discover / map-view must not include unverified.
        var pins = await client.GetFromJsonAsync<List<VacancyPinDto>>("api/vacancies/pins", JsonOpts);
        Assert.DoesNotContain(pins!, p => p.Id == unverifiedVacancyId);
        Assert.Contains(pins!, p => p.Id == _factory.VacancyId);

        var card = await client.GetAsync($"api/vacancies/{unverifiedVacancyId}/card");
        Assert.Equal(HttpStatusCode.NotFound, card.StatusCode);

        var cards = await client.GetAsync($"api/vacancies/cards?ids={unverifiedVacancyId},{_factory.VacancyId}");
        Assert.Equal(HttpStatusCode.OK, cards.StatusCode);
        var cardList = await cards.Content.ReadFromJsonAsync<List<VacancyCardDto>>(JsonOpts);
        Assert.DoesNotContain(cardList!, c => c.Id == unverifiedVacancyId);
        Assert.Contains(cardList!, c => c.Id == _factory.VacancyId);

        var discoverRaw = await client.GetStringAsync("api/vacancies/discover");
        using var discoverDoc = JsonDocument.Parse(discoverRaw);
        var discoverIds = discoverDoc.RootElement.ValueKind == JsonValueKind.Array
            ? discoverDoc.RootElement.EnumerateArray().Select(e => e.GetProperty("id").GetGuid()).ToList()
            : discoverDoc.RootElement.GetProperty("items").EnumerateArray().Select(e => e.GetProperty("id").GetGuid()).ToList();
        Assert.DoesNotContain(unverifiedVacancyId, discoverIds);
        Assert.Contains(_factory.VacancyId, discoverIds);

        // Public company pages.
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"api/public/companies/{kvk}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await client.GetAsync($"api/public/companies/{kvk}/{vestiging}")).StatusCode);

        // Flyer route.
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await client.GetAsync($"api/employer-flyers/public/branches/{unverifiedCompanyId}/route")).StatusCode);

        // Crawl index.
        var crawl = await client.GetFromJsonAsync<SiteCrawlIndexDto>("api/site/crawl-index", JsonOpts);
        Assert.DoesNotContain(crawl!.Vacancies, v => v.Id == unverifiedVacancyId);
        Assert.Contains(crawl.Vacancies, v => v.Id == _factory.VacancyId);
        Assert.DoesNotContain(crawl.CompanyPaths, p => p.Contains(kvk, StringComparison.Ordinal));

        // Apply must reject (visibility gate).
        var applyClient = _factory.CreateClient();
        JobsyTestAuth.Authorize(applyClient, _factory.CandidateId);
        var apply = await applyClient.PostAsJsonAsync("api/applications", new
        {
            vacancyId = unverifiedVacancyId,
            acceptedTerms = true,
            workPermitConfirmed = true
        });
        Assert.True(
            apply.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound,
            $"Apply expected 400/404, got {(int)apply.StatusCode}");

        // Flip to Verified → appears after index invalidate/refresh.
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var company = await db.Companies.FirstAsync(c => c.Id == unverifiedCompanyId);
            company.VerificationStatus = CompanyVerificationStatus.Verified;
            company.VerificationMethod = CompanyVerificationMethod.Manual;
            company.VerifiedAtUtc = DateTime.UtcNow;
            company.VerificationUpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var index = scope.ServiceProvider.GetRequiredService<IVacancyDiscoveryIndex>();
            await index.InvalidateCompanyAsync(unverifiedCompanyId);
            await index.RefreshAsync();
        }

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"api/vacancies/{unverifiedVacancyId}")).StatusCode);
        var pinsAfter = await client.GetFromJsonAsync<List<VacancyPinDto>>("api/vacancies/pins", JsonOpts);
        Assert.Contains(pinsAfter!, p => p.Id == unverifiedVacancyId);
    }

    [Fact]
    public async Task Employer_preview_of_unverified_vacancy_is_noindex()
    {
        var (_, unverifiedVacancyId, _, _) = await SeedUnverifiedWithActiveVacancyAsync();

        // Employer of a *different* company must not see it.
        var foreignClient = _factory.CreateClient();
        JobsyTestAuth.Authorize(foreignClient, _factory.EmployerId);
        var forbidden = await foreignClient.GetAsync($"api/vacancies/{unverifiedVacancyId}");
        Assert.Equal(HttpStatusCode.NotFound, forbidden.StatusCode);

        // Seed a manager on the unverified company and preview.
        Guid managerId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var vacancy = await db.Vacancies.AsNoTracking().FirstAsync(v => v.Id == unverifiedVacancyId);
            managerId = Guid.NewGuid();
            db.Users.Add(new User
            {
                Id = managerId,
                Email = $"preview-{managerId:N}@jobsy.local",
                FullName = "Preview Manager",
                Role = UserRole.BranchManager,
                IsActive = true,
                CompanyId = vacancy.CompanyId
            });
            db.UserCompanies.Add(new UserCompany { UserId = managerId, CompanyId = vacancy.CompanyId });
            await db.SaveChangesAsync();
        }

        var previewClient = _factory.CreateClient();
        JobsyTestAuth.Authorize(previewClient, managerId);
        var preview = await previewClient.GetAsync($"api/vacancies/{unverifiedVacancyId}");
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        Assert.Contains("noindex", preview.Headers.GetValues("X-Robots-Tag").FirstOrDefault() ?? "", StringComparison.OrdinalIgnoreCase);
        var dto = await preview.Content.ReadFromJsonAsync<VacancyListItemDto>(JsonOpts);
        Assert.True(dto!.IsPreview);
    }

    private async Task<(Guid CompanyId, Guid VacancyId, string Kvk, string Vestiging)> SeedUnverifiedWithActiveVacancyAsync()
    {
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        var kvk = "90123456";
        var vestiging = "0001";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        // Touch factory seed.
        _ = _factory.CreateClient();

        if (!await db.Companies.AnyAsync(c => c.Id == companyId))
        {
            db.Companies.Add(new Company
            {
                Id = companyId,
                Name = "Unverified Test BV",
                KvkNumber = kvk,
                KvkEstablishmentId = $"{kvk}_{vestiging}",
                Address = "Geheimestraat 1, Delft",
                Location = new GeoPoint(52.01, 4.35),
                Type = CompanyType.Employer,
                VerificationStatus = CompanyVerificationStatus.Unverified,
                VerificationMethod = CompanyVerificationMethod.None,
                VerificationUpdatedAtUtc = DateTime.UtcNow
            });
            db.Vacancies.Add(new Vacancy
            {
                Id = vacancyId,
                Title = "Unverified Active Vacancy",
                Description = "Should stay invisible until verified.",
                HourlyWage = 15m,
                StartDate = today.AddDays(-1),
                EndDate = today.AddMonths(1),
                Status = VacancyStatus.Active,
                CompanyId = companyId,
                Location = new GeoPoint(52.01, 4.35),
                RequiredTransport = TransportMode.Bike,
                WorkTypes = WorkType.Winkel,
                WorkTypeLabels = "Winkel",
                PublishedAtUtc = DateTime.UtcNow,
                MinHoursPerWeek = 8,
                MaxHoursPerWeek = 16,
                FlexibleTimes = true,
                MaxApplications = 5,
                CategoryId = VacancyCategoryDefaults.RegulierId
            });
            await db.SaveChangesAsync();
        }

        var index = scope.ServiceProvider.GetRequiredService<IVacancyDiscoveryIndex>();
        index.Invalidate();
        await index.RefreshAsync();

        return (companyId, vacancyId, kvk, vestiging);
    }
}
