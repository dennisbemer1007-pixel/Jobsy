using System.Net.Http.Headers;
using System.Security.Claims;
using Bunit;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Web.Components.Candidate.Passport;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;
using Microsoft.Playwright;
using UglyToad.PdfPig;

namespace Jobsy.Tests;

/// <summary>
/// Passport download button, and the PDF the button requests, with the flag on and off.
/// The flag is restored before the test returns.
/// </summary>
[Collection("PlaywrightSmoke")]
public class PassportPdfV2PlaywrightTests : BunitContext, IClassFixture<PassportPdfV2ApiFactory>
{
    private readonly PassportPdfV2ApiFactory _factory;

    public PassportPdfV2PlaywrightTests(PassportPdfV2ApiFactory factory)
    {
        _factory = factory;
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public async Task Download_button_says_dna_passport_when_the_flag_is_on()
    {
        var on = RenderCard(dna: true);
        var off = RenderCard(dna: false);
        Assert.Contains("DNA-paspoort (PDF)", on.Markup, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"passport-download-pdf\"", on.Markup, StringComparison.Ordinal);
        Assert.Contains("Lobsy-CV", off.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("DNA-paspoort (PDF)", off.Markup, StringComparison.Ordinal);

        await AssertButtonAsync(on.Markup, "DNA-paspoort (PDF)");
        await AssertButtonAsync(off.Markup, "Lobsy-CV");
    }

    [Fact]
    public async Task Flag_on_downloads_the_passport_and_flag_off_keeps_the_old_cv()
    {
        using var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, _factory.CandidateId);

        var before = await client.GetAsync("api/me/lobsy-cv.pdf");
        var beforeBytes = await before.Content.ReadAsByteArrayAsync();
        var beforeText = PdfText(beforeBytes);
        Assert.Equal("application/pdf", Media(before));
        Assert.Contains("visitekaartje", beforeText, StringComparison.OrdinalIgnoreCase);

        try
        {
            await _factory.SetPassportPdfAsync(true);

            Microsoft.Playwright.Program.Main(["install", "chromium"]);
            using var playwright = await Playwright.CreateAsync();
            await using var request = await playwright.APIRequest.NewContextAsync(new()
            {
                BaseURL = _factory.ServerAddress,
                ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    ["Authorization"] = new AuthenticationHeaderValue("Bearer", JobsyTestAuth.Mint(_factory.CandidateId)).ToString()
                }
            });

            var on = await request.GetAsync("api/me/lobsy-cv.pdf");
            Assert.True(on.Ok, await on.TextAsync());
            Assert.Contains("application/pdf", on.Headers["content-type"], StringComparison.OrdinalIgnoreCase);
            var onBytes = await on.BodyAsync();
            var onText = PdfText(onBytes);
            Assert.True(onBytes.Length > beforeBytes.Length);
            Assert.Contains("DNA-paspoort", onText, StringComparison.Ordinal);
            Assert.Contains("Samen & aardig", onText, StringComparison.Ordinal);
            Assert.DoesNotContain("%", onText, StringComparison.Ordinal);
            Assert.DoesNotContain("1994", onText, StringComparison.Ordinal);
            Assert.DoesNotContain("ZZ-AI-MARKER-WHOAMI", onText, StringComparison.Ordinal);
            Assert.DoesNotContain("Voorbeeldstraat", onText, StringComparison.Ordinal);
            Assert.DoesNotContain("geverifieerd", onText, StringComparison.OrdinalIgnoreCase);

            var alias = await request.GetAsync("api/me/paspoort.pdf");
            Assert.True(alias.Ok);
            Assert.Contains("application/pdf", alias.Headers["content-type"], StringComparison.OrdinalIgnoreCase);
            Assert.Contains("DNA-paspoort", PdfText(await alias.BodyAsync()), StringComparison.Ordinal);
        }
        finally
        {
            await _factory.SetPassportPdfAsync(false);
        }

        var after = await client.GetAsync("api/me/lobsy-cv.pdf");
        Assert.Contains("visitekaartje", PdfText(await after.Content.ReadAsByteArrayAsync()), StringComparison.OrdinalIgnoreCase);
    }

    private IRenderedComponent<PassportCard> RenderCard(bool dna)
        => Render<PassportCard>(p => p
            .Add(c => c.DisplayName, "Marta Kowalska")
            .Add(c => c.Initials, "MK")
            .Add(c => c.MemberNumber, "LB-48213")
            .Add(c => c.Compact, false)
            .Add(c => c.DnaPassport, dna));

    private static async Task AssertButtonAsync(string markup, string label)
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        foreach (var width in new[] { 390, 1440 })
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = 844 },
                Locale = "nl-NL"
            });
            var page = await context.NewPageAsync();
            await page.SetContentAsync("<!DOCTYPE html><html lang=\"nl\"><body style=\"margin:16px;font-family:sans-serif\">"
                                       + markup + "</body></html>");
            var overflow = await page.EvaluateAsync<bool>(
                "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
            Assert.False(overflow);
            await Assertions.Expect(page.GetByTestId("passport-download-pdf")).ToHaveTextAsync(label);
        }
    }

    private static string Media(HttpResponseMessage response)
        => response.Content.Headers.ContentType?.MediaType ?? "";

    private static string PdfText(byte[] bytes)
    {
        using var doc = PdfDocument.Open(bytes);
        return string.Join("\n", doc.GetPages().Select(page => page.Text));
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "marta")],
                "test"))));
    }
}

public sealed class PassportPdfV2ApiFactory : WebApplicationFactory<Jobsy.Api.ApiAssemblyMarker>
{
    public Guid CandidateId { get; } = Guid.Parse("d2a20000-0000-0000-0000-000000000001");

    public string ServerAddress { get; private set; } = "";

    private readonly string _dbName = "PassportPdfV2-" + Guid.NewGuid().ToString("N");
    private readonly object _seedGate = new();
    private bool _seeded;
    private IHost? _kestrel;

    public PassportPdfV2ApiFactory()
    {
        _ = CreateClient();
    }

    public async Task SetPassportPdfAsync(bool enabled)
    {
        using var scope = Services.CreateScope();
        var features = scope.ServiceProvider.GetRequiredService<IPlatformFeatureService>();
        await features.UpdateAsync(new PlatformFeatureUpdate(PassportPdfV2Enabled: enabled));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        JobsyTestAuth.ApplyStandardAuthSettings(builder);
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

            services.AddDbContext<JobsyDbContext>(options => options.UseInMemoryDatabase(_dbName));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var testHost = builder.Build();
        builder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseKestrel();
            webHostBuilder.UseUrls("http://127.0.0.1:0");
        });
        _kestrel = builder.Build();
        _kestrel.Start();
        var addresses = _kestrel.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        ServerAddress = addresses!.Addresses.First(a => a.StartsWith("http://", StringComparison.Ordinal));
        testHost.Start();
        return testHost;
    }

    protected override void ConfigureClient(HttpClient client)
    {
        EnsureSeeded();
        base.ConfigureClient(client);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _kestrel?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void EnsureSeeded()
    {
        lock (_seedGate)
        {
            if (_seeded)
            {
                return;
            }

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var user = new User
            {
                Id = CandidateId,
                Email = "marta@example.com",
                FullName = "Marta Kowalska",
                Role = UserRole.Candidate,
                IsActive = true,
                OpenForWork = true,
                PhoneNumber = "+31600000000",
                WhatsAppContactAllowed = true,
                EmailVerifiedAtUtc = new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc),
                DateOfBirth = new DateOnly(1994, 5, 12),
                PreferencesJson = """
                    {"roles":["Orderpicker"],"maxTravelMinutes":30,"preferredTransport":"Fiets","language":"nl","ageYears":32,"aboutMe":"Ik werk graag met mijn handen.","drivingLicenses":["B"],"availability":{"Ma":["Ochtend","Middag"],"Di":["Ochtend","Middag"]},"employers":[{"employerName":"Kwekerij De Voorbeeldtuin","role":"Oogstmedewerker","startMonth":"2024-03"}],"educations":["MBO 1"],"homeAddress":"Voorbeeldstraat 1, 2671 AB Naaldwijk","minHoursPerWeek":32,"maxHoursPerWeek":40,"certificates":[{"name":"VCA Basis","year":2024}],"spokenLanguages":[{"code":"pl","level":"moedertaal"}],"dutchLevel":"basis","workPreferences":{"indoor":"prefer","outdoor":"ok-not-frost","physicalWork":"lifting-15","pace":"norm-ok"},"shareEmployerPreferences":true,"employerPreferences":["small-team"],"workRegion":"Westland","hasOwnCar":false,"contractPreferences":["vast"]}
                    """
            };
            db.Users.Add(user);
            db.CandidateWhoAmIProfiles.Add(new CandidateWhoAmIProfile
            {
                Id = Guid.Parse("d2a20000-0000-0000-0000-000000000099"),
                UserId = CandidateId,
                IncludeOnCv = true,
                StoryText = "ZZ-AI-MARKER-WHOAMI dit verhaal hoort niet op het paspoort.",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
            var done = new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc);
            db.CandidateCompetencies.Add(new CandidateCompetency
            {
                Id = Guid.NewGuid(),
                UserId = CandidateId,
                Status = CandidateCompetencyStatuses.Completed,
                CompletedAtUtc = done,
                SamenwerkenPercent = 80,
                ResultaatgerichtheidPercent = 20,
                StressbestendigheidPercent = 21,
                InnovatiePercent = 22,
                ExtraversiePercent = 23
            });
            db.CandidateCareerInterests.Add(new CandidateCareerInterest
            {
                Id = Guid.NewGuid(),
                UserId = CandidateId,
                Status = CandidateCompetencyStatuses.Completed,
                CompletedAtUtc = done,
                RealisticPercent = 80,
                InvestigativePercent = 10,
                ArtisticPercent = 11,
                SocialPercent = 12,
                EnterprisingPercent = 13,
                ConventionalPercent = 14
            });
            db.CandidateCulturePersonalityProfiles.Add(new CandidateCulturePersonalityProfile
            {
                Id = Guid.NewGuid(),
                UserId = CandidateId,
                Status = CandidateCompetencyStatuses.Completed,
                CompletedAtUtc = done,
                AutonomyPercent = 10,
                InformalPercent = 20,
                CollaborationPercent = 90,
                FlexibilityPercent = 30,
                InnovationPercent = 40,
                PeopleFirstPercent = 50
            });
            db.CandidateValuesProfiles.Add(new CandidateValuesProfile
            {
                Id = Guid.NewGuid(),
                UserId = CandidateId,
                Status = CandidateCompetencyStatuses.Completed,
                CompletedAtUtc = done,
                AutonomyPercent = 10,
                ConnectionPercent = 20,
                AchievementPercent = 30,
                StabilityPercent = 90,
                ImpactPercent = 40
            });
            db.SaveChanges();
            _seeded = true;
        }
    }
}
