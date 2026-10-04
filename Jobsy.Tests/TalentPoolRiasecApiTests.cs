using System.Net;
using Bunit;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Web.Components.Werkgever.Sections;
using Jobsy.Web.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

public class TalentPoolRiasecApiTests
{
    private static readonly string[] Routes =
    [
        "api/employer/talent/search",
        "api/werkgever/talentpool/search"
    ];

    [Theory]
    [InlineData(false, "api/employer/talent/search")]
    [InlineData(false, "api/werkgever/talentpool/search")]
    [InlineData(true, "api/employer/talent/search")]
    [InlineData(true, "api/werkgever/talentpool/search")]
    public async Task Both_search_routes_honor_the_riasec_switch(bool showRiasec, string route)
    {
        await using var factory = new TalentPoolRiasecApiFactory(showRiasec);
        var client = factory.CreateClient();
        JobsyTestAuth.Authorize(client, factory.EmployerId);

        var social = await client.GetAsync($"{route}?tags={Uri.EscapeDataString(CareerTestCatalog.Social)}");
        Assert.Equal(HttpStatusCode.OK, social.StatusCode);
        var socialJson = await social.Content.ReadAsStringAsync();
        var socialCards = ReadCards(socialJson);

        var competency = await client.GetAsync($"{route}?tags=Samenwerken");
        Assert.Equal(HttpStatusCode.OK, competency.StatusCode);
        var competencyJson = await competency.Content.ReadAsStringAsync();
        var competencyCards = ReadCards(competencyJson);

        if (showRiasec)
        {
            Assert.Contains(socialCards, c => c.CandidateUserId == factory.CandidateId);
            var card = Assert.Single(socialCards, c => c.CandidateUserId == factory.CandidateId);
            Assert.Contains(CareerTestCatalog.Social, card.RiasecTags);
            Assert.Equal("S", card.HollandCode);
        }
        else
        {
            Assert.DoesNotContain(socialCards, c => c.CandidateUserId == factory.CandidateId);
            var card = Assert.Single(competencyCards, c => c.CandidateUserId == factory.CandidateId);
            Assert.Empty(card.RiasecTags);
            Assert.Null(card.HollandCode);
            Assert.Contains("Samenwerken", card.MatchTags);
            Assert.DoesNotContain(card.MatchTags, TalentPoolRiasecVisibility.IsCareerTestOutput);
            Assert.DoesNotContain(card.RiasecTags, TalentPoolRiasecVisibility.IsCareerTestOutput);
            AssertEmployerPayloadHasNoCareerTest(socialJson);
            AssertEmployerPayloadHasNoCareerTest(competencyJson);
            AssertHollandCodeNullOrAbsent(socialJson);
            AssertHollandCodeNullOrAbsent(competencyJson);

            var dutch = await client.GetAsync($"{route}?tags={Uri.EscapeDataString("Mensen helpen")}");
            Assert.Equal(HttpStatusCode.OK, dutch.StatusCode);
            Assert.DoesNotContain(
                factory.CandidateId.ToString("D"),
                await dutch.Content.ReadAsStringAsync(),
                StringComparison.OrdinalIgnoreCase);
            AssertCardMarkupHidesCareerTest(card);
        }

        AssertScoresAbsent(socialJson);
        AssertScoresAbsent(competencyJson);
    }

    private static void AssertCardMarkupHidesCareerTest(TalentCardJson card)
    {
        using var bunit = new BunitContext();
        var leaked = new AnonymousTalentCard
        {
            RegionLabel = "Westland",
            MatchTags = ["Samenwerken", .. card.MatchTags, "Realistic", "Social", "Mensen helpen", "SEC"],
            RiasecTags = ["Social", "Realistic", .. card.RiasecTags],
            HollandCode = card.HollandCode ?? "S",
            AvailabilitySummary = "Doordeweeks"
        };
        var cut = bunit.Render<TalentPoolResultCard>(p => p
            .Add(x => x.ShowRiasec, false)
            .Add(x => x.Card, leaked));
        var markup = cut.Markup;
        Assert.Contains("Samenwerken", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("data-testid=\"talent-holland\"", markup, StringComparison.Ordinal);
        foreach (var name in CareerTestCatalog.RiasecCodes)
        {
            Assert.DoesNotContain(name, markup, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var name in TalentPoolRiasecVisibility.DutchTypeLabels)
        {
            Assert.DoesNotContain(name, markup, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain(">SEC<", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Holland", markup, StringComparison.OrdinalIgnoreCase);

        var fromApi = bunit.Render<TalentPoolResultCard>(p => p
            .Add(x => x.ShowRiasec, false)
            .Add(x => x.Card, new AnonymousTalentCard
            {
                RegionLabel = "Westland",
                MatchTags = card.MatchTags,
                RiasecTags = card.RiasecTags,
                HollandCode = card.HollandCode
            }));
        Assert.False(TalentPoolRiasecVisibility.ContainsCareerTestName(fromApi.Markup), fromApi.Markup);
    }

    private static void AssertScoresAbsent(string json)
    {
        foreach (var key in new[]
        {
            "careerScores",
            "competencyScores",
            "competenceDeepCompleted",
            "careerDeepCompleted",
            "matchPercent",
            "matchScore",
            "matchBreakdown",
            "storyText",
            "fromOpenAi",
            "rank"
        })
        {
            Assert.DoesNotContain(key, json, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("88", json, StringComparison.Ordinal);
        Assert.DoesNotContain("81", json, StringComparison.Ordinal);
    }

    private static void AssertEmployerPayloadHasNoCareerTest(string json)
    {
        Assert.False(TalentPoolRiasecVisibility.ContainsCareerTestName(json), json);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Walk(doc.RootElement);
    }

    private static void Walk(System.Text.Json.JsonElement element)
    {
        switch (element.ValueKind)
        {
            case System.Text.Json.JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    Walk(prop.Value);
                }

                break;
            case System.Text.Json.JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    Walk(item);
                }

                break;
            case System.Text.Json.JsonValueKind.String:
                var text = element.GetString();
                Assert.False(TalentPoolRiasecVisibility.IsCareerTestOutput(text), text);
                Assert.False(TalentPoolRiasecVisibility.ContainsCareerTestName(text), text);
                break;
        }
    }

    private static void AssertHollandCodeNullOrAbsent(string json)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        foreach (var card in doc.RootElement.EnumerateArray())
        {
            if (!card.TryGetProperty("hollandCode", out var holland))
            {
                continue;
            }

            Assert.Equal(System.Text.Json.JsonValueKind.Null, holland.ValueKind);
        }
    }

    [Fact]
    public void Both_route_templates_are_on_the_search_action()
    {
        Assert.Equal(2, Routes.Length);
        var root = FindRepoRoot();
        var source = File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/TalentPoolController.cs"));
        Assert.Contains("[HttpGet(\"search\")]", source, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"/api/werkgever/talentpool/search\")]", source, StringComparison.Ordinal);
        Assert.Contains("ShowRiasecTagFilter", File.ReadAllText(Path.Combine(root, "Jobsy.Api/appsettings.json")), StringComparison.Ordinal);
        Assert.Contains("ShowRiasecTagFilter", File.ReadAllText(Path.Combine(root, "Jobsy.Web/appsettings.json")), StringComparison.Ordinal);
    }

    private static List<TalentCardJson> ReadCards(string json)
        => System.Text.Json.JsonSerializer.Deserialize<List<TalentCardJson>>(
            json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found.");
    }

    private sealed record TalentCardJson(
        Guid CandidateUserId,
        List<string> MatchTags,
        List<string> RiasecTags,
        string? HollandCode);
}

public sealed class TalentPoolRiasecApiFactory : WebApplicationFactory<Jobsy.Api.ApiAssemblyMarker>
{
    private readonly string _dbName = "TalentRiasec-" + Guid.NewGuid().ToString("N");
    private readonly bool _showRiasec;
    private bool _seeded;

    public TalentPoolRiasecApiFactory(bool showRiasec) => _showRiasec = showRiasec;

    public Guid CompanyId { get; } = Guid.Parse("b2000000-0000-0000-0000-000000000001");
    public Guid EmployerId { get; } = Guid.Parse("b2000000-0000-0000-0000-000000000002");
    public Guid CandidateId { get; } = Guid.Parse("b2000000-0000-0000-0000-000000000003");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        JobsyTestAuth.ApplyStandardAuthSettings(builder);
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("Swagger:Enabled", "false");
        builder.UseSetting(TalentPoolRiasecVisibility.ConfigKey, _showRiasec ? "true" : "false");
        builder.UseSetting(
            "ConnectionStrings:JobsyDb",
            "Host=127.0.0.1;Port=5432;Database=JobsyTest;Username=postgres;Password=postgres");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            var descriptors = services
                .Where(d =>
                    d.ServiceType == typeof(JobsyDbContext)
                    || d.ServiceType == typeof(DbContextOptions<JobsyDbContext>)
                    || (d.ImplementationType?.FullName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
                    || (d.ServiceType.FullName?.Contains("EntityFrameworkCore", StringComparison.Ordinal) == true
                        && d.ServiceType.FullName.Contains("JobsyDbContext", StringComparison.Ordinal)))
                .ToList();
            foreach (var d in descriptors)
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

        _seeded = true;
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        db.Companies.Add(new Company
        {
            Id = CompanyId,
            Name = "Filiaal",
            KvkNumber = "87654321",
            Address = "Straat 2",
            Location = new GeoPoint(52.01, 4.21),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow
        });
        db.Users.AddRange(
            new User
            {
                Id = EmployerId,
                Email = "bm-riasec@jobsy.local",
                FullName = "Filiaalmanager",
                Role = UserRole.BranchManager,
                IsActive = true,
                CompanyId = CompanyId
            },
            new User
            {
                Id = CandidateId,
                Email = "cand-riasec@jobsy.local",
                FullName = "Kandidaat",
                Role = UserRole.Candidate,
                IsActive = true,
                OpenForWork = true,
                DateOfBirth = new DateOnly(1994, 6, 1),
                TalentPoolConsentAt = DateTime.UtcNow,
                TalentPoolConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion
            });
        db.CandidateCareerInterests.Add(new CandidateCareerInterest
        {
            Id = Guid.NewGuid(),
            UserId = CandidateId,
            Status = CandidateCompetencyStatuses.Completed,
            HollandCode = "S",
            SocialPercent = 81,
            RealisticPercent = 70,
            RiasecTagsJson = CareerTestCatalog.SerializeTags([CareerTestCatalog.Social, CareerTestCatalog.Realistic]),
            MatchTagsJson = CareerTestCatalog.SerializeTags(
            [
                CareerTestCatalog.Realistic,
                CareerTestCatalog.Social,
                "Mensen helpen"
            ]),
            CompletedAtUtc = DateTime.UtcNow
        });
        db.CandidateCompetencies.Add(new CandidateCompetency
        {
            Id = Guid.NewGuid(),
            UserId = CandidateId,
            Status = CandidateCompetencyStatuses.Completed,
            SamenwerkenPercent = 88,
            MatchTagsJson = CompetencyTestCatalog.SerializeTags(["Samenwerken"]),
            CompletedAtUtc = DateTime.UtcNow
        });
        db.SaveChanges();
    }
}
