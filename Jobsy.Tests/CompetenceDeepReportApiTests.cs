using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Reports.Competence;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

[Collection("SequentialApi")]
public class CompetenceDeepReportApiTests : IClassFixture<CompetenceDeepReportApiFactory>
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private readonly CompetenceDeepReportApiFactory _factory;

    public CompetenceDeepReportApiTests(CompetenceDeepReportApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Completing_deep_competence_test_stores_report_json()
    {
        var client = Authed(_factory.UnlockedCandidateEmail);
        await CompleteDeepAnalysisAsync(client);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var row = await db.CandidateDeepAnalyses.SingleAsync(
            d => d.UserId == _factory.UnlockedCandidateId && d.Kind == AssessmentKind.Competence);

        Assert.Equal(CandidateDeepAnalysisStatuses.Completed, row.Status);
        Assert.False(string.IsNullOrWhiteSpace(row.ReportJson));

        var report = CompetenceDeepReportJson.Deserialize(row.ReportJson);
        Assert.NotNull(report);
        Assert.Equal(5, report!.Traits.Count);
        Assert.Equal(CompetenceDeepReportJson.CurrentReportVersion, row.ReportVersion);
    }

    [Fact]
    public async Task Get_deep_analysis_returns_stored_report_without_calling_ai_again()
    {
        _factory.Ai.Reset();
        var client = Authed(_factory.UnlockedCandidateEmail);
        await CompleteDeepAnalysisAsync(client);

        // Completion attempts AI once (tryAi:true); our fake throws, so the build falls back to
        // the template report — but the call itself must have happened exactly once.
        var callsAfterComplete = _factory.Ai.Calls;
        Assert.Equal(1, callsAfterComplete);

        var first = await client.GetAsync("api/me/deep-analysis?kind=competence");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstBody = await first.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(firstBody.TryGetProperty("competenceReport", out var reportElement));
        Assert.Equal(JsonValueKind.Object, reportElement.ValueKind);
        Assert.True(reportElement.GetProperty("traits").GetArrayLength() == 5);

        var second = await client.GetAsync("api/me/deep-analysis?kind=competence");
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        // GET must never call AI — neither on a fresh read nor a repeat read.
        Assert.Equal(callsAfterComplete, _factory.Ai.Calls);
    }

    [Fact]
    public async Task Pdf_download_returns_nine_pages_without_calling_ai()
    {
        var client = Authed(_factory.UnlockedCandidateEmail);
        await CompleteDeepAnalysisAsync(client);
        _factory.Ai.Reset();

        var response = await client.GetAsync("api/me/deep-analysis/report?kind=competence");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 1000);
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);

        Assert.Equal(9, CountPdfPages(bytes));

        // The 9-page rich report is rendered from the already-stored report only.
        Assert.Equal(0, _factory.Ai.Calls);

        // A second download should hit the in-memory PDF cache and return identical bytes.
        var second = await client.GetAsync("api/me/deep-analysis/report?kind=competence");
        var secondBytes = await second.Content.ReadAsByteArrayAsync();
        Assert.Equal(bytes.Length, secondBytes.Length);
        Assert.Equal(0, _factory.Ai.Calls);
    }

    [Fact]
    public async Task Free_user_without_unlock_never_leaks_a_competence_report()
    {
        var client = Authed(_factory.FreeCandidateEmail);
        var response = await client.GetAsync("api/me/deep-analysis?kind=competence");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.False(body.TryGetProperty("competenceReport", out _));
        Assert.False(body.GetProperty("isUnlocked").GetBoolean());
        Assert.False(body.GetProperty("isCompleted").GetBoolean());

        // The teaser is UI-only: the API must not hand back a report for a locked test either.
        var pdf = await client.GetAsync("api/me/deep-analysis/report?kind=competence");
        Assert.Equal(HttpStatusCode.NotFound, pdf.StatusCode);
    }

    private static int CountPdfPages(byte[] pdf)
    {
        var text = System.Text.Encoding.Latin1.GetString(pdf);
        return Regex.Matches(text, @"/Type\s*/Page(?!s)").Count;
    }

    private static async Task CompleteDeepAnalysisAsync(HttpClient client)
    {
        var answers = DeepAnalysisCatalog.Questions.ToDictionary(q => q.Id.ToString(), _ => 4);
        var response = await client.PutAsJsonAsync(
            "api/me/deep-analysis?kind=competence",
            new { answers, complete = true });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private HttpClient Authed(string email)
    {
        var userId = email == _factory.UnlockedCandidateEmail
            ? _factory.UnlockedCandidateId
            : _factory.FreeCandidateId;
        return JobsyTestAuth.CreateAuthenticatedClient(_factory, userId);
    }
}

public sealed class CompetenceDeepReportApiFactory : WebApplicationFactory<Program>
{
    public Guid UnlockedCandidateId { get; } = Guid.Parse("e4000000-0000-0000-0000-000000000010");
    public Guid FreeCandidateId { get; } = Guid.Parse("e4000000-0000-0000-0000-000000000011");
    public string UnlockedCandidateEmail => "deep-competence-unlocked@jobsy.local";
    public string FreeCandidateEmail => "deep-competence-free@jobsy.local";
    public ThrowingCompetenceAiService Ai { get; } = new();

    private readonly string _dbName = "CompetenceDeepReportApi-" + Guid.NewGuid();
    private bool _seeded;

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

            services.AddDbContext<JobsyDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            services.RemoveAll<ICompetenceDeepReportAiService>();
            services.AddSingleton<ICompetenceDeepReportAiService>(Ai);
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
        if (db.Users.Any(u => u.Id == UnlockedCandidateId))
        {
            _seeded = true;
            return;
        }

        db.Users.Add(new User
        {
            Id = UnlockedCandidateId,
            Email = UnlockedCandidateEmail,
            FullName = "Deep Report Kandidaat",
            Role = UserRole.Candidate,
            IsActive = true
        });
        db.Users.Add(new User
        {
            Id = FreeCandidateId,
            Email = FreeCandidateEmail,
            FullName = "Gratis Kandidaat",
            Role = UserRole.Candidate,
            IsActive = true
        });

        db.CandidateDeepAnalyses.Add(new CandidateDeepAnalysis
        {
            Id = Guid.NewGuid(),
            UserId = UnlockedCandidateId,
            Kind = AssessmentKind.Competence,
            Status = CandidateDeepAnalysisStatuses.Draft,
            AnswersJson = "{}",
            TagsJson = "[]",
            UnlockedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        db.SaveChanges();
        _seeded = true;
    }

    /// <summary>
    /// Fake AI service that counts calls and always throws, so tests can assert exactly when
    /// (and how often) the deep-report pipeline reaches out to OpenAI. Completion (tryAi:true)
    /// is expected to call this once and gracefully fall back to template copy; GET/PDF-download
    /// paths (tryAi:false, or reading an already-stored report) must never call it at all.
    /// </summary>
    public sealed class ThrowingCompetenceAiService : ICompetenceDeepReportAiService
    {
        public int Calls;

        public void Reset() => Calls = 0;

        public Task<(string Summary, IReadOnlyList<(string Title, string Body)> Steps)?> TryGenerateAsync(
            CompetenceDeepReport draftWithoutAi, string? jobTitle, CancellationToken ct)
        {
            Interlocked.Increment(ref Calls);
            throw new InvalidOperationException("AI must not be called in this test context.");
        }
    }
}
